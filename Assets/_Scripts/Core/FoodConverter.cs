using UnityEngine;
using System.Collections.Generic;

namespace WarOfCrowns.Core
{
    [System.Serializable]
    public class FoodSatietyMapping
    {
        public ResourceType foodType;
        public int satietyValue;
    }

    public class FoodConverter : MonoBehaviour
    {
        public static FoodConverter Instance { get; private set; } // Синглтон для доступа

        [SerializeField] private List<FoodSatietyMapping> foodSatietyTable;
        private Dictionary<ResourceType, int> _satietyMap = new Dictionary<ResourceType, int>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            foreach (var mapping in foodSatietyTable)
            {
                _satietyMap[mapping.foodType] = mapping.satietyValue;
            }
        }

        // --- НОВЫЙ МЕТОД, КОТОРЫЙ НУЖЕН ДЛЯ UNIT AI ---
        public int GetSatietyValue(ResourceType type)
        {
            if (_satietyMap.ContainsKey(type))
                return _satietyMap[type];

            return 0; // Если не нашли, питательность 0
        }
    }
}