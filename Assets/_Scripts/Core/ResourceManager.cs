using System;
using System.Collections.Generic;
using UnityEngine;
using WarOfCrowns.Core.Items; // <-- Важно, чтобы он "видел" ItemType

namespace WarOfCrowns.Core
{
    public class ResourceManager : MonoBehaviour
    {
        public static ResourceManager Instance { get; private set; }

        // Словарь для базовых ресурсов (дерево, камень...)
        private Dictionary<ResourceType, int> _resources = new Dictionary<ResourceType, int>();

        // --- НОВЫЙ СЛОВАРЬ для предметов (еда, товары...) ---
        private Dictionary<ItemType, int> _items = new Dictionary<ItemType, int>();

        // --- НОВЫЕ СОБЫТИЯ ---
        public static event Action<ResourceType, int> OnResourceChanged;
        public static event Action<ItemType, int> OnItemChanged;

        public Dictionary<ResourceType, int> GetAllResources()
        {
            return _resources;
        }

        public Dictionary<ItemType, int> GetAllItems()
        {
            return _items;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) Destroy(gameObject);
            else Instance = this;
            InitializeResources();
        }

        private void InitializeResources()
        {
            _resources[ResourceType.Wood] = 150;
            _resources[ResourceType.Stone] = 100;
            _resources[ResourceType.Gold] = 100;
            _resources[ResourceType.Food] = 200; // Это "Сытость", оставляем
            // Инициализируем остальные нулями
            _resources[ResourceType.IronOre] = 0;
            _resources[ResourceType.Coal] = 0;

            // Инициализируем все предметы нулями
            foreach (ItemType itemType in Enum.GetValues(typeof(ItemType)))
            {
                _items[itemType] = 0;
            }
        }

        // --- МЕТОДЫ ДЛЯ РЕСУРСОВ (без изменений) ---
        public void AddResource(ResourceType type, int amount)
        {
            if (!_resources.ContainsKey(type)) _resources[type] = 0;
            _resources[type] += amount;
            OnResourceChanged?.Invoke(type, _resources[type]);
        }
        public int GetResourceAmount(ResourceType type) => _resources.ContainsKey(type) ? _resources[type] : 0;

        // --- НОВЫЕ МЕТОДЫ ДЛЯ ПРЕДМЕТОВ ---
        public void AddItem(ItemType type, int amount)
        {
            if (!_items.ContainsKey(type)) _items[type] = 0;
            _items[type] += amount;
            OnItemChanged?.Invoke(type, _items[type]);
        }
        public int GetItemAmount(ItemType type) => _items.ContainsKey(type) ? _items[type] : 0;
    }
}