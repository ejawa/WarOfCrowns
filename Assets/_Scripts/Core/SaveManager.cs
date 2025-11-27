using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Unity.Netcode;
using UnityEngine.InputSystem; // <-- ВАЖНО: Новая система ввода
using WarOfCrowns.Buildings;
using WarOfCrowns.Units;
using WarOfCrowns.World;
using WarOfCrowns.Data;
using System.Linq;

namespace WarOfCrowns.Core
{
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance;

        [Header("Префабы для загрузки")]
        public List<GameObject> unitPrefabs;
        public List<GameObject> buildingPrefabs;
        public List<GameObject> resourcePrefabs;

        private const string SAVE_FILE_NAME = "savegame.json";

        private void Awake() { Instance = this; }

        private void Update()
        {
            // Проверяем, что мы Сервер и клавиатура подключена
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer && Keyboard.current != null)
            {
                // F5 - Сохранение
                if (Keyboard.current.f5Key.wasPressedThisFrame)
                {
                    SaveGame();
                }
                // F9 - Загрузка
                if (Keyboard.current.f9Key.wasPressedThisFrame)
                {
                    LoadGame();
                }
            }
        }

        public void SaveGame()
        {
            Debug.Log("[SaveManager] Начало сохранения...");
            GameSaveData data = new GameSaveData();

            // 1. Мир
            data.world = new WorldSaveData();
            if (WorldGenerator.Instance) data.world.seed = WorldGenerator.Instance.GetCurrentSeed();

            // 2. Королевство (Хоста)
            data.hostKingdom = new KingdomSaveData();
            if (Kingdom.PlayerKingdom)
            {
                foreach (var pair in Kingdom.PlayerKingdom.GetAllInventory())
                {
                    data.hostKingdom.inventory.Add(new ResourceSaveEntry(pair.Key.ToString(), pair.Value));
                }
            }

            // 3. Юниты
            foreach (var unit in FindObjectsOfType<Unit>())
            {
                UnitSaveData uData = unit.GetSaveData();
                uData.prefabName = "Peasant"; // Жестко задаем, так как у нас пока только крестьяне
                data.units.Add(uData);
            }

            // 4. Здания
            foreach (var building in FindObjectsOfType<Building>())
            {
                BuildingSaveData bData = building.GetSaveData();
                bData.prefabName = building.gameObject.name.Replace("(Clone)", "").Trim();
                data.buildings.Add(bData);
            }

            // 5. Ресурсы
            foreach (var res in FindObjectsOfType<ResourceNode>())
            {
                ResourceNodeSaveData rData = new ResourceNodeSaveData();
                rData.prefabName = res.gameObject.name.Replace("(Clone)", "").Trim();
                rData.posX = res.transform.position.x;
                rData.posY = res.transform.position.y;
                rData.posZ = res.transform.position.z;
                rData.hitsLeft = res.currentHitsLeft;
                data.resources.Add(rData);
            }

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME), json);
            Debug.Log($"[SaveManager] Игра сохранена в: {Application.persistentDataPath}/{SAVE_FILE_NAME}");
        }

        public void LoadGame()
        {
            string path = Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
            if (!File.Exists(path))
            {
                Debug.LogError("[SaveManager] Файл сохранения не найден!");
                return;
            }

            Debug.Log("[SaveManager] Загрузка...");
            string json = File.ReadAllText(path);
            GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);

            // 1. Чистим сцену
            DespawnAllNetworkObjects();

            // 2. Восстанавливаем Мир
            if (WorldGenerator.Instance)
                WorldGenerator.Instance.RegenerateWorldFromSave(data.world.seed);

            // 3. Восстанавливаем Ресурсы
            foreach (var rData in data.resources)
            {
                GameObject prefab = FindPrefab(resourcePrefabs, rData.prefabName);
                if (prefab)
                {
                    GameObject obj = Instantiate(prefab, new Vector3(rData.posX, rData.posY, rData.posZ), Quaternion.identity);
                    if (obj.TryGetComponent<ResourceNode>(out var node)) node.currentHitsLeft = rData.hitsLeft;
                }
            }

            // 4. Восстанавливаем Здания
            foreach (var bData in data.buildings)
            {
                GameObject prefab = FindPrefab(buildingPrefabs, bData.prefabName);
                if (prefab)
                {
                    GameObject obj = Instantiate(prefab, new Vector3(bData.posX, bData.posY, bData.posZ), Quaternion.identity);
                    var netObj = obj.GetComponent<NetworkObject>();
                    if (netObj) netObj.Spawn();

                    var building = obj.GetComponent<Building>();
                    if (building)
                    {
                        building.SetOwnerID(bData.ownerID);
                        building.LoadFromData(bData);
                    }
                }
            }

            // 5. Восстанавливаем Юнитов
            foreach (var uData in data.units)
            {
                GameObject prefab = FindPrefab(unitPrefabs, "Peasant");
                if (prefab)
                {
                    GameObject obj = Instantiate(prefab, new Vector3(uData.posX, uData.posY, uData.posZ), Quaternion.identity);
                    var netObj = obj.GetComponent<NetworkObject>();
                    if (netObj) netObj.Spawn();

                    var unit = obj.GetComponent<Unit>();
                    if (unit)
                    {
                        unit.ownerKingdomID.Value = uData.ownerID;
                        unit.LoadFromData(uData);
                    }
                }
            }

            // 6. Экономика Хоста
            if (Kingdom.PlayerKingdom)
            {
                Kingdom.PlayerKingdom.LoadInventoryFromSave(data.hostKingdom.inventory);
            }

            Debug.Log("[SaveManager] Загрузка завершена!");
        }

        private void DespawnAllNetworkObjects()
        {
            // Удаляем сетевые объекты (Юниты, Здания)
            // Находим все NetworkObject в сцене
            var netObjects = FindObjectsOfType<NetworkObject>();
            foreach (var netObj in netObjects)
            {
                // Не удаляем сам NetworkManager и системные объекты
                if (netObj.GetComponent<NetworkManager>() != null) continue;
                if (netObj.GetComponent<LobbyController>() != null) continue;
                if (netObj.GetComponent<BuildManager>() != null) continue;

                if (netObj.IsSpawned) netObj.Despawn();
                else Destroy(netObj.gameObject);
            }

            // Удаляем локальные ресурсы
            var resources = FindObjectsOfType<ResourceNode>();
            foreach (var r in resources) Destroy(r.gameObject);
        }

        private GameObject FindPrefab(List<GameObject> list, string name)
        {
            if (list == null) return null;
            foreach (var p in list)
            {
                if (p == null) continue;
                // Точное совпадение
                if (p.name == name) return p;
            }
            // Частичное совпадение
            foreach (var p in list)
            {
                if (p == null) continue;
                if (p.name.Contains(name) || name.Contains(p.name)) return p;
            }
            Debug.LogWarning($"[SaveManager] Префаб не найден: {name}");
            return null;
        }
    }
}