using UnityEngine;
using WarOfCrowns.Core;
using WarOfCrowns.Data;

namespace WarOfCrowns.World
{
    public class ResourceNode : MonoBehaviour
    {
        public enum DepletionBehaviour { Destroy, Respawn }

        [Header("Настройки Ресурса")]
        public ResourceType resourceType;
        public string resourcePrefabName;

        [Header("Баланс Добычи")]
        public int totalResourceAmount = 50;
        public int hitsToBreak = 10;

        [Header("Настройки Истощения")]
        public DepletionBehaviour depletionBehaviour;
        [SerializeField] private GameObject depletedPrefab;
        [SerializeField] private string depletedPrefabName;
        [SerializeField] private float respawnTime = 60f;

        [HideInInspector] public int currentHitsLeft;
        [HideInInspector] public int resourcesGivenOut;
        [HideInInspector] public float accumulatedDrop;

        // --- НОВОЕ: Уникальный ID ---
        public string uniqueID;

        private void Awake()
        {
            if (currentHitsLeft == 0) currentHitsLeft = hitsToBreak;

            // Генерируем ID, если его нет (для новых объектов)
            if (string.IsNullOrEmpty(uniqueID)) uniqueID = System.Guid.NewGuid().ToString();
        }

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
            switch (depletionBehaviour)
            {
                case DepletionBehaviour.Destroy:
                    Destroy(gameObject);
                    break;

                case DepletionBehaviour.Respawn:
                    if (depletedPrefab != null && !string.IsNullOrEmpty(depletedPrefabName))
                    {
                        GameObject depletedObject = Instantiate(depletedPrefab, transform.position, transform.rotation);
                        depletedObject.AddComponent<RespawnController>().StartRespawning(depletedPrefabName, respawnTime);
                    }
                    Destroy(gameObject);
                    break;
            }
        }

        public ResourceNodeSaveData GetSaveData()
        {
            ResourceNodeSaveData data = new ResourceNodeSaveData();
            data.uniqueID = this.uniqueID; // <-- СОХРАНЯЕМ ID
            data.prefabName = !string.IsNullOrEmpty(resourcePrefabName) ? resourcePrefabName : gameObject.name.Replace("(Clone)", "").Trim();
            data.posX = transform.position.x;
            data.posY = transform.position.y;
            data.posZ = transform.position.z;
            data.hitsLeft = this.currentHitsLeft;
            data.accumulated = this.accumulatedDrop;
            data.givenOut = this.resourcesGivenOut;
            return data;
        }
        public bool IsReserved { get; private set; } = false;
        public void Reserve()
        {
            IsReserved = true;
        }

        public void Unreserve()
        {
            IsReserved = false;
        }
        private void OnDestroy()
        {
            IsReserved = false;
        }
        public void LoadFromData(ResourceNodeSaveData data)
        {
            this.uniqueID = data.uniqueID; // <-- ЗАГРУЖАЕМ ID
            this.currentHitsLeft = data.hitsLeft;
            this.accumulatedDrop = data.accumulated;
            this.resourcesGivenOut = data.givenOut;
        }
    }
}