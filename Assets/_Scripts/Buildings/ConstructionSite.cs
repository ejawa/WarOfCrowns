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
                    iconRenderer.color = new Color(1f, 1f, 1f, 0.7f);
                    iconRenderer.gameObject.SetActive(true);
                }
                else
                {
                    iconRenderer.sprite = null;
                    iconRenderer.gameObject.SetActive(false);
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

            bool canAfford = true;
            if (_totalCosts != null)
            {
                foreach (var cost in _totalCosts)
                {
                    int tickCost = Mathf.CeilToInt(((float)cost.amount / buildTime) * amount);
                    if (OwningKingdom.GetResourceAmount(cost.resourceType) < tickCost) { canAfford = false; break; }
                }
            }

            if (canAfford)
            {
                if (_totalCosts != null)
                {
                    foreach (var cost in _totalCosts)
                    {
                        int tickCost = Mathf.CeilToInt(((float)cost.amount / buildTime) * amount);
                        if (tickCost > 0) OwningKingdom.AddResource(cost.resourceType, -tickCost);
                    }
                }
                buildProgressNet.Value += amount;
            }
            else return false;

            if (buildProgressNet.Value >= buildTime)
            {
                FinishConstruction();
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

                if (finalBuilding.TryGetComponent<TownHall>(out var townHall))
                {
                    townHall.OwningKingdom = OwningKingdom;
                }
            }
            GetComponent<NetworkObject>().Despawn();
        }

        public float GetProgress() => buildProgressNet.Value;
        public void SetProgress(float value) { if (IsServer) buildProgressNet.Value = value; }
    }
}