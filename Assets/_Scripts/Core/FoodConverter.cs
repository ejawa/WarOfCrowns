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
        public static FoodConverter Instance { get; private set; }

        [Header("Справочник Сытости")]
        [Tooltip("Здесь мы просто указываем, сколько сытости восстанавливает та или иная еда.")]
        [SerializeField] private List<FoodSatietyMapping> foodSatietyTable;

        private Dictionary<ResourceType, int> _satietyMap = new Dictionary<ResourceType, int>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            foreach (var mapping in foodSatietyTable)
            {
                if (!_satietyMap.ContainsKey(mapping.foodType))
                    _satietyMap.Add(mapping.foodType, mapping.satietyValue);
            }
        }

        // Юнит вызывает это, чтобы узнать, наелся ли он
        public int GetSatietyValue(ResourceType type)
        {
            if (_satietyMap.ContainsKey(type)) return _satietyMap[type];
            return 0;
        }

        // МЕТОД ConvertResourceToSatiety УДАЛЕН, так как он больше не нужен
    }
}