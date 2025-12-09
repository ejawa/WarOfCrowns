using UnityEngine;
using Unity.Netcode;
using WarOfCrowns.Core;
using System.Collections.Generic;

namespace WarOfCrowns.Buildings
{
    [RequireComponent(typeof(Building))]
    public class ConstructionSite : NetworkBehaviour
    {
        [Header("Настройки Строительства")]
        [SerializeField] private float buildTime = 10f;
        [SerializeField] private GameObject finishedBuildingPrefab;
        [SerializeField] private SpriteRenderer iconRenderer;

        private NetworkVariable<float> buildProgressNet = new NetworkVariable<float>(0f);
        public Kingdom OwningKingdom => GetComponent<Building>().OwningKingdom;
        private List<BuildingCost> _totalCosts;

        private void Start()
        {
            var myBuildingData = GetComponent<Building>();
            _totalCosts = myBuildingData.costs;

            if (iconRenderer != null)
            {
                Sprite iconToUse = null;
                if (myBuildingData.buildingIcon != null) iconToUse = myBuildingData.buildingIcon;
                else if (finishedBuildingPrefab != null)
                {
                    var fd = finishedBuildingPrefab.GetComponent<Building>();
                    if (fd != null) iconToUse = fd.buildingIcon;
                }

                if (iconToUse != null)
                {
                    iconRenderer.sprite = iconToUse;
                    iconRenderer.color = Color.white;
                    iconRenderer.gameObject.SetActive(true);
                }
            }
        }

        public bool AddBuildProgress(float progressAmount)
        {
            if (!IsServer)
            {
                AddProgressServerRpc(progressAmount);
                return false;
            }
            return ApplyBuildProgress(progressAmount);
        }

        [ServerRpc(RequireOwnership = false)]
        private void AddProgressServerRpc(float amount)
        {
            ApplyBuildProgress(amount);
        }

        private bool ApplyBuildProgress(float amount)
        {
            if (OwningKingdom == null) return false;

            // Вычисляем, какую долю от общего времени мы сейчас добавляем
            // amount (сек) / buildTime (сек) = доля (0.0 - 1.0)
            float ratio = 0f;
            if (buildTime > 0) ratio = amount / buildTime;

            bool canBuild = true;

            // Если есть стоимость - пытаемся списать ресурсы атомарно
            if (_totalCosts != null && _totalCosts.Count > 0)
            {
                // Метод вернет true только если ресурсов хватило и они списались
                canBuild = OwningKingdom.SpendResourcesAtomic(_totalCosts, ratio);
            }

            if (canBuild)
            {
                buildProgressNet.Value += amount;

                if (buildProgressNet.Value >= buildTime)
                {
                    FinishConstruction();
                }
                return true;
            }

            return false;
        }

        private void FinishConstruction()
        {
            if (!IsServer) return;

            if (finishedBuildingPrefab != null)
            {
                GameObject finalBuilding = Instantiate(finishedBuildingPrefab, transform.position, transform.rotation);
                var netObj = finalBuilding.GetComponent<NetworkObject>();

                if (netObj != null) netObj.Spawn();

                var bLogic = finalBuilding.GetComponent<Building>();
                var myB = GetComponent<Building>();
                if (bLogic != null && myB != null)
                {
                    bLogic.SetOwnerID(myB.ownerKingdomID.Value);
                }
            }
            GetComponent<NetworkObject>().Despawn();
        }

        public float GetProgressRatio() { if (buildTime <= 0) return 0; return Mathf.Clamp01(buildProgressNet.Value / buildTime); }
        public float GetProgress() => buildProgressNet.Value;
        public void SetProgress(float value) { if (IsServer) buildProgressNet.Value = value; }
    }
}