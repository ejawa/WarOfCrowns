using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using Random = System.Random;

namespace WarOfCrowns.World
{
    [System.Serializable]
    public class ResourceSpawnRule
    {
        public string name;
        public GameObject prefab;
        public int totalAmount = 300;
        public int groupSizeMin = 1;
        public int groupSizeMax = 3;
        public float groupRadius = 3.0f;
        public List<string> allowedBiomes; // Например: "Grass", "Forest"
        public float itemSpacing = 1.0f;

        [Tooltip("Радиус проверки земли под объектом. 0.5 = 1 тайл.")]
        public float footprintRadius = 0.5f; // <-- ВАЖНЫЙ ПАРАМЕТР
    }

    public class ResourceSpawner : NetworkBehaviour
    {
        public static ResourceSpawner Instance { get; private set; }

        [Header("Настройки Спавна")]
        public List<ResourceSpawnRule> spawnRules;
        public LayerMask obstacleLayer;
        public float spawnSafeZoneRadius = 15f;

        private void Awake() { Instance = this; }

        public void SpawnAllResources(string seedString)
        {
            if (!IsServer) return;
            if (WorldGenerator.Instance == null) return;

            int seed = seedString.GetHashCode();
            Random prng = new Random(seed);

            Debug.Log($"[ResourceSpawner] Сетевой спавн ресурсов (Seed: {seed})...");

            foreach (var r in FindObjectsOfType<ResourceNode>())
            {
                if (r.TryGetComponent<NetworkObject>(out var no)) no.Despawn();
                else Destroy(r.gameObject);
            }

            int width = WorldGenerator.Instance.width;
            int height = WorldGenerator.Instance.height;

            int playersCount = 2;
            if (WarOfCrowns.Core.ConnectionManager.Instance != null)
                playersCount = WarOfCrowns.Core.ConnectionManager.Instance.ConnectedPlayers.Count;

            List<Vector3> playerSpawnPoints = new List<Vector3>();
            for (int i = 0; i < playersCount; i++)
            {
                playerSpawnPoints.Add(WorldGenerator.Instance.GetSpawnPosition(i));
            }

            foreach (var rule in spawnRules)
            {
                if (rule.prefab == null) continue;

                int spawnedCount = 0;
                int attempts = 0;
                int maxAttempts = rule.totalAmount * 50;

                while (spawnedCount < rule.totalAmount && attempts < maxAttempts)
                {
                    attempts++;

                    float centerX = (float)prng.NextDouble() * width - (width / 2f);
                    float centerY = (float)prng.NextDouble() * height - (height / 2f);
                    Vector3 groupCenter = new Vector3(centerX, centerY, 0);

                    // Проверка биома центра группы
                    if (!IsAreaValid(groupCenter, 0.1f, rule.allowedBiomes)) continue;

                    int sizeThisTime = prng.Next(rule.groupSizeMin, rule.groupSizeMax + 1);

                    for (int i = 0; i < sizeThisTime; i++)
                    {
                        if (spawnedCount >= rule.totalAmount) break;

                        for (int k = 0; k < 10; k++)
                        {
                            float angle = (float)prng.NextDouble() * Mathf.PI * 2;
                            float radius = Mathf.Sqrt((float)prng.NextDouble()) * rule.groupRadius;
                            Vector3 pos = groupCenter + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);

                            // --- ПРОВЕРКИ ---

                            // 1. ЖЕСТКАЯ ПРОВЕРКА ТАЙЛОВ ПО ПЛОЩАДИ
                            // Мы передаем footprintRadius, чтобы проверить не только точку, но и соседей
                            if (!IsAreaValid(pos, rule.footprintRadius, rule.allowedBiomes)) continue;

                            // 2. Препятствия (вода, другие ресурсы)
                            if (Physics2D.OverlapCircle(pos, rule.itemSpacing, obstacleLayer)) continue;

                            // 3. Безопасная зона
                            bool tooClose = false;
                            foreach (var spawnPoint in playerSpawnPoints)
                            {
                                if (Vector3.Distance(pos, spawnPoint) < spawnSafeZoneRadius)
                                {
                                    tooClose = true;
                                    break;
                                }
                            }
                            if (tooClose) continue;

                            // --- СПАВН ---
                            GameObject res = Instantiate(rule.prefab, pos, Quaternion.identity);
                            var netObj = res.GetComponent<NetworkObject>();
                            if (netObj != null)
                            {
                                netObj.Spawn();
                            }
                            else
                            {
                                Destroy(res);
                            }

                            spawnedCount++;
                            break;
                        }
                    }
                }
                Debug.Log($"[ResourceSpawner] Заспавнено {spawnedCount} объектов типа {rule.name}");
            }
        }

        // --- НОВЫЙ МЕТОД ПРОВЕРКИ ---
        private bool IsAreaValid(Vector3 centerPos, float radius, List<string> allowedBiomes)
        {
            if (WorldGenerator.Instance == null || WorldGenerator.Instance.baseTilemap == null) return false;

            var tilemap = WorldGenerator.Instance.baseTilemap;

            // Вычисляем охват в клетках
            // Если radius = 0.5, мы проверим центр и ближайших соседей, если стоим на краю
            Vector3Int minCell = tilemap.WorldToCell(centerPos - new Vector3(radius, radius, 0));
            Vector3Int maxCell = tilemap.WorldToCell(centerPos + new Vector3(radius, radius, 0));

            for (int x = minCell.x; x <= maxCell.x; x++)
            {
                for (int y = minCell.y; y <= maxCell.y; y++)
                {
                    // Получаем реальное имя биома из WorldGenerator
                    // Используем метод GetBiomeAtCell, который мы написали ранее
                    string biome = WorldGenerator.Instance.GetBiomeAtCell(new Vector3Int(x, y, 0));

                    // Если биом этой клетки НЕ входит в список разрешенных - запрещаем спавн
                    if (!allowedBiomes.Contains(biome))
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}