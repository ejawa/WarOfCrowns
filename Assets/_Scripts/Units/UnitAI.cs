using UnityEngine;
using WarOfCrowns.Core;
using WarOfCrowns.Units;
using System.Collections;
using System.Collections.Generic;
using WarOfCrowns.Buildings;

namespace WarOfCrowns.Units
{
    public enum UnitState { Idling, MovingToTarget, Working, SeekingFood, Fighting }

    [RequireComponent(typeof(Unit), typeof(UnitMotor))]
    public class UnitAI : MonoBehaviour
    {
        public UnitState CurrentState { get; private set; }

        [Header("Настройки Поиска Еды")]
        [SerializeField] private List<ResourceType> foodPriorityList;

        private Unit _unit;
        private UnitMotor _motor;
        private UnitWorker _worker; // Ссылка на скрипт рабочего
        private Coroutine _currentActionCoroutine;

        // Переменная для запоминания здания, где мы работали
        private JobBuilding _jobToReturnTo;

        private void Awake()
        {
            _unit = GetComponent<Unit>();
            _motor = GetComponent<UnitMotor>();
            _worker = GetComponent<UnitWorker>();
        }

        private void Start()
        {
            // Не сбрасываем состояние, если оно загружено из сохранения
            if (CurrentState == UnitState.SeekingFood) SeekFood();
        }

        public void SetState(UnitState newState)
        {
            CurrentState = newState;
        }

        public void CancelAction()
        {
            if (_currentActionCoroutine != null) StopCoroutine(_currentActionCoroutine);
            _unit.IsEating = false;

            // Если отменили, пробуем вернуться в Idling
            SetState(UnitState.Idling);
        }

        public void SeekFood()
        {
            if (CurrentState == UnitState.SeekingFood || CurrentState == UnitState.Fighting) return;

            // --- ШАГ 1: ЗАПОМИНАЕМ РАБОТУ (До того, как остановим ее) ---
            if (_worker != null && _worker.CurrentJob != null)
            {
                _jobToReturnTo = _worker.CurrentJob;
                Debug.Log($"{gameObject.name}: Saved job at {_jobToReturnTo.name} before eating.");
            }
            else
            {
                // Если мы не работали, то и возвращаться некуда
                _jobToReturnTo = null;
            }
            // -------------------------------------------------------------

            // ШАГ 2: Отменяем текущие действия
            GetComponent<UnitGatherer>()?.StopGathering();
            GetComponent<UnitBuilder>()?.Cancel();
            _worker?.StopWorking(); // Это обнулит CurrentJob, но мы его уже сохранили выше

            SetState(UnitState.SeekingFood);

            if (_currentActionCoroutine != null) StopCoroutine(_currentActionCoroutine);
            _currentActionCoroutine = StartCoroutine(SeekFoodRoutine());
        }

        private IEnumerator SeekFoodRoutine()
        {
            WarehouseBuilding[] warehouses = FindObjectsOfType<WarehouseBuilding>();
            if (warehouses.Length == 0)
            {
                Debug.LogWarning($"{gameObject.name} wants to eat, but there are no warehouses!");
                ReturnToWorkOrIdle(); // Сразу пытаемся вернуться
                yield break;
            }

            // Берем первый склад (можно улучшить до ближайшего)
            WarehouseBuilding targetWarehouse = warehouses[0];

            // Идем к складу
            _motor.MoveTo(targetWarehouse.transform.position);

            // Ждем пока дойдем (проверяем дистанцию)
            while (targetWarehouse != null && Vector3.Distance(transform.position, targetWarehouse.transform.position) > 3f)
            {
                yield return null;
            }

            _motor.MoveTo(transform.position); // Стоп

            // --- ПРОЦЕСС ЕДЫ ---
            _unit.IsEating = true; // Включаем паузу голода

            // Едим, пока сытость не станет хотя бы 70
            while (_unit.satiety < 70)
            {
                bool ateSomething = false;

                // Проходим по списку приоритетов (Ягоды -> Хлеб)
                foreach (var foodType in foodPriorityList)
                {
                    // Есть ли эта еда на складе королевства?
                    if (_unit.OwningKingdom.GetResourceAmount(foodType) > 0)
                    {
                        // Берем 1 шт
                        _unit.OwningKingdom.AddResource(foodType, -1);

                        // Узнаем питательность и едим
                        int satietyVal = FoodConverter.Instance.GetSatietyValue(foodType);
                        _unit.Eat(satietyVal);

                        // Debug.Log($"{gameObject.name} ate {foodType}. Satiety: {_unit.satiety}");
                        ateSomething = true;
                        break; // Съели что-то одно, выходим из foreach, ждем секунду
                    }
                }

                if (!ateSomething)
                {
                    Debug.LogWarning($"{gameObject.name}: No food left in warehouse! Stopping lunch.");
                    break; // Еды нет, прерываем обед
                }

                yield return new WaitForSeconds(1f); // Жуем
            }

            _unit.IsEating = false; // Выключаем паузу голода

            // --- ВОЗВРАЩЕНИЕ ---
            ReturnToWorkOrIdle();
        }

        private void ReturnToWorkOrIdle()
        {
            // Проверяем, есть ли сохраненная работа и существует ли еще это здание
            if (_jobToReturnTo != null)
            {
                Debug.Log($"{gameObject.name} is returning to work at {_jobToReturnTo.name}.");

                // Восстанавливаем состояние "Работает"
                _worker.SetTarget(_jobToReturnTo);
                // SetTarget сам запустит корутину движения и работы
            }
            else
            {
                Debug.Log($"{gameObject.name} has no job to return to. Idling.");
                SetState(UnitState.Idling);
            }
        }
    }
}