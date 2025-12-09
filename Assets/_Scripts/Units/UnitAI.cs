using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using WarOfCrowns.Buildings;
using WarOfCrowns.Core;
using WarOfCrowns.World;

namespace WarOfCrowns.Units
{
    public enum UnitState { Idling, MovingToTarget, Working, Building, SeekingFood, Fighting, Training, Garrisoning, Foraging, Fleeing, ReturningToPost }
    [RequireComponent(typeof(Unit), typeof(UnitMotor))]
    public class UnitAI : NetworkBehaviour
    {
        public UnitState CurrentState { get; private set; }

        [Header("Настройки ИИ")]
        [SerializeField] private List<ResourceType> foodPriorityList;

        [Header("Зрение и Бой")]
        public float viewRadius = 7f;
        [Range(0, 360)] public float viewAngle = 140f;
        public LayerMask enemyLayer;
        [Tooltip("Дистанция, на которой юнит чувствует врага спиной")]
        [SerializeField] private float proximitySenseRange = 3.0f;
        [SerializeField] private float fleeThreshold = 30f;

        // Дистанция преследования для Defensive стойки
        [SerializeField] private float defensiveChaseLimit = 10f;

        private Unit _unit;
        private UnitMotor _motor;
        private UnitWorker _worker;
        private Fighter _fighter;
        private Health _health;
        private Coroutine _currentActionCoroutine;

        // Память
        private JobBuilding _jobToReturnTo;
        private Vector2 _facingDirection = Vector2.right;
        private Vector3 _guardPosition; // Точка, которую мы охраняем (для Defensive)

        // Цели
        private DefenseTower _targetTower;
        private Barracks _targetBarracks;
        private ResourceType _pendingWeapon;
        private ResourceNode _reservedResource;

        // Логика бегства
        private float _fleeTimer = 0f;
        private Vector3 _fleeDirection;
        private UnitBuilder _builder;
        private void Awake()
        {
            _unit = GetComponent<Unit>();
            _motor = GetComponent<UnitMotor>();
            _worker = GetComponent<UnitWorker>();
            _fighter = GetComponent<Fighter>();
            _health = GetComponent<Health>();
            _builder = GetComponent<UnitBuilder>();
        }

        private void Start()
        {
            if (CurrentState == UnitState.SeekingFood) SeekFood();
            if (_health != null) _health.OnHealthChanged += OnTakeDamage;
            _guardPosition = transform.position; // Изначально охраняем спавн
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (_health != null) _health.OnHealthChanged -= OnTakeDamage;
        }

        public void CommandMoveTo(Vector3 position)
        {
            CancelAction();
            SetState(UnitState.MovingToTarget);
            _motor.MoveTo(position);
            // Запоминаем новую точку охраны
            _guardPosition = position;
        }

        public void SetState(UnitState newState)
        {
            CurrentState = newState;
        }
        public void CommandBuild(ConstructionSite site)
        {
            CancelAction(); // Сбрасываем всё
            SetState(UnitState.Building); // Ставим состояние
            if (_builder != null) _builder.SetTarget(site);
        }
        public void CancelAction()
        {
            if (_builder != null) _builder.Cancel();
            if (_currentActionCoroutine != null)
                StopCoroutine(_currentActionCoroutine);

            _unit.IsEating = false;

            if (_reservedResource != null)
            {
                _reservedResource.Unreserve();
                _reservedResource = null;
            }

            // Останавливаем все подсистемы
            GetComponent<UnitGatherer>()?.StopGathering();

            // ВАЖНО: Принудительная отмена стройки
            var builder = GetComponent<UnitBuilder>();
            if (builder != null) builder.Cancel();

            if (_fighter != null) _fighter.Cancel();

            var worker = GetComponent<UnitWorker>();
            if (worker != null) worker.StopWorking();

            if (_motor) _motor.StopMoving();

            SetState(UnitState.Idling);
        }

        private void OnTakeDamage(int current, int max)
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

            float hpPercent = (float)current / max * 100f;
            if (hpPercent < fleeThreshold && CurrentState != UnitState.Garrisoning)
            {
                if (CurrentState == UnitState.Fighting && _fighter != null && _fighter.HasTarget()) return;

                if (CurrentState == UnitState.Fleeing)
                {
                    _fleeTimer = 3.0f;
                    return;
                }
                StartFleeingLogic();
                return;
            }

            if (CurrentState == UnitState.Fighting && _fighter != null)
            {
                if (_fighter.HasTarget()) return;
            }

            if (CurrentState == UnitState.Garrisoning || CurrentState == UnitState.Fleeing) return;

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 10f, enemyLayer);
            foreach (var hit in hits)
            {
                Unit other = hit.GetComponent<Unit>();
                if (other != null && other.ownerKingdomID.Value != _unit.ownerKingdomID.Value)
                {
                    CancelAction();
                    SetState(UnitState.Fighting);
                    if (_fighter) _fighter.Attack(hit.GetComponent<Health>());
                    return;
                }
            }
        }

        private void StartFleeingLogic()
        {
            Collider2D enemy = Physics2D.OverlapCircle(transform.position, 10f, enemyLayer);
            Vector3 dir = Vector3.right;
            if (enemy != null) dir = (transform.position - enemy.transform.position).normalized;
            else dir = -_facingDirection;

            CancelAction();
            SetState(UnitState.Fleeing);
            StartFleeingClientRpc(dir);
        }

        [ClientRpc]
        private void StartFleeingClientRpc(Vector3 direction)
        {
            CancelAction();
            SetState(UnitState.Fleeing);
            _fleeDirection = direction;
            _fleeTimer = 4.0f;
        }

        private void Update()
        {
            if (_motor.IsMoving)
            {
                Vector3 dir = (_motor.TargetPosition - transform.position).normalized;
                if (dir != Vector3.zero) _facingDirection = dir;
            }

            if (CurrentState == UnitState.MovingToTarget)
            {
                if (!_motor.IsMoving) SetState(UnitState.Idling);
            }

            // --- ЛОГИКА DEFENSIVE: ВОЗВРАТ НА ПОСТ ---
            if (CurrentState == UnitState.Fighting && _unit.Stance == UnitStance.Defensive)
            {
                // Если мы отошли слишком далеко от поста в пылу битвы
                if (Vector3.Distance(transform.position, _guardPosition) > defensiveChaseLimit)
                {
                    // Бросаем врага и возвращаемся
                    CancelAction();
                    _motor.MoveTo(_guardPosition);
                    SetState(UnitState.ReturningToPost);
                }
            }

            if (CurrentState == UnitState.ReturningToPost)
            {
                if (!_motor.IsMoving || Vector3.Distance(transform.position, _guardPosition) < 0.5f)
                {
                    SetState(UnitState.Idling);
                }
            }
            // -----------------------------------------

            if (CurrentState == UnitState.Fleeing)
            {
                if (IsOwner)
                {
                    _fleeTimer -= Time.deltaTime;
                    if (_fleeTimer > 0)
                    {
                        Vector3 runPoint = transform.position + _fleeDirection * 3f;
                        _motor.MoveTo(runPoint);
                    }
                    else
                    {
                        SetState(UnitState.Idling);
                        _motor.StopMoving();
                    }
                }
            }

            if (CurrentState == UnitState.Garrisoning)
            {
                if (!_targetTower) { SetState(UnitState.Idling); return; }
                if (Vector3.Distance(transform.position, _targetTower.transform.position) < 2.0f)
                {
                    if (_targetTower.CanEnter()) { _motor.StopMoving(); _targetTower.RequestEnter(_unit); }
                    else SetState(UnitState.Idling);
                }
            }

            if (NetworkManager.Singleton.IsServer)
            {
                // Сканируем врагов, только если свободны
                if (CurrentState != UnitState.SeekingFood &&
                    CurrentState != UnitState.Garrisoning &&
                    CurrentState != UnitState.Training &&
                    CurrentState != UnitState.Fighting &&
                    CurrentState != UnitState.Fleeing &&
                    CurrentState != UnitState.MovingToTarget &&
                    CurrentState != UnitState.ReturningToPost) // Не отвлекаемся, если бежим домой
                {
                    ScanForEnemies();
                }
            }
        }

        private void ScanForEnemies()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, viewRadius, enemyLayer);
            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;
                Unit other = hit.GetComponent<Unit>();
                if (other != null)
                {
                    if (other.ownerKingdomID.Value == _unit.ownerKingdomID.Value) continue;

                    float dist = Vector3.Distance(transform.position, hit.transform.position);
                    Vector2 dirToEnemy = (hit.transform.position - transform.position).normalized;
                    float angle = Vector2.Angle(_facingDirection, dirToEnemy);

                    // Если видим или слышим
                    if (angle < viewAngle / 2f || dist < proximitySenseRange)
                    {
                        var hp = hit.GetComponent<Health>();
                        if (hp != null && hp.CurrentHealth > 0)
                        {
                            // --- ПРОВЕРКА СТОЙКИ ПЕРЕД АТАКОЙ ---
                            if (_unit.Stance == UnitStance.Hold)
                            {
                                // В режиме Hold атакуем ТОЛЬКО если враг уже в радиусе оружия
                                // Для этого нужно знать дальность оружия. Пока берем условно 1.5м или 6м для лука.
                                // Лучше проверять через базу данных, но упростим:
                                // Если дальний бой (лук) - стреляем. Если ближний - ждем упора.
                                bool isRanged = _unit.Weapon.ToString().Contains("Bow");
                                float engageDist = isRanged ? 6f : 1.5f;
                                if (dist > engageDist) continue; // Игнорируем далеких врагов
                            }
                            // ------------------------------------

                            CancelAction();
                            SetState(UnitState.Fighting);
                            if (_fighter) _fighter.Attack(hp);
                            return;
                        }
                    }
                }
            }
        }

        public void CommandGarrison(DefenseTower tower)
        {
            CancelAction(); _targetTower = tower; SetState(UnitState.Garrisoning); _motor.MoveTo(tower.transform.position);
        }

        public void CommandGoTrain(Barracks barracks, ResourceType weapon)
        {
            CancelAction(); _targetBarracks = barracks; _pendingWeapon = weapon; SetState(UnitState.Training); _currentActionCoroutine = StartCoroutine(GoToBarracksRoutine());
        }

        private IEnumerator GoToBarracksRoutine()
        {
            if (!_targetBarracks) yield break;
            _motor.MoveTo(_targetBarracks.transform.position);
            while (_targetBarracks && Vector3.Distance(transform.position, _targetBarracks.transform.position) > 1.5f) yield return null;
            _motor.StopMoving();
            if (_targetBarracks) _targetBarracks.TrainUnitServerRpc(_unit.NetworkObjectId, _pendingWeapon);
        }

        public void SeekFood()
        {
            if (CurrentState == UnitState.SeekingFood || CurrentState == UnitState.Fighting || CurrentState == UnitState.Garrisoning) return;
            if (_worker && _worker.CurrentJob) _jobToReturnTo = _worker.CurrentJob; else _jobToReturnTo = null;
            CancelAction(); SetState(UnitState.SeekingFood); _currentActionCoroutine = StartCoroutine(SeekFoodRoutine());
        }

        private IEnumerator SeekFoodRoutine()
        {
            // Здесь должна быть логика еды (без изменений)
            yield break;
        }
    }
}