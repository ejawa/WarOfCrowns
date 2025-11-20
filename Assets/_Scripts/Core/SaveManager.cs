using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem; // Для новой системы ввода
using WarOfCrowns.Data;        // Для классов сохранения (SaveSystem, Wrapper'ы)
using WarOfCrowns.Buildings;   // Для Building, TownHall, ConstructionSite
using WarOfCrowns.Units;       // Для Unit

namespace WarOfCrowns.Core
{
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        [Header("Ссылки для загрузки Юнитов")]
        [SerializeField] private GameObject peasantPrefab;

        [Header("Ссылки для загрузки Зданий")]
        [Tooltip("Перетащи сюда ВСЕ префабы зданий и фундаментов, которые есть в игре.")]
        [SerializeField] private List<GameObject> allBuildingPrefabs;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Инициализируем систему файлов (создаем папку, если нет)
            SaveSystem.Init();
        }

        private void Update()
        {
            if (Keyboard.current == null) return;

            // F5 - Сохранить
            if (Keyboard.current.f5Key.wasPressedThisFrame)
            {
                SaveGame();
            }

            // F9 - Загрузить
            if (Keyboard.current.f9Key.wasPressedThisFrame)
            {
                LoadGame();
            }
        }

        public void SaveGame()
        {
            Debug.Log("--- STARTING SAVE PROCESS ---");
            SaveKingdom();
            SaveUnits();
            SaveBuildings();
            Debug.Log("--- SAVE COMPLETE ---");
        }

        public void LoadGame()
        {
            Debug.Log("--- STARTING LOAD PROCESS ---");
            // Порядок важен! Сначала экономика, потом здания, потом юниты.
            LoadKingdom();
            LoadBuildings();
            LoadUnits();
            Debug.Log("--- LOAD COMPLETE ---");
        }

        // --- 1. ЭКОНОМИКА (RESOURCES) ---

        private void SaveKingdom()
        {
            KingdomSaveData data = new KingdomSaveData();

            if (Kingdom.PlayerKingdom != null)
            {
                // Берем словарь из Kingdom
                Dictionary<ResourceType, int> currentInventory = Kingdom.PlayerKingdom.GetAllInventory();

                // Конвертируем в список для JSON
                foreach (var pair in currentInventory)
                {
                    data.inventory.Add(new ResourceSaveEntry(pair.Key, pair.Value));
                }
            }

            SaveSystem.SaveData(data, "resources.json");
        }

        private void LoadKingdom()
        {
            KingdomSaveData data = SaveSystem.LoadData<KingdomSaveData>("resources.json");

            if (data != null && Kingdom.PlayerKingdom != null)
            {
                // Передаем данные в Kingdom для обновления
                Kingdom.PlayerKingdom.LoadInventoryFromSave(data.inventory);
            }
            else
            {
                Debug.LogWarning("SaveManager: Save file 'resources.json' not found or Kingdom is null.");
            }
        }

        // --- 2. ЮНИТЫ (UNITS) ---

        private void SaveUnits()
        {
            UnitListWrapper wrapper = new UnitListWrapper();

            // Берем список всех живых юнитов из PopulationManager
            if (PopulationManager.Instance != null)
            {
                foreach (Unit unit in PopulationManager.Instance.AllUnits)
                {
                    if (unit != null)
                    {
                        wrapper.units.Add(unit.GetSaveData());
                    }
                }
            }

            SaveSystem.SaveData(wrapper, "units.json");
        }

        private void LoadUnits()
        {
            UnitListWrapper wrapper = SaveSystem.LoadData<UnitListWrapper>("units.json");
            if (wrapper == null) return;

            // 1. Уничтожаем всех текущих юнитов
            if (PopulationManager.Instance != null)
            {
                PopulationManager.Instance.ClearAllUnits();
            }

            // 2. Создаем сохраненных
            foreach (UnitSaveData unitData in wrapper.units)
            {
                if (peasantPrefab != null)
                {
                    // Создаем нового юнита
                    GameObject newUnitObj = Instantiate(peasantPrefab);

                    // Настраиваем его
                    Unit unitComponent = newUnitObj.GetComponent<Unit>();
                    if (unitComponent != null)
                    {
                        unitComponent.OwningKingdom = Kingdom.PlayerKingdom; // Возвращаем гражданство
                        unitComponent.LoadFromData(unitData); // Применяем позицию, здоровье и т.д.
                    }
                }
            }
        }

        // --- 3. ЗДАНИЯ (BUILDINGS) ---

        private void SaveBuildings()
        {
            BuildingListWrapper wrapper = new BuildingListWrapper();

            // Находим все здания на сцене
            Building[] allBuildings = FindObjectsOfType<Building>();

            foreach (var building in allBuildings)
            {
                // Игнорируем "призраков" (обычно у них отключен коллайдер или спец. слой, или имя Ghost)
                // Простейшая проверка: если имя содержит "Ghost", не сохраняем
                if (building.name.Contains("Ghost")) continue;

                // Также можно проверить слой, если призраки на слое IgnoreRaycast
                // if (building.gameObject.layer == LayerMask.NameToLayer("Ignore Raycast")) continue;

                wrapper.buildings.Add(building.GetSaveData());
            }

            SaveSystem.SaveData(wrapper, "buildings.json");
        }

        private void LoadBuildings()
        {
            BuildingListWrapper wrapper = SaveSystem.LoadData<BuildingListWrapper>("buildings.json");
            if (wrapper == null) return;

            // 1. Уничтожаем все текущие здания
            var currentBuildings = FindObjectsOfType<Building>();
            foreach (var b in currentBuildings)
            {
                // Не удаляем призраков, если они есть
                if (!b.name.Contains("Ghost"))
                {
                    Destroy(b.gameObject);
                }
            }

            // 2. Строим заново из файла
            foreach (var data in wrapper.buildings)
            {
                // Ищем префаб по имени в нашем списке
                GameObject prefabToSpawn = FindPrefabByName(data.prefabName);

                if (prefabToSpawn != null)
                {
                    Vector3 position = new Vector3(data.posX, data.posY, data.posZ);
                    GameObject newObj = Instantiate(prefabToSpawn, position, Quaternion.identity);

                    // Восстанавливаем "гражданство" (OwningKingdom)
                    // Проверяем все возможные компоненты, которым это нужно
                    if (newObj.TryGetComponent<Building>(out var b))
                        b.OwningKingdom = Kingdom.PlayerKingdom;

                    if (newObj.TryGetComponent<TownHall>(out var th))
                        th.OwningKingdom = Kingdom.PlayerKingdom;

                    if (newObj.TryGetComponent<ConstructionSite>(out var cs))
                        cs.OwningKingdom = Kingdom.PlayerKingdom;

                    // В будущем здесь можно восстановить прогресс стройки для ConstructionSite
                    // if (data.isConstructionSite && cs != null) cs.SetProgress(data.constructionProgress);
                }
                else
                {
                    Debug.LogError($"SaveManager: Could not find building prefab with name '{data.prefabName}' in the AllBuildingPrefabs list!");
                }
            }
        }

        // Вспомогательный метод для поиска префаба в списке инспектора
        private GameObject FindPrefabByName(string name)
        {
            if (allBuildingPrefabs == null) return null;

            foreach (var prefab in allBuildingPrefabs)
            {
                if (prefab == null) continue;
                // Сравниваем имя префаба с именем из сохранения
                if (prefab.name == name) return prefab;
            }
            return null;
        }
    }
}