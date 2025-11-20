using System;
using System.Collections.Generic;
using UnityEngine;
using WarOfCrowns.Buildings; // Для BuildingCost
using WarOfCrowns.Data;      // Для ResourceSaveEntry (система сохранений)

namespace WarOfCrowns.Core
{
    // --- Вспомогательные классы для чтения InitialResources.json ---
    [Serializable]
    public class InitialResourceEntry
    {
        public string type;
        public int amount;
    }

    [Serializable]
    public class InitialResourceDatabase
    {
        public List<InitialResourceEntry> resources;
    }
    // ---------------------------------------------------------------

    public class Kingdom : MonoBehaviour
    {
        public int kingdomID;
        public static Kingdom PlayerKingdom { get; private set; }

        public event Action<ResourceType, int> OnResourceChanged;

        private Dictionary<ResourceType, int> _inventory = new Dictionary<ResourceType, int>();

        private void Awake()
        {
            // 1. Регистрация синглтона (только для игрока)
            if (kingdomID == 0)
            {
                if (PlayerKingdom == null) PlayerKingdom = this;
                else { Destroy(gameObject); return; }
            }

            // 2. Инициализация пустыми значениями
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                _inventory[type] = 0;
            }

            // 3. Загрузка "Заводских настроек" (Шаблон новой игры)
            LoadFromTemplate();
        }

        // Загрузка из InitialResources.json (вызывается при старте)
        private void LoadFromTemplate()
        {
            TextAsset jsonFile = Resources.Load<TextAsset>("InitialResources");

            if (jsonFile != null)
            {
                InitialResourceDatabase db = JsonUtility.FromJson<InitialResourceDatabase>(jsonFile.text);

                foreach (var entry in db.resources)
                {
                    if (Enum.TryParse(entry.type, out ResourceType parsedType))
                    {
                        // Используем AddResource, чтобы не дублировать код установки
                        // Но здесь мы просто устанавливаем значение, событие пока некому слушать
                        _inventory[parsedType] = entry.amount;
                    }
                }
                Debug.Log("Kingdom: Initial resources loaded from template.");
            }
            else
            {
                Debug.LogError("Kingdom: 'InitialResources.json' not found in Assets/Resources!");
            }
        }

        // --- НОВЫЙ МЕТОД ДЛЯ ЗАГРУЗКИ СОХРАНЕНИЯ (Вызывается из SaveManager) ---
        public void LoadInventoryFromSave(List<ResourceSaveEntry> savedInventory)
        {
            // 1. Обнуляем текущий инвентарь
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                _inventory[type] = 0;
            }

            // 2. Применяем сохраненные значения
            foreach (var entry in savedInventory)
            {
                _inventory[entry.type] = entry.amount;

                // ВАЖНО: Принудительно вызываем событие, чтобы UI (Топ-Бар, Склад) обновился!
                OnResourceChanged?.Invoke(entry.type, entry.amount);
            }

            Debug.Log("Kingdom: Inventory overwritten from Save File.");
        }
        // ------------------------------------------------------------------------

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