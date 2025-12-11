using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using WarOfCrowns.Data;
using WarOfCrowns.Buildings;
using WarOfCrowns.UI;

namespace WarOfCrowns.Core
{
    [Serializable] public class InitialResourceEntry { public string type; public int amount; }
    [Serializable] public class InitialResourceDatabase { public List<InitialResourceEntry> resources; }

    public struct NetworkResource : INetworkSerializable, IEquatable<NetworkResource>
    {
        public int typeId;
        public int amount;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref typeId);
            serializer.SerializeValue(ref amount);
        }

        public bool Equals(NetworkResource other) => typeId == other.typeId && amount == other.amount;
    }

    public class Kingdom : NetworkBehaviour
    {
        // --- НОВОЕ: Глобальный реестр королевств ---
        public static Dictionary<int, Kingdom> ActiveKingdoms = new Dictionary<int, Kingdom>();
        // -------------------------------------------

        public static Kingdom PlayerKingdom { get; private set; }

        public event Action<ResourceType, int> OnResourceChanged;
        public event Action<float> OnLegitimacyChanged;

        public NetworkVariable<int> kingdomID = new NetworkVariable<int>(-1);
        public NetworkVariable<FixedString64Bytes> kingdomName = new NetworkVariable<FixedString64Bytes>("Loading...");
        public NetworkVariable<Color> kingdomColor = new NetworkVariable<Color>(Color.white);
        public NetworkVariable<float> legitimacy = new NetworkVariable<float>(100f);

        [Header("Настройки Легитимности")]
        [SerializeField] private float decayInterval = 5f;
        [SerializeField] private float decayAmountBase = 1f;
        [SerializeField] private float decayAmountCrisis = 2f;
        private float _decayTimer;

        [Header("Внешний вид")]
        [SerializeField] private List<Color> kingdomColorPalette;

        public NetworkList<int> enemiesList;
        public NetworkList<int> incomingPeaceOffers;
        private NetworkList<NetworkResource> _netInventory;
        private Dictionary<ResourceType, int> _inventory = new Dictionary<ResourceType, int>();

        [Header("Экономика")]
        public List<ResourceType> foodPriorityList;

        private void Awake()
        {
            enemiesList = new NetworkList<int>();
            // Инициализация нового списка
            incomingPeaceOffers = new NetworkList<int>();
            _netInventory = new NetworkList<NetworkResource>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                PlayerKingdom = this;
            }

            if (IsServer)
            {
                int id = (int)OwnerClientId;
                kingdomID.Value = id;
                kingdomName.Value = $"Kingdom {id + 1}";

                if (kingdomColorPalette != null && kingdomColorPalette.Count > 0)
                {
                    kingdomColor.Value = kingdomColorPalette[id % kingdomColorPalette.Count];
                }
                legitimacy.Value = 100f;
            }

            // --- НОВОЕ: Регистрируемся при изменении ID ---
            kingdomID.OnValueChanged += OnKingdomIDChanged;
            if (kingdomID.Value != -1) RegisterKingdom(kingdomID.Value);
            // ----------------------------------------------

            if (!IsServer)
            {
                _netInventory.OnListChanged += OnNetInventoryChanged;
            }

            legitimacy.OnValueChanged += (oldVal, newVal) => {
                if (IsOwner) OnLegitimacyChanged?.Invoke(newVal);
            };

            SyncInventoryLocal();
            if (IsOwner) Invoke(nameof(ForceUpdateUI), 0.5f);
        }

        public override void OnNetworkDespawn()
        {
            // --- НОВОЕ: Удаляемся из реестра ---
            if (kingdomID.Value != -1 && ActiveKingdoms.ContainsKey(kingdomID.Value))
            {
                ActiveKingdoms.Remove(kingdomID.Value);
            }
            kingdomID.OnValueChanged -= OnKingdomIDChanged;
            // -----------------------------------

            if (!IsServer)
            {
                _netInventory.OnListChanged -= OnNetInventoryChanged;
            }
        }

        private void OnKingdomIDChanged(int oldId, int newId)
        {
            if (oldId != -1 && ActiveKingdoms.ContainsKey(oldId)) ActiveKingdoms.Remove(oldId);
            RegisterKingdom(newId);
        }

        private void RegisterKingdom(int id)
        {
            if (id == -1) return;
            if (!ActiveKingdoms.ContainsKey(id))
            {
                ActiveKingdoms.Add(id, this);
                Debug.Log($"[KingdomRegistry] Зарегистрировано королевство ID: {id} ({kingdomName.Value})");
            }
            else
            {
                ActiveKingdoms[id] = this;
            }
        }

        // --- НОВЫЙ МЕТОД ПОЛУЧЕНИЯ (СУПЕР-БЫСТРЫЙ) ---
        public static Kingdom GetKingdomByID(int id)
        {
            if (ActiveKingdoms.TryGetValue(id, out var k)) return k;
            return null;
        }
        // ---------------------------------------------

        private void Update()
        {
            if (!IsServer) return;

            if (WorldState.Instance != null && WorldState.Instance.CurrentPhase.Value == WorldPhase.Game)
            {
                _decayTimer += Time.deltaTime;
                if (_decayTimer >= decayInterval)
                {
                    _decayTimer = 0;
                    float decay = (legitimacy.Value < 50f) ? decayAmountCrisis : decayAmountBase;
                    ModifyLegitimacy(-decay);
                }
            }
        }

        private void OnNetInventoryChanged(NetworkListEvent<NetworkResource> changeEvent)
        {
            SyncInventoryLocal();
        }

        private void SyncInventoryLocal()
        {
            _inventory.Clear();
            foreach (var netRes in _netInventory)
            {
                ResourceType t = (ResourceType)netRes.typeId;
                _inventory[t] = netRes.amount;
                if (IsOwner) OnResourceChanged?.Invoke(t, netRes.amount);
            }
            if (IsOwner) ForceUpdateUI();
        }

        public void AddResource(ResourceType t, int amountToAdd)
        {
            if (!IsServer) return;

            if (!_inventory.ContainsKey(t)) _inventory[t] = 0;
            _inventory[t] += amountToAdd;

            int typeId = (int)t;
            bool found = false;

            for (int i = 0; i < _netInventory.Count; i++)
            {
                if (_netInventory[i].typeId == typeId)
                {
                    if (_inventory[t] > 0)
                        _netInventory[i] = new NetworkResource { typeId = typeId, amount = _inventory[t] };
                    else
                        _netInventory.RemoveAt(i);
                    found = true;
                    break;
                }
            }

            if (!found && _inventory[t] > 0)
            {
                _netInventory.Add(new NetworkResource { typeId = typeId, amount = _inventory[t] });
            }

            if (IsOwner) OnResourceChanged?.Invoke(t, _inventory[t]);
        }

        public int GetResourceAmount(ResourceType t) => _inventory.TryGetValue(t, out int a) ? a : 0;

        public int GetTotalFoodAmount()
        {
            int total = 0;
            if (foodPriorityList == null) return 0;
            foreach (var foodType in foodPriorityList)
            {
                total += GetResourceAmount(foodType);
            }
            return total;
        }

        public bool TrySpendFood(int amountToSpend)
        {
            if (!IsServer) return false;
            if (GetTotalFoodAmount() < amountToSpend) return false;

            int remainingCost = amountToSpend;
            for (int i = foodPriorityList.Count - 1; i >= 0; i--)
            {
                ResourceType foodType = foodPriorityList[i];
                int available = GetResourceAmount(foodType);
                if (available > 0)
                {
                    int toSpend = Mathf.Min(remainingCost, available);
                    AddResource(foodType, -toSpend);
                    remainingCost -= toSpend;
                }
                if (remainingCost <= 0) break;
            }
            return true;
        }

        public bool SpendResourcesAtomic(List<BuildingCost> costs, float ratio)
        {
            if (!IsServer) return false;
            Dictionary<ResourceType, int> toDeduct = new Dictionary<ResourceType, int>();

            foreach (var cost in costs)
            {
                int tickCost = Mathf.CeilToInt(cost.amount * ratio);
                if (tickCost > 0)
                {
                    if (GetResourceAmount(cost.resourceType) < tickCost) return false;
                    toDeduct[cost.resourceType] = tickCost;
                }
            }

            foreach (var kvp in toDeduct) AddResource(kvp.Key, -kvp.Value);
            return true;
        }

        public bool TrySpendResources(List<BuildingCost> costs)
        {
            if (costs == null) return true;
            foreach (var c in costs) if (GetResourceAmount(c.resourceType) < c.amount) return false;
            if (IsServer) foreach (var c in costs) AddResource(c.resourceType, -c.amount);
            return true;
        }

        public void GrantStartingResources()
        {
            if (!IsServer) return;
            _netInventory.Clear();
            _inventory.Clear();

            var file = Resources.Load<TextAsset>("InitialResources");
            bool loaded = false;
            if (file != null)
            {
                try
                {
                    var db = JsonUtility.FromJson<InitialResourceDatabase>(file.text);
                    foreach (var e in db.resources)
                        if (Enum.TryParse(e.type, out ResourceType t)) AddResource(t, e.amount);
                    loaded = true;
                }
                catch { Debug.LogWarning("JSON Error loading initial resources."); }
            }

            if (!loaded)
            {
                AddResource(ResourceType.Gold, 100);
                AddResource(ResourceType.Wood, 150);
                AddResource(ResourceType.Stone, 100);
                AddResource(ResourceType.Food, 200);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void AddResourceFromUnitServerRpc(ResourceType type, int amount, ServerRpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != OwnerClientId) return;
            AddResource(type, amount);
        }

        [ServerRpc]
        public void RequestProducePeasantServerRpc(ulong townHallNetworkId, ServerRpcParams rpcParams = default)
        {
            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(townHallNetworkId, out var townHallObject)) return;
            var townHall = townHallObject.GetComponent<TownHall>();
            if (townHall == null) return;

            if (PopulationManager.Instance != null && PopulationManager.Instance.IsCapReached())
            {
                ClientRpcParams clientRpcParams = new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { OwnerClientId } } };
                NotifyClientNotEnoughResourcesClientRpc("жилья", clientRpcParams);
                return;
            }

            if (TrySpendFood(townHall.peasantFoodCost))
            {
                townHall.StartProductionRoutine(OwnerClientId);
            }
            else
            {
                ClientRpcParams clientRpcParams = new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { OwnerClientId } } };
                NotifyClientNotEnoughResourcesClientRpc("еды", clientRpcParams);
            }
        }

        [ClientRpc]
        public void NotifyClientNotEnoughResourcesClientRpc(string resourceName, ClientRpcParams clientRpcParams = default)
        {
            if (NotificationUI.Instance != null)
                NotificationUI.Instance.ShowNotification($"Недостаточно {resourceName}!", Color.yellow);
        }

        // --- ДЕБАГ ---
        [ServerRpc]
        public void Debug_RequestFoodAmountServerRpc()
        {
            int serverSideFoodAmount = GetResourceAmount(ResourceType.Food);
            Debug.LogWarning($"[SERVER] Client {OwnerClientId} asked for food. Have: {serverSideFoodAmount}");
        }

        public void ModifyLegitimacy(float amount)
        {
            if (!IsServer) return;
            legitimacy.Value = Mathf.Clamp(legitimacy.Value + amount, 0f, 100f);
        }

        public Dictionary<ResourceType, int> GetAllInventory() => _inventory;
        public bool IsAtWarWith(int id) => enemiesList.Contains(id);
        public void SetName(string n) { if (IsServer) kingdomName.Value = n; else SetNameServerRpc(n); }
        [ServerRpc] private void SetNameServerRpc(string n) => kingdomName.Value = n;

        public void ForceUpdateUI()
        {
            foreach (var kvp in _inventory) OnResourceChanged?.Invoke(kvp.Key, kvp.Value);
            OnLegitimacyChanged?.Invoke(legitimacy.Value);
        }

        public void LoadInventoryFromSave(List<ResourceSaveEntry> l) { }
    }
}