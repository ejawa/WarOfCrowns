using UnityEngine;
using WarOfCrowns.Buildings;
using WarOfCrowns.Core;
using System.Collections.Generic;

namespace WarOfCrowns.Buildings
{
    [RequireComponent(typeof(Building))]
    public class ConstructionSite : MonoBehaviour
    {
        [Header("Настройки Строительства")]
        [SerializeField] private float buildTime = 10f;
        [SerializeField] private GameObject finishedBuildingPrefab;

        [Header("Визуал")]
        [Tooltip("Сюда перетащи дочерний SpriteRenderer (Icon_Overlay).")]
        [SerializeField] private SpriteRenderer iconRenderer;

        public Kingdom OwningKingdom { get; set; }

        private List<BuildingCost> _totalCosts;
        private float _currentBuildProgress;

        private void Start()
        {
            // 1. Получаем данные о стоимости
            var myBuildingData = GetComponent<Building>();
            _totalCosts = myBuildingData.costs;

            if (_totalCosts == null || _totalCosts.Count == 0)
            {
                // Debug.LogWarning(...); 
            }

            // 2. --- ЛОГИКА ИКОНКИ (ИСПРАВЛЕНА) ---
            if (iconRenderer != null)
            {
                Sprite iconToUse = null;

                // А) Сначала ищем иконку на САМОМ ЭТОМ ФУНДАМЕНТЕ (это логичнее всего)
                if (myBuildingData.buildingIcon != null)
                {
                    iconToUse = myBuildingData.buildingIcon;
                }
                // Б) Если нет, ищем на ФИНАЛЬНОМ здании
                else if (finishedBuildingPrefab != null)
                {
                    var finishedData = finishedBuildingPrefab.GetComponent<Building>();
                    if (finishedData != null)
                    {
                        iconToUse = finishedData.buildingIcon;
                    }
                }

                // В) Применяем
                if (iconToUse != null)
                {
                    iconRenderer.sprite = iconToUse;
                    iconRenderer.color = new Color(1f, 1f, 1f, 0.7f); // Полупрозрачный
                    iconRenderer.gameObject.SetActive(true);
                }
                else
                {
                    // Если иконки нет, ВЫКЛЮЧАЕМ белый квадрат, чтобы не мешал
                    iconRenderer.sprite = null;
                    iconRenderer.gameObject.SetActive(false);
                }
            }
        }

        public bool AddBuildProgress(float progressAmount)
        {
            if (OwningKingdom == null) return false;

            bool canAffordTick = true;
            if (_totalCosts != null)
            {
                foreach (var cost in _totalCosts)
                {
                    int costForThisTick = Mathf.CeilToInt(((float)cost.amount / buildTime) * progressAmount);
                    if (OwningKingdom.GetResourceAmount(cost.resourceType) < costForThisTick)
                    {
                        canAffordTick = false;
                        break;
                    }
                }
            }

            if (canAffordTick)
            {
                if (_totalCosts != null)
                {
                    foreach (var cost in _totalCosts)
                    {
                        int costForThisTick = Mathf.CeilToInt(((float)cost.amount / buildTime) * progressAmount);
                        if (costForThisTick > 0) OwningKingdom.AddResource(cost.resourceType, -costForThisTick);
                    }
                }
                _currentBuildProgress += progressAmount;
            }
            else
            {
                return false;
            }

            if (_currentBuildProgress >= buildTime)
            {
                if (finishedBuildingPrefab != null)
                {
                    GameObject finalBuilding = Instantiate(finishedBuildingPrefab, transform.position, transform.rotation);

                    if (finalBuilding.TryGetComponent<Building>(out var buildingLogic))
                        buildingLogic.OwningKingdom = this.OwningKingdom;
                    if (finalBuilding.TryGetComponent<TownHall>(out var townHallLogic))
                        townHallLogic.OwningKingdom = this.OwningKingdom;
                }
                Destroy(gameObject);
                return true;
            }
            return false;
        }

        // --- МЕТОДЫ ДЛЯ СОХРАНЕНИЯ ---
        public float GetProgress() => _currentBuildProgress;
        public void SetProgress(float value) => _currentBuildProgress = value;
    }
}