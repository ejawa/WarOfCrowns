using UnityEngine;
using System.Collections.Generic;
using Random = System.Random; // Системный рандом для синхронизации

namespace WarOfCrowns.World
{
    [System.Serializable]
    public class ResourceSpawnRule
    {
        public string name;
        public GameObject prefab;

        [Header("Количество")]
        public int totalAmount = 500;

        [Header("Настройки Группы")]
        public int groupSizeMin = 1;
        public int groupSizeMax = 1;
        public float groupRadius = 2.0f;

        [Header("Правила размещения")]
        public List<string> allowedBiomes;
        public float itemSpacing = 0.8f;
        public float footprintRadius = 0.4f;
    }

    public class ResourceSpawner : MonoBehaviour
    {
        public static ResourceSpawner Instance;

        [Header("Настройки Спавна")]
        public List<ResourceSpawnRule> spawnRules;
        public LayerMask obstacleLayer;

        private void Awake() { Instance = this; }

        // Вызывается из LobbyController
        public void SpawnAllResources(string seedString)
        {
            if (WorldGenerator.Instance == null) return;

            int seed = seedString.GetHashCode();
            Random prng = new Random(seed);

            Debug.Log($"ResourceSpawner: Заселяем мир (Sync Seed: {seed})...");

            // Очистка старых
            ResourceNode[] existing = FindObjectsOfType<ResourceNode>();
            foreach (var r in existing) Destroy(r.gameObject);

            int width = WorldGenerator.Instance.width;
            int height = WorldGenerator.Instance.height;

            foreach (var rule in spawnRules)
            {
                if (rule.prefab == null) continue;

                int currentCount = 0;
                int globalAttempts = 0;
                int maxGlobalAttempts = rule.totalAmount * 50;

                while (currentCount < rule.totalAmount && globalAttempts < maxGlobalAttempts)
                {
                    globalAttempts++;

                    // 1. Центр группы
                    float centerX = (float)prng.NextDouble() * width - (width / 2f);
                    float centerY = (float)prng.NextDouble() * height - (height / 2f);
                    Vector3 groupCenter = new Vector3(centerX, centerY, 0);

                    string centerBiome = WorldGenerator.Instance.GetBiomeAt(groupCenter);
                    if (!IsBiomeAllowed(centerBiome, rule)) continue;

                    // 2. Размер
                    int sizeThisTime = prng.Next(rule.groupSizeMin, rule.groupSizeMax + 1);

                    // 3. Предметы
                    for (int i = 0; i < sizeThisTime; i++)
                    {
                        if (currentCount >= rule.totalAmount) break;

                        for (int attempt = 0; attempt < 10; attempt++)
                        {
                            float angle = (float)prng.NextDouble() * Mathf.PI * 2;
                            float radius = Mathf.Sqrt((float)prng.NextDouble()) * rule.groupRadius;
                            float offX = Mathf.Cos(angle) * radius;
                            float offY = Mathf.Sin(angle) * radius;

                            Vector3 pos = groupCenter + new Vector3(offX, offY, 0);

                            if (!IsFootprintSafe(pos, rule)) continue;
                            if (Physics2D.OverlapCircle(pos, rule.itemSpacing, obstacleLayer)) continue;

                            // Спавн (ЛОКАЛЬНО, так как ресурсы не сетевые объекты)
                            GameObject res = Instantiate(rule.prefab, pos, Quaternion.identity);
                            // res.transform.parent = this.transform; // <-- ЭТУ СТРОКУ УБРАЛИ СПЕЦИАЛЬНО
                            res.name = rule.prefab.name;

                            currentCount++;
                            break;
                        }
                    }
                }
                Debug.Log($"Spawned {currentCount} of {rule.name}");
            }
        }

        private bool IsBiomeAllowed(string biome, ResourceSpawnRule rule)
        {
            return rule.allowedBiomes.Contains(biome);
        }

        private bool IsFootprintSafe(Vector3 center, ResourceSpawnRule rule)
        {
            float r = rule.footprintRadius;
            if (r <= 0) return IsBiomeAllowed(WorldGenerator.Instance.GetBiomeAt(center), rule);

            Vector3[] checkPoints = new Vector3[]
            {
                center,
                center + Vector3.up * r,
                center + Vector3.down * r,
                center + Vector3.left * r,
                center + Vector3.right * r
            };

            foreach (var p in checkPoints)
            {
                string biome = WorldGenerator.Instance.GetBiomeAt(p);
                if (!IsBiomeAllowed(biome, rule)) return false;
            }
            return true;
        }
    }
}