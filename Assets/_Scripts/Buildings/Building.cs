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
        public NetworkVariable<int> ownerKingdomID = new NetworkVariable<int>(-1);
        [HideInInspector] public Kingdom OwningKingdom;
        public string uniqueID;
        private bool _popApplied = false;
        [Header("Визуал")]
        [SerializeField] private SpriteRenderer roofRenderer;
        private void Awake()
        {
            if (string.IsNullOrEmpty(uniqueID)) uniqueID = System.Guid.NewGuid().ToString();
        }
        private List<Unit> _unitsInside = new List<Unit>();
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
                // Скрываем юнита визуально и отключаем коллизии
                unit.SetVisibility(false);
            }
        }

        public void ExitUnit(Unit unit)
        {
            if (_unitsInside.Contains(unit))
            {
                _unitsInside.Remove(unit);
                // Показываем юнита
                unit.SetVisibility(true);
                // Телепортируем на выход
                if (entrancePoint) unit.transform.position = entrancePoint.position;
                else unit.transform.position = transform.position + Vector3.down;
            }
        }
        public void CheckPopulationRegistration()
        {
            UpdateKingdomReference();

            // ОБНОВЛЕНИЕ ЦВЕТА КРЫШИ
            if (roofRenderer != null && OwningKingdom != null)
            {
                Color kColor = OwningKingdom.kingdomColor.Value;
                // Смешиваем 50/50 с белым, чтобы цвет был пастельным
                Color desaturatedColor = Color.Lerp(Color.white, kColor, 0.8f);
                // Или можно просто затемнить: kColor * 0.8f

                roofRenderer.color = desaturatedColor;
            }

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
            foreach (var k in FindObjectsOfType<Kingdom>())
            {
                if (k.kingdomID.Value == ownerKingdomID.Value)
                {
                    OwningKingdom = k;
                    break;
                }
            }
        }

        

        public void Demolish() { DemolishServerRpc(); }

        [ServerRpc(RequireOwnership = false)]
        private void DemolishServerRpc(ServerRpcParams rpcParams = default)
        {
            int senderID = (int)rpcParams.Receive.SenderClientId;
            // Проверка по ID подключения (более надежно)
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

            // Если сервер выключается, _unitsInside могут быть null, но если здание рушат в игре:
            foreach (var unit in _unitsInside)
            {
                if (unit != null)
                {
                    unit.SetVisibility(true);
                    // Сбрасываем статус "Внутри" у юнита
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
                // Сразу обновляем ссылку для сервера
                CheckPopulationRegistration();
            }
        }

        public BuildingSaveData GetSaveData()
        {
            BuildingSaveData data = new BuildingSaveData();
            data.uniqueID = this.uniqueID;
            data.prefabName = gameObject.name.Replace("(Clone)", "").Trim();
            data.ownerID = ownerKingdomID.Value; // .Value
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