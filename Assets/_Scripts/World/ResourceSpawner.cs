using UnityEngine;
using System.Collections.Generic;

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
        [Tooltip("Радиус разброса группы")]
        public float groupRadius = 2.0f;

        [Header("Правила размещения")]
        public List<string> allowedBiomes;

        [Tooltip("Минимальное расстояние между предметами")]
        public float itemSpacing = 0.8f;

        // --- НОВАЯ НАСТРОЙКА ---
        [Tooltip("Радиус основания предмета. Скрипт проверит, чтобы земля была валидной в радиусе X метров вокруг центра.")]
        public float footprintRadius = 0.4f;
    }

    public class ResourceSpawner : MonoBehaviour
    {
        public static ResourceSpawner Instance;

        [Header("Настройки Спавна")]
        public List<ResourceSpawnRule> spawnRules;

        [Header("Слои препятствий")]
        public LayerMask obstacleLayer;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            Invoke(nameof(SpawnAllResources), 0.5f);
        }

        public void SpawnAllResources()
        {
            if (WorldGenerator.Instance == null) return;

            Debug.Log("ResourceSpawner v5.0: Проверка основания (Footprint)...");

            ResourceNode[] existing = FindObjectsOfType<ResourceNode>();
            foreach (var r in existing) Destroy(r.gameObject);

            int width = WorldGenerator.Instance.width;
            int height = WorldGenerator.Instance.height;
            if (FindObjectsOfType<ResourceNode>().Length > 100)
            {
                Debug.Log("ResourceSpawner: Ресурсы уже есть (вероятно, загрузка сохранения). Пропуск спавна.");
                return;
            }

            foreach (var rule in spawnRules)
            {
                if (rule.prefab == null) continue;

                int currentCount = 0;
                int globalAttempts = 0;
                int maxGlobalAttempts = rule.totalAmount * 50;

                while (currentCount < rule.totalAmount && globalAttempts < maxGlobalAttempts)
                {
                    globalAttempts++;

                    // 1. Центр новой группы
                    float centerX = Random.Range(-width / 2f, width / 2f);
                    float centerY = Random.Range(-height / 2f, height / 2f);
                    Vector3 groupCenter = new Vector3(centerX, centerY, 0);

                    // Грубая проверка центра группы
                    if (!IsBiomeAllowed(WorldGenerator.Instance.GetBiomeAt(groupCenter), rule)) continue;

                    // 2. Размер кучки
                    int sizeThisTime = Random.Range(rule.groupSizeMin, rule.groupSizeMax + 1);

                    // 3. Спавн отдельных предметов
                    for (int i = 0; i < sizeThisTime; i++)
                    {
                        if (currentCount >= rule.totalAmount) break;

                        for (int attempt = 0; attempt < 10; attempt++)
                        {
                            Vector2 randomOffset = Random.insideUnitCircle * rule.groupRadius;
                            Vector3 pos = groupCenter + new Vector3(randomOffset.x, randomOffset.y, 0);

                            // --- ГЛАВНОЕ ИСПРАВЛЕНИЕ: ПРОВЕРКА ПЯТНА ЗАСТРОЙКИ ---
                            // Мы проверяем, чтобы ВЕСЬ объект стоял на разрешенной земле, а не только его центр.
                            if (!IsFootprintSafe(pos, rule)) continue;

                            // Проверка физических препятствий
                            if (Physics2D.OverlapCircle(pos, rule.itemSpacing, obstacleLayer)) continue;

                            // Спавн
                            GameObject res = Instantiate(rule.prefab, pos, Quaternion.identity);
                            res.transform.parent = this.transform;
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

        // Проверяет 5 точек: центр + края
        private bool IsFootprintSafe(Vector3 center, ResourceSpawnRule rule)
        {
            float r = rule.footprintRadius;
            if (r <= 0) return IsBiomeAllowed(WorldGenerator.Instance.GetBiomeAt(center), rule);

            // Список точек для проверки (Крест)
            Vector3[] checkPoints = new Vector3[]
            {
                center,                         // Центр
                center + Vector3.up * r,        // Верхний край
                center + Vector3.down * r,      // Нижний край
                center + Vector3.left * r,      // Левый край
                center + Vector3.right * r      // Правый край
            };

            foreach (var p in checkPoints)
            {
                string biome = WorldGenerator.Instance.GetBiomeAt(p);
                // Если хотя бы одна точка попала в Воду или Гору (запрещенный биом) - возвращаем false
                if (!IsBiomeAllowed(biome, rule)) return false;
            }

            return true;
        }
    }
}