using System;
using System.Collections.Generic;
using UnityEngine;
using WarOfCrowns.Buildings;

namespace WarOfCrowns.Core
{
    // Классы для чтения JSON
    [Serializable]
    public class ResourceEntry
    {
        public string type;
        public int amount;
    }

    [Serializable]
    public class ResourceDatabase
    {
        public List<ResourceEntry> resources;
    }

    public class Kingdom : MonoBehaviour
    {
        public int kingdomID;
        public static Kingdom PlayerKingdom { get; private set; }

        public event Action<ResourceType, int> OnResourceChanged;

        // Наш инвентарь
        private Dictionary<ResourceType, int> _inventory = new Dictionary<ResourceType, int>();

        private void Awake()
        {
            // 1. Регистрация
            if (kingdomID == 0)
            {
                if (PlayerKingdom == null) PlayerKingdom = this;
                else { Destroy(gameObject); return; }
            }

            // 2. Инициализация пустыми значениями (чтобы не было ошибок KeyNotFound)
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                _inventory[type] = 0;
            }

            // 3. Загрузка из JSON
            LoadResourcesFromJSON();
        }

        private void LoadResourcesFromJSON()
        {
            TextAsset jsonFile = Resources.Load<TextAsset>("InitialResources");

            if (jsonFile != null)
            {
                // Читаем файл
                ResourceDatabase db = JsonUtility.FromJson<ResourceDatabase>(jsonFile.text);

                // Записываем значения
                foreach (var entry in db.resources)
                {
                    // Конвертируем строку "Wood" в enum ResourceType.Wood
                    if (Enum.TryParse(entry.type, out ResourceType parsedType))
                    {
                        _inventory[parsedType] = entry.amount;
                    }
                }
                Debug.Log("Kingdom: Resources loaded from JSON successfully.");
            }
            else
            {
                Debug.LogError("Kingdom: 'InitialResources.json' not found in Assets/Resources!");
            }
        }

        public void AddResource(ResourceType type, int amount)
        {
            if (!_inventory.ContainsKey(type)) _inventory[type] = 0;
            _inventory[type] += amount;
            OnResourceChanged?.Invoke(type, _inventory[type]);
        }

        public int GetResourceAmount(ResourceType type)
        {
            _inventory.TryGetValue(type, out int amount);
            return amount;
        }

        public Dictionary<ResourceType, int> GetAllInventory() => _inventory;

        public bool TrySpendResources(List<BuildingCost> costs)
        {
            if (costs == null) return true;
            foreach (var cost in costs)
            {
                if (GetResourceAmount(cost.resourceType) < cost.amount) return false;
            }
            foreach (var cost in costs)
            {
                AddResource(cost.resourceType, -cost.amount);
            }
            return true;
        }
    }
}