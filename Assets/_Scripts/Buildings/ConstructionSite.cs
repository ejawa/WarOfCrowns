using UnityEngine;
using WarOfCrowns.Buildings;
using WarOfCrowns.Core;
using System.Collections.Generic;

namespace WarOfCrowns.Buildings // <-- Добавили namespace
{
    [RequireComponent(typeof(Building))]
    public class ConstructionSite : MonoBehaviour
    {
        [Header("Настройки Строительства")]
        [SerializeField] private float buildTime = 10f;
        [SerializeField] private GameObject finishedBuildingPrefab;

        public Kingdom OwningKingdom { get; set; }

        private List<BuildingCost> _totalCosts;
        private float _currentBuildProgress;

        private void Start()
        {
            _totalCosts = GetComponent<Building>().costs;

            if (_totalCosts == null || _totalCosts.Count == 0)
            {
                Debug.LogWarning($"ConstructionSite on '{gameObject.name}' has no Costs defined. It will be built for free.");
            }
        }

        // Вызывается юнитом-строителем каждую секунду
        public bool AddBuildProgress(float progressAmount)
        {
            if (OwningKingdom == null)
            {
                Debug.LogWarning("ConstructionSite cannot work: Missing Kingdom assignment.");
                return false;
            }

            if (_totalCosts != null && _totalCosts.Count > 0)
            {
                // --- ЛОГИКА ПОСТЕПЕННОЙ ТРАТЫ РЕСУРСОВ ---
                // Проверяем, хватает ли ресурсов НА ЭТОТ ТИК
                bool canAffordTick = true;
                foreach (var cost in _totalCosts)
                {
                    // (Стоимость / Время) * Прогресс за тик
                    int costForThisTick = Mathf.CeilToInt(((float)cost.amount / buildTime) * progressAmount);
                    if (OwningKingdom.GetResourceAmount(cost.resourceType) < costForThisTick)
                    {
                        canAffordTick = false;
                        Debug.Log($"Not enough {cost.resourceType} to continue construction!");
                        break; // Прерываем проверку, если не хватает хотя бы одного ресурса
                    }
                }

                // Если ресурсов хватает, тратим их и добавляем прогресс
                if (canAffordTick)
                {
                    foreach (var cost in _totalCosts)
                    {
                        int costForThisTick = Mathf.CeilToInt(((float)cost.amount / buildTime) * progressAmount);
                        if (costForThisTick > 0)
                        {
                            OwningKingdom.AddResource(cost.resourceType, -costForThisTick);
                        }
                    }
                    _currentBuildProgress += progressAmount;
                }
                else
                {
                    return false; // Стройка "заморожена", прогресс не идет
                }
            }
            else // Если здание бесплатное, просто добавляем прогресс
            {
                _currentBuildProgress += progressAmount;
            }

            // --- ПРОВЕРКА ЗАВЕРШЕНИЯ ---
            if (_currentBuildProgress >= buildTime)
            {
                if (finishedBuildingPrefab != null)
                {
                    GameObject finalBuilding = Instantiate(finishedBuildingPrefab, transform.position, transform.rotation);

                    if (finalBuilding.TryGetComponent<Building>(out var buildingLogic))
                    {
                        buildingLogic.OwningKingdom = this.OwningKingdom;
                    }
                    if (finalBuilding.TryGetComponent<TownHall>(out var townHallLogic))
                    {
                        townHallLogic.OwningKingdom = this.OwningKingdom;
                    }
                }
                Destroy(gameObject);
                return true;
            }
            return false;
        }
    }
}