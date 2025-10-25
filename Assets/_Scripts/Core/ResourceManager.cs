using System;
using System.Collections.Generic;
using UnityEngine;

namespace WarOfCrowns.Core
{
    public class ResourceManager : MonoBehaviour
    {
        public static ResourceManager Instance { get; private set; }

        private Dictionary<ResourceType, int> _resources = new Dictionary<ResourceType, int>();

        // This event will notify the UI when a resource amount changes
        public static event Action<ResourceType, int> OnResourceChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                InitializeResources();
            }
        }

        private void InitializeResources()
        {
            // According to the design doc, we start with some resources
            _resources[ResourceType.Wood] = 150;
            _resources[ResourceType.Stone] = 100;
            _resources[ResourceType.Gold] = 100;
            _resources[ResourceType.Food] = 200;
        }

        public void AddResource(ResourceType type, int amount)
        {
            if (!_resources.ContainsKey(type))
            {
                _resources[type] = 0;
            }
            _resources[type] += amount;

            // Fire the event to let listeners (like the UI) know
            OnResourceChanged?.Invoke(type, _resources[type]);
            Debug.Log($"Added {amount} {type}. Total: {_resources[type]}");
        }

        public int GetResourceAmount(ResourceType type)
        {
            return _resources.ContainsKey(type) ? _resources[type] : 0;
        }
    }
}