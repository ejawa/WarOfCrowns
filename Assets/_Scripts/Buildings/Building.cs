using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;
using WarOfCrowns.Core;
using WarOfCrowns.Data;
using WarOfCrowns.Units;

namespace WarOfCrowns.Buildings
{
    [System.Serializable]
    public class BuildingCost { public ResourceType resourceType; public int amount; }

    public class Building : NetworkBehaviour
    {
        [Header("Информация")]
        public string buildingName;
        public Sprite buildingIcon;
        [TextArea] public string description;

        [Header("Стоимость")]
        public List<BuildingCost> costs;

        [Header("Экономика")]
        public int populationBonus = 0;

        [Header("Вход")]
        public Transform entrancePoint;

        [Header("Визуал")] // <--- НОВЫЙ РАЗДЕЛ
        [Tooltip("Перетащи сюда SpriteRenderer крыши или другого окрашиваемого элемента.")]
        [SerializeField] private SpriteRenderer roofRenderer; // <--- НОВОЕ ПОЛЕ

        public NetworkVariable<int> ownerKingdomID = new NetworkVariable<int>(-1);
        [HideInInspector] public Kingdom OwningKingdom;
        public string uniqueID;

        private bool _popApplied = false;
        private List<Unit> _unitsInside = new List<Unit>();

        private void Awake()
        {
            if (string.IsNullOrEmpty(uniqueID)) uniqueID = System.Guid.NewGuid().ToString();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            CheckPopulationRegistration(); // Здесь же обновим и цвет
            ownerKingdomID.OnValueChanged += (o, n) => CheckPopulationRegistration();
        }

        public void EnterUnit(Unit unit)
        {
            if (!_unitsInside.Contains(unit))
            {
                _unitsInside.Add(unit);
                unit.SetVisibility(false);
            }
        }

        public void ExitUnit(Unit unit)
        {
            if (_unitsInside.Contains(unit))
            {
                _unitsInside.Remove(unit);
                unit.SetVisibility(true);
                if (entrancePoint) unit.transform.position = entrancePoint.position;
                else unit.transform.position = transform.position + Vector3.down;
            }
        }

        public void CheckPopulationRegistration()
        {
            UpdateKingdomReference();

            // --- ОБНОВЛЕНИЕ ЦВЕТА КРЫШИ ---
            if (roofRenderer != null && OwningKingdom != null)
            {
                Color kColor = OwningKingdom.kingdomColor.Value;
                Color desaturatedColor = Color.Lerp(Color.white, kColor, 0.8f);
                desaturatedColor.a = 1f; // <--- ИЗМЕНЕНИЕ: Форсируем непрозрачность
                roofRenderer.color = desaturatedColor;
            }
            // --------------------------------

            if (Kingdom.PlayerKingdom == null || PopulationManager.Instance == null) return;
            if (ownerKingdomID.Value == -1) return;
            if (GetComponent<ConstructionSite>() != null) return;

            if (ownerKingdomID.Value == Kingdom.PlayerKingdom.kingdomID.Value)
            {
                if (!_popApplied && populationBonus > 0)
                {
                    PopulationManager.Instance.AddPopulationCap(populationBonus);
                    _popApplied = true;
                }
            }
            else
            {
                if (_popApplied && populationBonus > 0)
                {
                    PopulationManager.Instance.AddPopulationCap(-populationBonus);
                    _popApplied = false;
                }
            }
        }

        private void UpdateKingdomReference()
        {
            if (ownerKingdomID.Value == -1)
            {
                OwningKingdom = null;
                return;
            }

            // Если ссылка уже есть и верна, ничего не делаем
            if (OwningKingdom != null && OwningKingdom.kingdomID.Value == ownerKingdomID.Value) return;

            // Ищем нужный объект Kingdom в сцене
            OwningKingdom = Kingdom.GetKingdomByID(ownerKingdomID.Value);
        }

        // ... (остальной код Demolish, Save/Load и т.д. без изменений) ...

        public void Demolish() { DemolishServerRpc(); }

        [ServerRpc(RequireOwnership = false)]
        private void DemolishServerRpc(ServerRpcParams rpcParams = default)
        {
            int senderID = (int)rpcParams.Receive.SenderClientId;
            if (OwnerClientId != (ulong)senderID && ownerKingdomID.Value != senderID) return;

            if (OwningKingdom != null && costs != null)
            {
                float refundMultiplier = 1.0f;
                if (TryGetComponent<ConstructionSite>(out var site)) refundMultiplier = site.GetProgressRatio();
                foreach (var cost in costs)
                {
                    int amountToRefund = Mathf.FloorToInt(cost.amount * refundMultiplier);
                    if (amountToRefund > 0) OwningKingdom.AddResource(cost.resourceType, amountToRefund);
                }
            }
            GetComponent<NetworkObject>().Despawn();
        }

        public List<Unit> GetUnitsInside() => _unitsInside;

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            if (_unitsInside == null) return;

            foreach (var unit in _unitsInside)
            {
                if (unit != null)
                {
                    unit.SetVisibility(true);
                    unit.currentBuildingNetID.Value = 0;
                }
            }
            _unitsInside.Clear();

            if (_popApplied && PopulationManager.Instance != null)
            {
                PopulationManager.Instance.AddPopulationCap(-populationBonus);
                _popApplied = false;
            }
        }

        public void SetOwnerID(int id)
        {
            if (IsServer)
            {
                ownerKingdomID.Value = id;
            }
        }

        public BuildingSaveData GetSaveData()
        {
            BuildingSaveData data = new BuildingSaveData();
            data.uniqueID = this.uniqueID;
            data.prefabName = gameObject.name.Replace("(Clone)", "").Trim();
            data.ownerID = ownerKingdomID.Value;
            data.posX = transform.position.x; data.posY = transform.position.y; data.posZ = transform.position.z;
            if (TryGetComponent<ConstructionSite>(out var site)) { data.isConstructionSite = true; data.constructionProgress = site.GetProgress(); }
            else if (TryGetComponent<JobBuilding>(out var job)) { data.isConstructionSite = false; data.productionProgress = job.GetProgress(); }
            else if (TryGetComponent<Smithy>(out var smithy)) { data.isConstructionSite = false; data.activeRecipeIndex = smithy.GetCurrentRecipeIndex(); data.craftingTimer = smithy.GetCurrentTimer(); }
            return data;
        }

        public void LoadFromData(BuildingSaveData data)
        {
            this.uniqueID = data.uniqueID;
            if (data.isConstructionSite) { if (TryGetComponent<ConstructionSite>(out var site)) site.SetProgress(data.constructionProgress); }
            else { if (TryGetComponent<JobBuilding>(out var job)) job.SetProgress(data.productionProgress); }
            if (TryGetComponent<Smithy>(out var smithy)) smithy.LoadState(data.activeRecipeIndex, data.craftingTimer);
        }
    }
}