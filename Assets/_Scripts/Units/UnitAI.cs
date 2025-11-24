using UnityEngine;
using WarOfCrowns.Core;
using System.Collections;
using System.Collections.Generic;
using WarOfCrowns.Buildings;
using System.Linq; // Для сортировки дистанции

namespace WarOfCrowns.Units
{
    public enum UnitState { Idling, MovingToTarget, Working, SeekingFood, Fighting, Training }

    [RequireComponent(typeof(Unit), typeof(UnitMotor))]
    public class UnitAI : MonoBehaviour
    {
        public UnitState CurrentState { get; private set; }

        [Header("Настройки Поиска Еды")]
        [Tooltip("Список приоритетов (Сначала едим Ягоды, если нет - Хлеб)")]
        [SerializeField] private List<ResourceType> foodPriorityList;

        private Unit _unit;
        private UnitMotor _motor;
        private UnitWorker _worker;
        private Coroutine _currentActionCoroutine;

        // Память
        private JobBuilding _jobToReturnTo;
        private Vector3 _lastIdlePosition;

        // Переменные для тренировки
        private Barracks _targetBarracks;
        private ResourceType _pendingWeapon;

        public void CommandGoTrain(Barracks barracks, ResourceType weapon)
        {
            // Отменяем все текущие дела
            GetComponent<UnitGatherer>()?.StopGathering();
            GetComponent<UnitBuilder>()?.Cancel();
            GetComponent<UnitWorker>()?.StopWorking();

            _targetBarracks = barracks;
            _pendingWeapon = weapon;

            SetState(UnitState.Training);

            if (_currentActionCoroutine != null) StopCoroutine(_currentActionCoroutine);
            _currentActionCoroutine = StartCoroutine(GoToBarracksRoutine());
        }

        private IEnumerator GoToBarracksRoutine()
        {
            if (_targetBarracks == null) { SetState(UnitState.Idling); yield break; }

            // Идем ко входу
            // (Предполагаем, что Barracks это здание, берем его позицию)
            _motor.MoveTo(_targetBarracks.transform.position);

            // Ждем пока дойдем
            while (Vector3.Distance(transform.position, _targetBarracks.transform.position) > 1.5f)
            {
                if (_targetBarracks == null) { SetState(UnitState.Idling); yield break; }
                yield return null;
            }

            _motor.StopMoving();

            // Мы пришли. Сообщаем казарме, чтобы она нас переодела.
            _targetBarracks.FinalizeTraining(_unit, _pendingWeapon);

            // Состояние сбросится внутри FinalizeTraining (в Idling)
        }
        private void Awake()
        {
            _unit = GetComponent<Unit>();
            _motor = GetComponent<UnitMotor>();
            _worker = GetComponent<UnitWorker>();
        }

        private void Start()
        {
            // Если загрузились в состоянии поиска еды - перезапускаем процесс
            if (CurrentState == UnitState.SeekingFood) SeekFood();
        }

        public void SetState(UnitState newState) => CurrentState = newState;

        public void CancelAction()
        {
            if (_currentActionCoroutine != null) StopCoroutine(_currentActionCoroutine);
            _unit.IsEating = false;
            SetState(UnitState.Idling);
        }

        public void SeekFood()
        {
            if (CurrentState == UnitState.SeekingFood || CurrentState == UnitState.Fighting) return;

            // 1. Запоминаем, куда вернуться
            if (_worker != null && _worker.CurrentJob != null)
            {
                _jobToReturnTo = _worker.CurrentJob;
                _lastIdlePosition = Vector3.zero;
            }
            else
            {
                _jobToReturnTo = null;
                _lastIdlePosition = transform.position;
            }

            // 2. Останавливаем текущую работу
            GetComponent<UnitGatherer>()?.StopGathering();
            GetComponent<UnitBuilder>()?.Cancel();
            _worker?.StopWorking();

            SetState(UnitState.SeekingFood);

            if (_currentActionCoroutine != null) StopCoroutine(_currentActionCoroutine);
            _currentActionCoroutine = StartCoroutine(SeekFoodRoutine());
        }

        private IEnumerator SeekFoodRoutine()
        {
            // --- ИСПРАВЛЕНИЕ: Если складов нет, сразу отменяем ---
            WarehouseBuilding[] warehouses = FindObjectsOfType<WarehouseBuilding>();
            if (warehouses.Length == 0)
            {
                // Debug.LogWarning($"{gameObject.name}: No warehouses found!");
                ReturnToWorkOrIdle();
                yield break;
            }

            // Находим ближайший склад (простая сортировка)
            WarehouseBuilding targetWarehouse = GetClosestWarehouse(warehouses);

            // Движение к складу
            _motor.MoveTo(targetWarehouse.transform.position);

            // Ждем пока дойдем (дистанция побольше, чтобы не толкать здание)
            while (targetWarehouse != null && Vector3.Distance(transform.position, targetWarehouse.transform.position) > 2.5f)
            {
                // Если склад уничтожили пока мы шли - отмена
                if (targetWarehouse == null)
                {
                    ReturnToWorkOrIdle();
                    yield break;
                }
                yield return null;
            }

            // --- ИСПРАВЛЕНИЕ: Явная остановка ---
            _motor.StopMoving();
            // -----------------------------------

            // --- НОВАЯ ЛОГИКА: МГНОВЕННОЕ ПОГЛОЩЕНИЕ ---
            _unit.IsEating = true;

            // Небольшая задержка для визуализации (чтобы он не телепортировался мгновенно)
            yield return new WaitForSeconds(0.5f);

            // Пробегаем по списку приоритетов
            foreach (var foodType in foodPriorityList)
            {
                // Если мы уже наелись (больше 90%), прекращаем жрать
                if (_unit.satiety >= 90f) break;

                int amountInStock = _unit.OwningKingdom.GetResourceAmount(foodType);

                if (amountInStock > 0)
                {
                    // Узнаем питательность 1 шт.
                    int satietyPerItem = FoodConverter.Instance.GetSatietyValue(foodType);
                    if (satietyPerItem <= 0) continue; // Защита от ошибок

                    // Считаем, сколько нам не хватает до 100
                    float missingSatiety = 100f - _unit.satiety;

                    // Считаем, сколько штук нужно съесть (округляем вверх)
                    // Например: не хватает 45, хлеб дает 50. 45/50 = 0.9 -> берем 1 хлеб.
                    int itemsNeedToEat = Mathf.CeilToInt(missingSatiety / satietyPerItem);

                    // Берем столько, сколько нужно, ИЛИ столько, сколько есть на складе
                    int itemsToTake = Mathf.Min(itemsNeedToEat, amountInStock);

                    // --- ТРАНЗАКЦИЯ ---
                    _unit.OwningKingdom.AddResource(foodType, -itemsToTake);
                    _unit.Eat(itemsToTake * satietyPerItem);

                    // Debug.Log($"{gameObject.name} ate {itemsToTake} {foodType}.");
                }
            }

            _unit.IsEating = false;

            // --- ВОЗВРАЩЕНИЕ ---
            // Если еды не хватило или ее не было - мы все равно возвращаемся, 
            // чтобы не стоять вечно у пустого склада.
            ReturnToWorkOrIdle();
        }

        private void ReturnToWorkOrIdle()
        {
            // Проверка: если мы возвращаемся на работу, существует ли еще здание?
            if (_jobToReturnTo != null)
            {
                _worker.SetTarget(_jobToReturnTo);
                SetState(UnitState.Working);
            }
            else if (_lastIdlePosition != Vector3.zero)
            {
                _motor.MoveTo(_lastIdlePosition);
                SetState(UnitState.Idling);
            }
            else
            {
                SetState(UnitState.Idling);
            }
        }

        private WarehouseBuilding GetClosestWarehouse(WarehouseBuilding[] warehouses)
        {
            WarehouseBuilding bestTarget = null;
            float closestDistanceSqr = Mathf.Infinity;
            Vector3 currentPosition = transform.position;

            foreach (WarehouseBuilding w in warehouses)
            {
                if (w == null) continue;
                Vector3 directionToTarget = w.transform.position - currentPosition;
                float dSqrToTarget = directionToTarget.sqrMagnitude;
                if (dSqrToTarget < closestDistanceSqr)
                {
                    closestDistanceSqr = dSqrToTarget;
                    bestTarget = w;
                }
            }
            return bestTarget;
        }
    }
}