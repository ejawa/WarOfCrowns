using UnityEngine;
using Unity.Netcode;
using WarOfCrowns.Core;
using WarOfCrowns.Data;

namespace WarOfCrowns.World
{
    public class ResourceNode : NetworkBehaviour // <-- Добавил NetworkBehaviour на всякий случай
    {
        public enum DepletionBehaviour { Destroy, Respawn }

        [Header("Настройки Ресурса")]
        public ResourceType resourceType;
        public string resourcePrefabName;

        [Header("Баланс Добычи")]
        public int totalResourceAmount = 50;
        public int hitsToBreak = 10;

        // --- НОВОЕ: Лимит рабочих ---
        [Header("Лимиты")]
        [Tooltip("Сколько юнитов могут одновременно добывать этот ресурс")]
        public int maxWorkers = 2;
        private int _currentWorkers = 0;
        // ----------------------------

        [Header("Настройки Истощения")]
        public DepletionBehaviour depletionBehaviour;
        [SerializeField] private GameObject depletedPrefab;
        [SerializeField] private string depletedPrefabName;
        [SerializeField] private float respawnTime = 60f;

        [HideInInspector] public int currentHitsLeft;
        [HideInInspector] public int resourcesGivenOut;
        [HideInInspector] public float accumulatedDrop;
        public int CurrentWorkers => _currentWorkers;
        public string uniqueID;

        private void Awake()
        {
            if (currentHitsLeft == 0) currentHitsLeft = hitsToBreak;
            if (string.IsNullOrEmpty(uniqueID)) uniqueID = System.Guid.NewGuid().ToString();
        }

        // --- ЛОГИКА БРОНИРОВАНИЯ ---
        public bool CanReserve()
        {
            return _currentWorkers < maxWorkers;
        }

        public bool TryReserve()
        {
            if (_currentWorkers < maxWorkers)
            {
                _currentWorkers++;
                return true;
            }
            return false;
        }

        public void Unreserve()
        {
            _currentWorkers--;
            if (_currentWorkers < 0) _currentWorkers = 0;
        }
        // ---------------------------

        public int TakeHit()
        {
            if (currentHitsLeft <= 0) return 0;

            currentHitsLeft--;
            if (currentHitsLeft <= 0)
            {
                int remaining = totalResourceAmount - resourcesGivenOut;
                resourcesGivenOut += remaining;
                Deplete();
                return remaining;
            }

            float theoreticalDropPerHit = (float)totalResourceAmount / hitsToBreak;
            accumulatedDrop += theoreticalDropPerHit;

            int amountToGive = Mathf.FloorToInt(accumulatedDrop);
            if (amountToGive > 0)
            {
                accumulatedDrop -= amountToGive;
                resourcesGivenOut += amountToGive;
            }
            return amountToGive;
        }

        private void Deplete()
        {
            // Сначала всех выгоняем, так как ресурса больше нет
            // (Логика отмены действий уже есть в UnitAI, когда ресурс станет null)

            switch (depletionBehaviour)
            {
                case DepletionBehaviour.Destroy:
                    Destroy(gameObject);
                    break;
                case DepletionBehaviour.Respawn:
                    if (depletedPrefab != null && !string.IsNullOrEmpty(depletedPrefabName))
                    {
                        GameObject depletedObject = Instantiate(depletedPrefab, transform.position, transform.rotation);
                        var netObj = depletedObject.GetComponent<NetworkObject>();
                        if (netObj) netObj.Spawn(); // Важно спавнить

                        depletedObject.AddComponent<RespawnController>().StartRespawning(depletedPrefabName, respawnTime);
                    }
                    // Деспавним текущий объект (сетевой)
                    if (TryGetComponent<NetworkObject>(out var myNetObj)) myNetObj.Despawn();
                    else Destroy(gameObject);
                    break;
            }
        }

        public ResourceNodeSaveData GetSaveData()
        {
            ResourceNodeSaveData data = new ResourceNodeSaveData();
            data.uniqueID = this.uniqueID;
            data.prefabName = !string.IsNullOrEmpty(resourcePrefabName) ? resourcePrefabName : gameObject.name.Replace("(Clone)", "").Trim();
            data.posX = transform.position.x;
            data.posY = transform.position.y;
            data.posZ = transform.position.z;
            data.hitsLeft = this.currentHitsLeft;
            data.accumulated = this.accumulatedDrop;
            data.givenOut = this.resourcesGivenOut;
            return data;
        }

        public void LoadFromData(ResourceNodeSaveData data)
        {
            this.uniqueID = data.uniqueID;
            this.currentHitsLeft = data.hitsLeft;
            this.accumulatedDrop = data.accumulated;
            this.resourcesGivenOut = data.givenOut;
        }

        public override void OnNetworkDespawn()
        {
            _currentWorkers = 0; // Сброс при уничтожении
            base.OnNetworkDespawn();
        }
    }
}