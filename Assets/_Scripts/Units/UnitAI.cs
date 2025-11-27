using UnityEngine;
using WarOfCrowns.Buildings;
using WarOfCrowns.Core;
using WarOfCrowns.World;

namespace WarOfCrowns.Units
{
    // Добавили состояние Garrisoning
    public enum UnitState { Idling, MovingToTarget, Working, SeekingFood, Fighting, Training, Garrisoning }

    [RequireComponent(typeof(Unit), typeof(UnitMotor))]
    public class UnitAI : MonoBehaviour
    {
        public UnitState CurrentState { get; private set; }

        [Header("Настройки Поиска Еды")]
        [Tooltip("Список приоритетов (Сначала едим Ягоды, если нет - Хлеб)")]
        [SerializeField] private System.Collections.Generic.List<ResourceType> foodPriorityList;

        private Unit _unit;
        private UnitMotor _motor;
        private UnitWorker _worker;
        private Coroutine _currentActionCoroutine;

        // Память
        private JobBuilding _jobToReturnTo;
        private Vector3 _lastIdlePosition;

        // Для тренировки
        private Barracks _targetBarracks;
        private ResourceType _pendingWeapon;

        // Для гарнизона (НОВОЕ)
        private DefenseTower _targetTower;

        private void Awake()
        {
            _unit = GetComponent<Unit>();
            _motor = GetComponent<UnitMotor>();
            _worker = GetComponent<UnitWorker>();
        }

        private void Start()
        {
            if (CurrentState == UnitState.SeekingFood) SeekFood();
        }

        public void SetState(UnitState newState) => CurrentState = newState;

        public void CancelAction()
        {
            if (_currentActionCoroutine != null) StopCoroutine(_currentActionCoroutine);
            _unit.IsEating = false;
            SetState(UnitState.Idling);
        }

        // --- НОВЫЙ МЕТОД: ИДТИ В ГАРНИЗОН ---
        public void CommandGarrison(DefenseTower tower)
        {
            // Сбрасываем текущие дела
            CancelAction();
            GetComponent<UnitWorker>()?.StopWorking();
            GetComponent<UnitGatherer>()?.StopGathering();
            GetComponent<UnitBuilder>()?.Cancel();
            GetComponent<Fighter>()?.Cancel();

            _targetTower = tower;
            SetState(UnitState.Garrisoning);

            // Идем к башне
            if (_motor != null) _motor.MoveTo(tower.transform.position);
        }

        // --- МЕТОД ДЛЯ КАЗАРМЫ ---
        public void CommandGoTrain(Barracks barracks, ResourceType weapon)
        {
            GetComponent<UnitGatherer>()?.StopGathering();
            GetComponent<UnitBuilder>()?.Cancel();
            GetComponent<UnitWorker>()?.StopWorking();

            _targetBarracks = barracks;
            _pendingWeapon = weapon;
            SetState(UnitState.Training);

            if (_currentActionCoroutine != null) StopCoroutine(_currentActionCoroutine);
            _currentActionCoroutine = StartCoroutine(GoToBarracksRoutine());
        }

        private System.Collections.IEnumerator GoToBarracksRoutine()
        {
            if (_targetBarracks == null) { SetState(UnitState.Idling); yield break; }
            _motor.MoveTo(_targetBarracks.transform.position);

            while (_targetBarracks != null && Vector3.Distance(transform.position, _targetBarracks.transform.position) > 1.5f)
            {
                yield return null;
            }

            if (_targetBarracks == null) { SetState(UnitState.Idling); yield break; }

            _motor.StopMoving();
            _targetBarracks.FinalizeTraining(_unit, _pendingWeapon);
        }

        // --- ЛОГИКА ГОЛОДА ---
        public void SeekFood()
        {
            if (CurrentState == UnitState.SeekingFood || CurrentState == UnitState.Fighting || CurrentState == UnitState.Garrisoning) return;

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

            GetComponent<UnitGatherer>()?.StopGathering();
            GetComponent<UnitBuilder>()?.Cancel();
            _worker?.StopWorking();

            SetState(UnitState.SeekingFood);
            if (_currentActionCoroutine != null) StopCoroutine(_currentActionCoroutine);
            _currentActionCoroutine = StartCoroutine(SeekFoodRoutine());
        }

        private System.Collections.IEnumerator SeekFoodRoutine()
        {
            WarehouseBuilding[] warehouses = FindObjectsOfType<WarehouseBuilding>();
            if (warehouses.Length == 0)
            {
                ReturnToWorkOrIdle();
                yield break;
            }

            WarehouseBuilding targetWarehouse = GetClosestWarehouse(warehouses);
            if (targetWarehouse == null) { ReturnToWorkOrIdle(); yield break; }

            _motor.MoveTo(targetWarehouse.transform.position);

            while (targetWarehouse != null && Vector3.Distance(transform.position, targetWarehouse.transform.position) > 2.5f)
            {
                if (targetWarehouse == null) { ReturnToWorkOrIdle(); yield break; }
                yield return null;
            }

            _motor.StopMoving();
            _unit.IsEating = true;
            yield return new WaitForSeconds(0.5f);

            if (_unit.OwningKingdom != null)
            {
                foreach (var foodType in foodPriorityList)
                {
                    if (_unit.satiety >= 90f) break;
                    int amountInStock = _unit.OwningKingdom.GetResourceAmount(foodType);
                    if (amountInStock > 0)
                    {
                        int satietyPerItem = FoodConverter.Instance.GetSatietyValue(foodType);
                        if (satietyPerItem <= 0) continue;
                        int itemsNeedToEat = Mathf.CeilToInt((100f - _unit.satiety) / satietyPerItem);
                        int itemsToTake = Mathf.Min(itemsNeedToEat, amountInStock);

                        _unit.OwningKingdom.AddResource(foodType, -itemsToTake);
                        _unit.Eat(itemsToTake * satietyPerItem);
                    }
                }
            }

            _unit.IsEating = false;
            ReturnToWorkOrIdle();
        }

        private void ReturnToWorkOrIdle()
        {
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
                // Проверяем владельца склада (чтобы не бежать к врагу)
                // (Здесь предполагается, что у WarehouseBuilding есть Building компонент с ownerID)
                var b = w.GetComponent<Building>();
                if (b != null && Kingdom.PlayerKingdom != null && b.ownerKingdomID.Value != Kingdom.PlayerKingdom.kingdomID) continue;

                float dSqrToTarget = (w.transform.position - currentPosition).sqrMagnitude;
                if (dSqrToTarget < closestDistanceSqr)
                {
                    closestDistanceSqr = dSqrToTarget;
                    bestTarget = w;
                }
            }
            return bestTarget;
        }

        private void Update()
        {
            // --- ЛОГИКА ГАРНИЗОНА ---
            if (CurrentState == UnitState.Garrisoning)
            {
                // Если башню уничтожили пока мы шли
                if (_targetTower == null)
                {
                    SetState(UnitState.Idling);
                    _motor.StopMoving();
                    return;
                }

                // Проверяем дистанцию
                if (Vector3.Distance(transform.position, _targetTower.transform.position) < 2.0f)
                {
                    if (_targetTower.CanEnter())
                    {
                        _motor.StopMoving();
                        _targetTower.RequestEnter(_unit); // Стучимся в дверь
                        // При успешном входе юнит выключится (SetActive false), так что код дальше не важен
                    }
                    else
                    {
                        // Мест нет, просто стоим рядом
                        SetState(UnitState.Idling);
                    }
                }
            }
        }
    }
}