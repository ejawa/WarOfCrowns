using System.Collections;
using UnityEngine;
using WarOfCrowns.Core;
using WarOfCrowns.Units;

namespace WarOfCrowns.Buildings
{
    public class TownHall : MonoBehaviour
    {
        [Header("Настройки Производства")]
        [SerializeField] private GameObject peasantPrefab;
        [SerializeField] private float productionTime = 5f;
        [SerializeField] private Transform spawnPoint;

        [Header("Стоимость")]
        // Теперь ты можешь выбрать Berries или Bread прямо в инспекторе!
        [SerializeField] private ResourceType resourceCostType = ResourceType.Food;
        [SerializeField] private int amountCost = 50;

        [HideInInspector]
        public Kingdom OwningKingdom;

        public void TryProducePeasant()
        {
            Debug.Log("TownHall: Кнопка нажата! Пытаюсь создать юнита...");

            if (OwningKingdom == null)
            {
                Debug.LogError("TownHall: Ошибка! OwningKingdom не назначен.");
                return;
            }

            if (PopulationManager.Instance.IsCapReached())
            {
                Debug.Log("TownHall: Достигнут лимит населения!");
                return;
            }

            // Проверяем наличие выбранного ресурса
            int currentAmount = OwningKingdom.GetResourceAmount(resourceCostType);
            Debug.Log($"TownHall: Проверка ресурсов. Нужно {amountCost} {resourceCostType}, есть {currentAmount}.");

            if (currentAmount >= amountCost)
            {
                OwningKingdom.AddResource(resourceCostType, -amountCost);
                StartCoroutine(ProductionRoutine());
            }
            else
            {
                Debug.Log("TownHall: Недостаточно ресурсов!");
            }
        }

        private IEnumerator ProductionRoutine()
        {
            Debug.Log("TownHall: Производство начато...");
            yield return new WaitForSeconds(productionTime);
            if (spawnPoint != null)
            {
                GameObject peasantInstance = Instantiate(peasantPrefab, spawnPoint.position, Quaternion.identity);
                if (peasantInstance.TryGetComponent<Unit>(out var unit))
                {
                    unit.OwningKingdom = this.OwningKingdom;
                }
                Debug.Log("TownHall: Юнит создан!");
            }
        }
    }
}