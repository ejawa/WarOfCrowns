using System.Collections.Generic;
using UnityEngine;

namespace WarOfCrowns.Core
{
    public class ToolStorageManager : MonoBehaviour
    {
        public static ToolStorageManager Instance { get; private set; }

        private List<Buildings.ToolStorage> _allStorages = new List<Buildings.ToolStorage>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void RegisterStorage(Buildings.ToolStorage storage)
        {
            if (!_allStorages.Contains(storage))
                _allStorages.Add(storage);
        }

        public void UnregisterStorage(Buildings.ToolStorage storage)
        {
            if (_allStorages.Contains(storage))
                _allStorages.Remove(storage);
        }

        public Buildings.ToolStorage GetNearestStorage(Vector3 position)
        {
            if (_allStorages.Count == 0) return null;

            Buildings.ToolStorage nearest = null;
            float minDistance = float.MaxValue;

            foreach (var storage in _allStorages)
            {
                float distance = Vector3.Distance(position, storage.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = storage;
                }
            }
            return nearest;
        }
    }
}