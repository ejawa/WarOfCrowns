using UnityEngine;
using WarOfCrowns.Core; // Для ResourceType и Kingdom
using WarOfCrowns.Units;
using System.Collections;
using System.Collections.Generic;
using WarOfCrowns.Buildings; // Для WarehouseBuilding

namespace WarOfCrowns.Units
{
    public enum UnitState { Idling, MovingToTarget, Working, SeekingFood, Fighting }

    [RequireComponent(typeof(Unit), typeof(UnitMotor))]
    public class UnitAI : MonoBehaviour
    {
        public UnitState CurrentState { get; private set; }

        [Header("Настройки Поиска Еды")]
        [Tooltip("Список типов ресурсов в порядке приоритета (что есть первым).")]
        [SerializeField] private List<ResourceType> foodPriorityList; // <-- ИСПРАВЛЕНО: теперь ResourceType

        private Unit _unit;
        private UnitMotor _motor;
        private Coroutine _currentActionCoroutine;

        private void Awake()
        {
            _unit = GetComponent<Unit>();
            _motor = GetComponent<UnitMotor>();
        }

        private void Start()
        {
            SetState(UnitState.Idling);
        }

        public void SetState(UnitState newState)
        {
            CurrentState = newState;
        }

        // Метод для принудительной остановки (вызывается из Unit.cs и Controller)
        public void CancelAction()
        {
            if (_currentActionCoroutine != null) StopCoroutine(_currentActionCoroutine);
            SetState(UnitState.Idling);
        }

        public void SeekFood()
        {
            if (CurrentState == UnitState.SeekingFood || CurrentState == UnitState.Fighting) return;

            // Отменяем любую текущую работу
            GetComponent<UnitGatherer>()?.StopGathering();
            GetComponent<UnitBuilder>()?.Cancel();
            GetComponent<UnitWorker>()?.StopWorking();

            SetState(UnitState.SeekingFood);

            if (_currentActionCoroutine != null) StopCoroutine(_currentActionCoroutine);
            _currentActionCoroutine = StartCoroutine(SeekFoodRoutine());
        }

        private IEnumerator SeekFoodRoutine()
        {
            // 1. Ищем ближайший склад
            WarehouseBuilding[] warehouses = FindObjectsOfType<WarehouseBuilding>();
            if (warehouses.Length == 0)
            {
                Debug.LogWarning($"{gameObject.name} wants to eat, but there are no warehouses!");
                SetState(UnitState.Idling);
                yield break;
            }

            // (Упрощение: берем первый попавшийся)
            WarehouseBuilding targetWarehouse = warehouses[0];

            // 2. Идем к складу
            _motor.MoveTo(targetWarehouse.transform.position);

            // Ждем пока дойдем (дистанция 3 метра)
            while (Vector3.Distance(transform.position, targetWarehouse.transform.position) > 3f)
            {
                yield return null;
            }

            _motor.MoveTo(transform.position); // Останавливаемся у склада

            // 3. ЦИКЛ ЕДЫ: Ешь, пока голоден
            while (_unit.hunger > 0)
            {
                bool ateSomething = false;

                // Ищем еду по приоритету из списка Enum
                foreach (var foodType in foodPriorityList)
                {
                    // Проверяем наличие в Королевстве
                    if (_unit.OwningKingdom.GetResourceAmount(foodType) > 0)
                    {
                        // Забираем 1 единицу со склада
                        _unit.OwningKingdom.AddResource(foodType, -1);

                        // Узнаем питательность через FoodConverter
                        int satiety = FoodConverter.Instance.GetSatietyValue(foodType);

                        // Утоляем голод
                        _unit.Eat(satiety);

                        Debug.Log($"{gameObject.name} ate {foodType}. Hunger is now {_unit.hunger}");
                        ateSomething = true;
                        break; // Съели 1 предмет, выходим из foreach и ждем секунду
                    }
                }

                if (!ateSomething)
                {
                    Debug.LogWarning("No food left in warehouse!");
                    break; // Еды нет совсем, уходим
                }

                // Жуем 1 секунду перед следующим укусом
                yield return new WaitForSeconds(1f);
            }

            SetState(UnitState.Idling);
        }
    }
}