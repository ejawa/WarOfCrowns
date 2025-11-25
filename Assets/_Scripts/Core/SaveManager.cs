using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using WarOfCrowns.Data;
using WarOfCrowns.Buildings;
using WarOfCrowns.Units;
using WarOfCrowns.World;
using System.Linq;

namespace WarOfCrowns.Core
{
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        [Header("Ссылки для загрузки Юнитов")]
        [SerializeField] private GameObject peasantPrefab;

        [Header("Ссылки для загрузки Зданий")]
        [Tooltip("Перетащи сюда ВСЕ префабы зданий и фундаментов.")]
        [SerializeField] private List<GameObject> allBuildingPrefabs;

        [Header("Ссылки для загрузки Ресурсов Мира")]
        [Tooltip("Перетащи сюда префабы Tree_Resource, BerryBush_Full и BerryBush_Empty.")]
        [SerializeField] private List<GameObject> allResourcePrefabs;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            SaveSystem.Init();
        }

        private void Update()
        {
            if (Keyboard.current == null) return;
            if (Keyboard.current.f5Key.wasPressedThisFrame) SaveGame();
            if (Keyboard.current.f9Key.wasPressedThisFrame) LoadGame();
        }

        public void SaveGame()
        {
            Debug.Log("--- STARTING SAVE PROCESS ---");
            SaveKingdom();
            SaveBuildings();
            SaveWorldResources();
            SaveUnits();
            SaveWorldGen();
            Debug.Log("--- SAVE COMPLETE ---");

        }

        public void LoadGame()
        {
            Debug.Log("--- STARTING LOAD PROCESS ---");

            // 1. Сначала восстанавливаем Ландшафт (Сид)
            LoadWorldGen();

            // 2. Потом всё остальное
            LoadKingdom();
            LoadBuildings();
            LoadWorldResources(); // Ресурсы встанут на свои места на новой карте
            LoadUnits();

            Debug.Log("--- LOAD COMPLETE ---");
        }

        // --- 1. ЭКОНОМИКА ---
        private void SaveKingdom()
        {
            KingdomSaveData data = new KingdomSaveData();
            if (Kingdom.PlayerKingdom != null)
            {
                foreach (var pair in Kingdom.PlayerKingdom.GetAllInventory())
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
                Kingdom.PlayerKingdom.LoadInventoryFromSave(data.inventory);
            }
        }
        private void SaveWorldGen()
        {
            if (WorldGenerator.Instance != null)
            {
                WorldSaveData data = new WorldSaveData();
                data.seed = WorldGenerator.Instance.GetCurrentSeed();
                SaveSystem.SaveData(data, "world_data.json");
            }
        }

        private void LoadWorldGen()
        {
            WorldSaveData data = SaveSystem.LoadData<WorldSaveData>("world_data.json");
            if (data != null && WorldGenerator.Instance != null)
            {
                // Это критически важно сделать ДО загрузки зданий и ресурсов,
                // чтобы земля под ними была правильной.
                WorldGenerator.Instance.RegenerateWorldFromSave(data.seed);
            }
        }
        // --- 2. ЗДАНИЯ ---
        private void SaveBuildings()
        {
            BuildingListWrapper wrapper = new BuildingListWrapper();
            Building[] allBuildings = FindObjectsOfType<Building>();
            foreach (var building in allBuildings)
            {
                if (building.name.Contains("Ghost")) continue;
                wrapper.buildings.Add(building.GetSaveData());
            }
            SaveSystem.SaveData(wrapper, "buildings.json");
        }

        private void LoadBuildings()
        {
            BuildingListWrapper wrapper = SaveSystem.LoadData<BuildingListWrapper>("buildings.json");
            if (wrapper == null) return;

            var currentBuildings = FindObjectsOfType<Building>();
            foreach (var b in currentBuildings) if (!b.name.Contains("Ghost")) Destroy(b.gameObject);

            foreach (var data in wrapper.buildings)
            {
                GameObject prefabToSpawn = FindPrefabByName(allBuildingPrefabs, data.prefabName);
                if (prefabToSpawn != null)
                {
                    GameObject newObj = Instantiate(prefabToSpawn, new Vector3(data.posX, data.posY, data.posZ), Quaternion.identity);
                    if (newObj.TryGetComponent<Building>(out var b))
                    {
                        b.OwningKingdom = Kingdom.PlayerKingdom;
                        b.LoadFromData(data);
                    }
                    if (newObj.TryGetComponent<TownHall>(out var th)) th.OwningKingdom = Kingdom.PlayerKingdom;
                    if (newObj.TryGetComponent<ConstructionSite>(out var cs)) cs.OwningKingdom = Kingdom.PlayerKingdom;
                }
            }
        }

        // --- 3. РЕСУРСЫ МИРА ---
        private void SaveWorldResources()
        {
            WorldResourceListWrapper wrapper = new WorldResourceListWrapper();

            // Активные
            ResourceNode[] nodes = FindObjectsOfType<ResourceNode>();
            foreach (var node in nodes) wrapper.activeResources.Add(node.GetSaveData());

            // Респаунящиеся (пустые)
            RespawnController[] respawners = FindObjectsOfType<RespawnController>();
            foreach (var respawner in respawners) wrapper.respawningResources.Add(respawner.GetSaveData());

            SaveSystem.SaveData(wrapper, "world_resources.json");
        }

        private void LoadWorldResources()
        {
            WorldResourceListWrapper wrapper = SaveSystem.LoadData<WorldResourceListWrapper>("world_resources.json");
            if (wrapper == null) return;

            var currentNodes = FindObjectsOfType<ResourceNode>();
            foreach (var node in currentNodes) Destroy(node.gameObject);
            var currentRespawners = FindObjectsOfType<RespawnController>();
            foreach (var r in currentRespawners) Destroy(r.gameObject);

            // Активные
            foreach (var data in wrapper.activeResources)
            {
                GameObject prefab = FindPrefabByName(allResourcePrefabs, data.prefabName);
                if (prefab != null)
                {
                    GameObject newObj = Instantiate(prefab, new Vector3(data.posX, data.posY, data.posZ), Quaternion.identity);
                    if (newObj.TryGetComponent<ResourceNode>(out var node)) node.LoadFromData(data);
                }
            }
            // Пустые
            foreach (var data in wrapper.respawningResources)
            {
                GameObject emptyPrefab = FindPrefabByName(allResourcePrefabs, data.emptyPrefabName);
                if (emptyPrefab != null)
                {
                    GameObject newObj = Instantiate(emptyPrefab, new Vector3(data.posX, data.posY, data.posZ), Quaternion.identity);
                    RespawnController controller = newObj.GetComponent<RespawnController>();
                    if (controller == null) controller = newObj.AddComponent<RespawnController>();
                    controller.LoadFromData(data);
                }
            }
        }

        // --- 4. ЮНИТЫ (С ИСПРАВЛЕНИЕМ) ---
        private void SaveUnits()
        {
            UnitListWrapper wrapper = new UnitListWrapper();
            if (PopulationManager.Instance != null)
            {
                foreach (Unit unit in PopulationManager.Instance.AllUnits)
                {
                    if (unit != null) wrapper.units.Add(unit.GetSaveData());
                }
            }
            SaveSystem.SaveData(wrapper, "units.json");
        }

        private void LoadUnits()
        {
            UnitListWrapper wrapper = SaveSystem.LoadData<UnitListWrapper>("units.json");
            if (wrapper == null) return;

            if (PopulationManager.Instance != null) PopulationManager.Instance.ClearAllUnits();

            List<Unit> restoredUnits = new List<Unit>();

            // 1. Создаем
            foreach (UnitSaveData unitData in wrapper.units)
            {
                if (peasantPrefab != null)
                {
                    GameObject newUnitObj = Instantiate(peasantPrefab);
                    Unit unitComponent = newUnitObj.GetComponent<Unit>();
                    if (unitComponent != null)
                    {
                        unitComponent.OwningKingdom = Kingdom.PlayerKingdom;
                        unitComponent.LoadFromData(unitData);
                        restoredUnits.Add(unitComponent);
                    }
                }
            }

            // 2. Восстанавливаем связи (Relinking)
            Building[] buildingsOnScene = FindObjectsOfType<Building>();

            foreach (Unit unit in restoredUnits)
            {
                // Работа
                if (!string.IsNullOrEmpty(unit.savedWorkplaceID))
                {
                    Building workplaceData = buildingsOnScene.FirstOrDefault(b => b.uniqueID == unit.savedWorkplaceID);
                    if (workplaceData != null)
                    {
                        JobBuilding jobBuilding = workplaceData.GetComponent<JobBuilding>();
                        if (jobBuilding != null)
                        {
                            jobBuilding.AddWorker(unit);
                        }
                    }
                }

                // Мозг
                if (unit.TryGetComponent<UnitAI>(out var ai))
                {
                    if (unit.savedState == UnitState.Working && string.IsNullOrEmpty(unit.savedWorkplaceID))
                    {
                        ai.SetState(UnitState.Idling);
                    }
                    else if (unit.savedState == UnitState.SeekingFood)
                    {
                        ai.SeekFood();
                    }
                    else
                    {
                        ai.SetState(unit.savedState);
                    }
                }

                // --- ВОТ ЗДЕСЬ ТЕПЕРЬ ВЫЗЫВАЕТСЯ RestoreActions ВНУТРИ ЦИКЛА ---
                unit.RestoreActions();
                // ---------------------------------------------------------------
            }
        }

        private GameObject FindPrefabByName(List<GameObject> prefabsList, string name)
        {
            if (prefabsList == null) return null;
            foreach (var prefab in prefabsList)
            {
                if (prefab == null) continue;
                if (prefab.name == name) return prefab;
            }
            return null;
        }
    }
}