using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using WarOfCrowns.Data;
using WarOfCrowns.Buildings;

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
        public static Kingdom PlayerKingdom { get; private set; }
        public event Action<ResourceType, int> OnResourceChanged;

        public NetworkVariable<int> kingdomID = new NetworkVariable<int>(-1);
        public NetworkVariable<FixedString64Bytes> kingdomName = new NetworkVariable<FixedString64Bytes>("Loading...");
        public NetworkVariable<Color> kingdomColor = new NetworkVariable<Color>(Color.white);

        public NetworkList<int> enemiesList;
        private NetworkList<NetworkResource> _netInventory;
        private Dictionary<ResourceType, int> _inventory = new Dictionary<ResourceType, int>();

        [Header("Экономика")]
        public List<ResourceType> foodPriorityList;

        private void Awake()
        {
            enemiesList = new NetworkList<int>();
            _netInventory = new NetworkList<NetworkResource>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                PlayerKingdom = this;
                Invoke(nameof(ForceUpdateUI), 0.5f);
            }

            if (IsServer)
            {
                int id = (int)OwnerClientId;
                kingdomID.Value = id;
                kingdomName.Value = $"Kingdom {id + 1}";

                if (id == 0) kingdomColor.Value = new Color(0.2f, 0.4f, 1.0f); // Синий
                else if (id == 1) kingdomColor.Value = new Color(1.0f, 0.3f, 0.3f); // Красный
                else kingdomColor.Value = Color.yellow;
            }

            _netInventory.OnListChanged += OnNetInventoryChanged;
            SyncInventoryLocal();
        }

        public override void OnNetworkDespawn()
        {
            _netInventory.OnListChanged -= OnNetInventoryChanged;
        }

        public void InitializeKingdom(int id)
        {
            if (!IsServer) return;
            kingdomID.Value = id;
            kingdomName.Value = $"Kingdom {id + 1}";
        }

        private void OnNetInventoryChanged(NetworkListEvent<NetworkResource> changeEvent) => SyncInventoryLocal();

        private void SyncInventoryLocal()
        {
            foreach (var netRes in _netInventory)
            {
                ResourceType t = (ResourceType)netRes.typeId;
                _inventory[t] = netRes.amount;
                OnResourceChanged?.Invoke(t, netRes.amount);
            }
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
                catch { Debug.LogWarning("JSON Error"); }
            }

            if (!loaded)
            {
                AddResource(ResourceType.Gold, 100);
                AddResource(ResourceType.Wood, 150);
                AddResource(ResourceType.Stone, 100);
                AddResource(ResourceType.Food, 200);
            }
            SyncInventoryLocal();
        }

        public void AddResource(ResourceType t, int amountToAdd)
        {
            if (!IsServer) return;

            if (!_inventory.ContainsKey(t)) _inventory[t] = 0;
            _inventory[t] += amountToAdd;
            if (IsOwner) OnResourceChanged?.Invoke(t, _inventory[t]);

            int typeId = (int)t;
            bool found = false;
            for (int i = 0; i < _netInventory.Count; i++)
            {
                if (_netInventory[i].typeId == typeId)
                {
                    _netInventory[i] = new NetworkResource { typeId = typeId, amount = _inventory[t] };
                    found = true;
                    break;
                }
            }
            if (!found) _netInventory.Add(new NetworkResource { typeId = typeId, amount = _inventory[t] });
        }

        // --- НОВЫЙ МЕТОД: АТОМАРНОЕ СПИСАНИЕ ---
        public bool SpendResourcesAtomic(List<BuildingCost> costs, float ratio)
        {
            if (!IsServer) return false;

            // 1. Предварительный расчет сколько нужно
            // Используем словарь для хранения того, сколько списать, чтобы не менять инвентарь раньше времени
            Dictionary<ResourceType, int> toDeduct = new Dictionary<ResourceType, int>();

            foreach (var cost in costs)
            {
                // Вычисляем, сколько ресурса нужно на этот "кусочек" прогресса
                // Например, цена 100, прогресс 1%. Нужно 1 ед.
                // Если прогресс очень маленький, tickCost будет 0. Это нормально (бесплатный тик).
                int tickCost = Mathf.CeilToInt(cost.amount * ratio);

                if (tickCost > 0)
                {
                    // Проверяем, хватает ли
                    if (GetResourceAmount(cost.resourceType) < tickCost)
                    {
                        return false; // Не хватает одного из ресурсов - отмена всего
                    }
                    toDeduct[cost.resourceType] = tickCost;
                }
            }

            // 2. Если мы здесь, значит всего хватает. Списываем.
            foreach (var kvp in toDeduct)
            {
                AddResource(kvp.Key, -kvp.Value);
            }

            return true;
        }

        public int GetResourceAmount(ResourceType t) => _inventory.TryGetValue(t, out int a) ? a : 0;
        public Dictionary<ResourceType, int> GetAllInventory() => _inventory;
        public bool IsAtWarWith(int id) => enemiesList.Contains(id);
        public void SetName(string n) { if (IsServer) kingdomName.Value = n; else SetNameServerRpc(n); }
        [ServerRpc] private void SetNameServerRpc(string n) => kingdomName.Value = n;

        // Старый метод для разовой проверки (например, для крафта)
        public bool TrySpendResources(List<BuildingCost> costs)
        {
            if (costs == null) return true;
            foreach (var c in costs) if (GetResourceAmount(c.resourceType) < c.amount) return false;
            if (IsServer) foreach (var c in costs) AddResource(c.resourceType, -c.amount);
            return true;
        }

        public bool TrySpendFood(int amount)
        {
            if (GetResourceAmount(ResourceType.Food) >= amount)
            {
                if (IsServer) AddResource(ResourceType.Food, -amount);
                return true;
            }
            return false;
        }

        public static Kingdom GetKingdomByID(int id)
        {
            foreach (var k in FindObjectsOfType<Kingdom>())
                if (k.kingdomID.Value == id) return k;
            return null;
        }

        public void ForceUpdateUI() { foreach (var kvp in _inventory) OnResourceChanged?.Invoke(kvp.Key, kvp.Value); }
        public void LoadInventoryFromSave(List<ResourceSaveEntry> l) { }
        public void InitializeKingdomLogic(int id) { }
    }
}