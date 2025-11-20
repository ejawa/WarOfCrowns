using UnityEngine;
using WarOfCrowns.Core;

namespace WarOfCrowns.Buildings
{
    [RequireComponent(typeof(Building))]
    public class JobBuilding : MonoBehaviour
    {
        [Header("Производство")]
        public ResourceType producedResource;
        public int producedAmount = 1;
        public float productionTime = 5f; // 5 секунд работы рабочего = 1 предмет

        [Header("Потребление")]
        public ResourceType requiredResource;
        public int requiredAmount = 1;

        private Building _building;
        private float _currentProgress;

        private void Awake() { _building = GetComponent<Building>(); }

        // Update удален! Само здание ничего не делает.

        public void AddWorkProgress(float amount)
        {
            if (_building.OwningKingdom == null) return;

            _currentProgress += amount;
            if (_currentProgress >= productionTime)
            {
                TryProduce();
                _currentProgress = 0;
            }
        }

        private void TryProduce()
        {
            // Конвертер (Мельница, Пекарня)
            if (requiredAmount > 0)
            {
                if (_building.OwningKingdom.GetResourceAmount(requiredResource) >= requiredAmount)
                {
                    _building.OwningKingdom.AddResource(requiredResource, -requiredAmount);
                    _building.OwningKingdom.AddResource(producedResource, producedAmount);

                    // Дублируем сытость (Хлеб)
                    if (producedResource == ResourceType.Bread)
                        _building.OwningKingdom.AddResource(ResourceType.Food, producedAmount * 50);
                }
            }
            else // Генератор (Ферма)
            {
                _building.OwningKingdom.AddResource(producedResource, producedAmount);
            }
        }
    }
}