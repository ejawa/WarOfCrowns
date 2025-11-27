using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using WarOfCrowns.Buildings;
using WarOfCrowns.Data;
using WarOfCrowns.Units; // Нужно для переклички юнитов

namespace WarOfCrowns.Core
{
    [Serializable] public class InitialResourceEntry { public string type; public int amount; }
    [Serializable] public class InitialResourceDatabase { public List<InitialResourceEntry> resources; }

    public class Kingdom : MonoBehaviour
    {
        public int kingdomID;
        public static Kingdom PlayerKingdom { get; private set; }
        public event Action<ResourceType, int> OnResourceChanged;

        [Header("Экономика")]
        public List<ResourceType> foodPriorityList;

        private Dictionary<ResourceType, int> _inventory = new Dictionary<ResourceType, int>();

        private void Awake()
        {
            if (PlayerKingdom == null) PlayerKingdom = this;
            else { Destroy(gameObject); return; }

            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
                _inventory[type] = 0;

            LoadFromTemplate();
        }

        // --- ИСПРАВЛЕННЫЙ МЕТОД: Принимает ID ---
        public void InitializeKingdomLogic(int assignedID)
        {
            kingdomID = assignedID;
            Debug.Log($"[Kingdom] ID установлен: {kingdomID}");
        }

        // --- СБРОС НАСЕЛЕНИЯ (Для чистого старта) ---
        public void ResetPopulationLogic()
        {
            if (PopulationManager.Instance != null)
            {
                // 1. Очищаем список (забываем старых, если были)
                PopulationManager.Instance.ClearAllUnits();

                // 2. Сбрасываем лимит в 0 (Мэрия потом добавит свои +10 при спавне)
                PopulationManager.Instance.SetInitialPopulation(0, 0);
            }
        }

        // --- УМНОЕ СПИСАНИЕ ЕДЫ ---
        public bool TrySpendFood(int amountNeeded)
        {
            int found = 0;
            var toSpend = new Dictionary<ResourceType, int>();

            foreach (var type in foodPriorityList)
            {
                if (found >= amountNeeded) break;
                int stock = GetResourceAmount(type);
                if (stock > 0)
                {
                    int take = Mathf.Min(stock, amountNeeded - found);
                    toSpend.Add(type, take);
                    found += take;
                }
            }

            if (found >= amountNeeded)
            {
                foreach (var pair in toSpend) AddResource(pair.Key, -pair.Value);
                return true;
            }
            return false;
        }

        // --- БАЗОВЫЕ МЕТОДЫ ---
        private void LoadFromTemplate()
        {
            var file = Resources.Load<TextAsset>("InitialResources");
            if (file)
            {
                var db = JsonUtility.FromJson<InitialResourceDatabase>(file.text);
                foreach (var e in db.resources)
                {
                    if (Enum.TryParse(e.type, out ResourceType t)) _inventory[t] = e.amount;
                }
            }
        }

        public void AddResource(ResourceType t, int a)
        {
            if (!_inventory.ContainsKey(t)) _inventory[t] = 0;
            _inventory[t] += a;
            OnResourceChanged?.Invoke(t, _inventory[t]);
        }

        public int GetResourceAmount(ResourceType t) => _inventory.TryGetValue(t, out int a) ? a : 0;

        public Dictionary<ResourceType, int> GetAllInventory() => _inventory;

        public bool TrySpendResources(List<BuildingCost> costs)
        {
            if (costs == null) return true;
            foreach (var c in costs)
                if (GetResourceAmount(c.resourceType) < c.amount) return false;

            foreach (var c in costs)
                AddResource(c.resourceType, -c.amount);

            return true;
        }

        public void LoadInventoryFromSave(List<ResourceSaveEntry> list)
        {
            foreach (ResourceType t in Enum.GetValues(typeof(ResourceType))) _inventory[t] = 0;
            foreach (var e in list)
            {
                if (Enum.TryParse(e.type, out ResourceType p))
                {
                    _inventory[p] = e.amount;
                    OnResourceChanged?.Invoke(p, e.amount);
                }
            }
        }
    }
}