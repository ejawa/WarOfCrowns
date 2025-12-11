using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using WarOfCrowns.Core;
using Random = System.Random;

namespace WarOfCrowns.World
{
    [System.Serializable]
    public class ResourceSpawnRule
    {
        public string name;
        public GameObject prefab;
        [Tooltip("Это количество будет заспавнено в СЕКТОРЕ КАЖДОГО игрока.")]
        public int amountPerPlayerZone = 100;
        public List<string> allowedBiomes;
        public float itemSpacing = 1.0f;
    }

    public class ResourceSpawner : NetworkBehaviour
    {
        public static ResourceSpawner Instance { get; private set; }
        public bool IsSpawningComplete { get; private set; } = true; // <--- НОВЫЙ ФЛАГ

        [Header("Настройки Спавна")]
        public List<ResourceSpawnRule> spawnRules;
        public LayerMask obstacleLayer;

        [Header("Зоны Спавна")]
        [Tooltip("Не спавнить ресурсы ближе этого радиуса к стартовой ратуше игрока.")]
        public float spawnSafeZoneRadius = 15f;

        private void Awake() { Instance = this; }

        public void SpawnAllResources(string seedString)
        {
            if (!IsServer) return;
            IsSpawningComplete = false; // <--- Опускаем флаг перед стартом
            StartCoroutine(SpawnRoutine(seedString));
        }

        private IEnumerator SpawnRoutine(string seedString)
        {
            if (WorldGenerator.Instance == null)
            {
                IsSpawningComplete = true; // <--- Поднимаем флаг в случае ошибки
                yield break;
            }

            int seed = seedString.GetHashCode();
            Random prng = new Random(seed);

            Debug.Log($"[ResourceSpawner] Запуск спавна ресурсов по секторам (Seed: {seed})...");

            // Чистка
            foreach (var r in FindObjectsOfType<ResourceNode>())
            {
                if (r.TryGetComponent<NetworkObject>(out var no)) no.Despawn();
                else Destroy(r.gameObject);
            }
            yield return null;

            int playersCount = WorldGenerator.Instance.CurrentKingdomsCount;
            int width = WorldGenerator.Instance.width;
            int height = WorldGenerator.Instance.height;

            List<Vector3> playerBases = new List<Vector3>();
            for (int i = 0; i < playersCount; i++)
            {
                playerBases.Add(WorldGenerator.Instance.GetSpawnPosition(i));
            }

            foreach (var rule in spawnRules)
            {
                if (rule.prefab == null) continue;

                for (int playerID = 0; playerID < playersCount; playerID++)
                {
                    int spawnedForThisPlayer = 0;
                    int attempts = 0;
                    int maxAttempts = rule.amountPerPlayerZone * 200;

                    while (spawnedForThisPlayer < rule.amountPerPlayerZone && attempts < maxAttempts)
                    {
                        attempts++;

                        float angleDeg = (playerID * (360f / playersCount)) - (360f / (playersCount * 2)) + ((float)prng.NextDouble() * (360f / playersCount));
                        float dist = (float)prng.NextDouble() * (width / 2f);
                        float angleRad = angleDeg * Mathf.Deg2Rad;
                        Vector3 randomPos = new Vector3(Mathf.Cos(angleRad) * dist, Mathf.Sin(angleRad) * dist, 0);

                        if (Vector3.Distance(randomPos, playerBases[playerID]) < spawnSafeZoneRadius) continue;
                        if (!IsAreaValid(randomPos, 0.5f, rule.allowedBiomes)) continue;
                        if (Physics2D.OverlapCircle(randomPos, rule.itemSpacing, obstacleLayer)) continue;

                        GameObject res = Instantiate(rule.prefab, randomPos, Quaternion.identity);
                        var netObj = res.GetComponent<NetworkObject>();
                        if (netObj != null) netObj.Spawn();

                        spawnedForThisPlayer++;

                        if (spawnedForThisPlayer % 50 == 0)
                            yield return null;
                    }
                    if (attempts >= maxAttempts) Debug.LogWarning($"[ResourceSpawner] Не удалось заспавнить все {rule.name} для игрока {playerID}.");
                }
                Debug.Log($"[ResourceSpawner] Завершён спавн ресурса: {rule.name}");
            }

            IsSpawningComplete = true; // <--- Поднимаем флаг по завершении
            Debug.Log("[ResourceSpawner] Спавн всех ресурсов завершен.");
        }

        private bool IsAreaValid(Vector3 centerPos, float radius, List<string> allowedBiomes)
        {
            if (WorldGenerator.Instance == null) return false;
            string biome = WorldGenerator.Instance.GetBiomeAt(centerPos);
            return allowedBiomes.Contains(biome);
        }
    }
}