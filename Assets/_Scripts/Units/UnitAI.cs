using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using WarOfCrowns.Buildings;
using WarOfCrowns.Core;
using WarOfCrowns.World;

namespace WarOfCrowns.Units
{
    public enum UnitState { Idling, MovingToTarget, Working, Building, SeekingFood, Fighting, Training, Garrisoning, Foraging, Fleeing, ReturningToPost, FetchingTool }

    [RequireComponent(typeof(Unit), typeof(UnitMotor))]
    public class UnitAI : NetworkBehaviour
    {
        public UnitState CurrentState { get; private set; }

        [Header("Настройки ИИ")]
        [SerializeField] private List<ResourceType> foodPriorityList;

        [Header("Блуждание (Idle)")]
        [SerializeField] private float wanderRadius = 4f;
        [SerializeField] private float minIdleWait = 3f;
        [SerializeField] private float maxIdleWait = 10f;

        [Header("Зрение и Бой")]
        public float viewRadius = 7f;
        [Range(0, 360)] public float viewAngle = 140f;
        public LayerMask enemyLayer;
        [SerializeField] private float proximitySenseRange = 3.0f;
        [SerializeField] private float fleeThreshold = 30f;
        [SerializeField] private float defensiveChaseLimit = 10f;

        private Unit _unit;
        private UnitMotor _motor;
        private UnitWorker _worker;
        private Fighter _fighter;
        private Health _health;
        private UnitBuilder _builder;

        private Coroutine _currentActionCoroutine;
        private Coroutine _idleCoroutine;

        private JobBuilding _jobToReturnTo;
        private Vector2 _facingDirection = Vector2.right;
        private Vector3 _guardPosition;

        private DefenseTower _targetTower;
        private Barracks _targetBarracks;
        private ResourceType _pendingWeapon;
        private ResourceNode _reservedResource;

        private float _fleeTimer = 0f;
        private Vector3 _fleeDirection;

        private void Awake()
        {
            _unit = GetComponent<Unit>();
            _motor = GetComponent<UnitMotor>();
            _worker = GetComponent<UnitWorker>();
            _fighter = GetComponent<Fighter>();
            _health = GetComponent<Health>();
            _builder = GetComponent<UnitBuilder>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _guardPosition = transform.position;
            if (IsServer) SetState(UnitState.Idling);
        }

        private void Start()
        {
            if (CurrentState == UnitState.SeekingFood) SeekFood();
            if (_health != null) _health.OnHealthChanged += OnTakeDamage;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (_health != null) _health.OnHealthChanged -= OnTakeDamage;
        }

        // --- КОМАНДЫ ---

        public void CommandMoveTo(Vector3 position)
        {
            if (_unit.isControlLocked) return;
            CancelAction();
            SetState(UnitState.MovingToTarget);
            _motor.MoveTo(position);
            _guardPosition = position;
        }

        public void CommandGather(ResourceNode resource)
        {
            if (_unit.isControlLocked) return;
            CancelAction();
            // Сбор ресурсов использует старую добрую рутину
            _currentActionCoroutine = StartCoroutine(PrepareForWorkRoutine(resource, "Gather"));
        }

        public void CommandBuild(ConstructionSite site)
        {
            if (_unit.isControlLocked) return;
            CancelAction();
            // Стройка использует НОВУЮ УМНУЮ рутину
            _currentActionCoroutine = StartCoroutine(SmartBuildRoutine(site));
        }

        public void CommandGarrison(DefenseTower tower)
        {
            if (_unit.isControlLocked) return;
            CancelAction();
            _targetTower = tower;
            SetState(UnitState.Garrisoning);
            _motor.MoveTo(tower.transform.position);
        }

        public void CommandGoTrain(Barracks barracks, ResourceType weapon)
        {
            if (_unit.isControlLocked) return;
            CancelAction();
            _targetBarracks = barracks;
            _pendingWeapon = weapon;
            SetState(UnitState.Training);
            _currentActionCoroutine = StartCoroutine(GoToBarracksRoutine());
        }

        public void SeekFood()
        {
            if (CurrentState == UnitState.SeekingFood || CurrentState == UnitState.Fighting || CurrentState == UnitState.Garrisoning) return;
            if (_worker && _worker.CurrentJob) _jobToReturnTo = _worker.CurrentJob;
            else _jobToReturnTo = null;
            CancelAction();
            SetState(UnitState.SeekingFood);
            // Заглушка
        }

        // --- УПРАВЛЕНИЕ СОСТОЯНИЯМИ ---

        public void SetState(UnitState newState)
        {
            if (!IsServer) return;

            if (CurrentState == UnitState.Idling && _idleCoroutine != null)
            {
                StopCoroutine(_idleCoroutine);
                _idleCoroutine = null;
            }

            CurrentState = newState;

            if (newState == UnitState.Idling)
            {
                _idleCoroutine = StartCoroutine(IdleWanderRoutine());
            }
        }

        public void CancelAction()
        {
            if (_builder != null) _builder.Cancel();
            if (_currentActionCoroutine != null) StopCoroutine(_currentActionCoroutine);
            if (_idleCoroutine != null) { StopCoroutine(_idleCoroutine); _idleCoroutine = null; }

            _unit.IsEating = false;

            if (_reservedResource != null)
            {
                _reservedResource.Unreserve();
                _reservedResource = null;
            }

            GetComponent<UnitGatherer>()?.StopGathering();
            if (_fighter != null) _fighter.Cancel();

            var worker = GetComponent<UnitWorker>();
            if (worker != null) worker.StopWorking();

            if (_motor) _motor.StopMoving();

            SetState(UnitState.Idling);
        }

        // --- ЛОГИКА ---

        private IEnumerator IdleWanderRoutine()
        {
            while (CurrentState == UnitState.Idling)
            {
                float waitTime = Random.Range(minIdleWait, maxIdleWait);
                yield return new WaitForSeconds(waitTime);

                if (CurrentState != UnitState.Idling || _unit.isControlLocked) yield break;

                Vector2 randomPoint = Random.insideUnitCircle * wanderRadius;
                Vector3 target = _guardPosition + (Vector3)randomPoint;

                if (WorldGenerator.Instance != null && !WorldGenerator.Instance.IsCellBuildable(Vector3Int.FloorToInt(target)))
                    continue;

                _motor.MoveTo(target);

                while (_motor.IsMoving && CurrentState == UnitState.Idling)
                    yield return null;
            }
        }

        // --- СТАРАЯ РУТИНА (ДЛЯ СБОРА РЕСУРСОВ) ---
        private IEnumerator PrepareForWorkRoutine(Component target, string taskType)
        {
            if (target == null) yield break;

            ResourceType requiredToolCategory = ResourceType.Wood;
            string targetName = "";

            if (taskType == "Gather" && target is ResourceNode node)
            {
                targetName = node.resourceType.ToString();
                if (targetName.Contains("Wood")) requiredToolCategory = ResourceType.WoodenAxe;
                else requiredToolCategory = ResourceType.WoodenPickaxe;

                // Бронирование слота
                if (!node.TryReserve())
                {
                    SetState(UnitState.Idling);
                    yield break;
                }
                _reservedResource = node;
            }

            // Проверка инструмента
            if (!_unit.IsToolSuitable(_unit.Tool, targetName))
            {
                if (_unit.HasBetterToolInStock(requiredToolCategory))
                {
                    SetState(UnitState.FetchingTool);
                    var storage = ToolStorageManager.Instance.GetNearestStorage(transform.position);
                    if (storage != null)
                    {
                        _motor.MoveTo(storage.transform.position);
                        while (Vector3.Distance(transform.position, storage.transform.position) > 2f && _motor.IsMoving)
                            yield return null;
                        _motor.StopMoving();
                        _unit.EquipBestTool(requiredToolCategory);
                        yield return new WaitForSeconds(0.5f);
                    }
                }
            }

            if (taskType == "Gather")
            {
                SetState(UnitState.Foraging);
                GetComponent<UnitGatherer>().StartWorkingOn(target as ResourceNode);
            }
        }

        // --- НОВАЯ УМНАЯ РУТИНА (ДЛЯ СТРОЙКИ) ---
        private IEnumerator SmartBuildRoutine(ConstructionSite site)
        {
            if (site == null) yield break;

            SetState(UnitState.Building);

            while (site != null)
            {
                // 1. Проверяем ресурсы
                ResourceType missingRes = site.GetMissingResource();

                if (missingRes == ResourceType.None)
                {
                    // --- РЕСУРСЫ ЕСТЬ: СТРОИМ ---
                    SetState(UnitState.Building);

                    // Подходим к стройке
                    Collider2D siteCol = site.GetComponent<Collider2D>();
                    Vector3 buildPos = siteCol ? siteCol.ClosestPoint(transform.position) : site.transform.position;

                    if (Vector3.Distance(transform.position, buildPos) > 1.6f)
                    {
                        _motor.MoveTo(buildPos);
                        while (site != null && Vector3.Distance(transform.position, buildPos) > 1.6f)
                            yield return null;
                    }
                    _motor.StopMoving();

                    if (site == null) break;

                    // Поворот и анимация
                    _unit.SetFacingDirection(site.transform.position);
                    _unit.PlayAttackVisualsClientRpc(site.transform.position);

                    float speed = _unit.GetToolSpeedMultiplier("Construction");
                    yield return new WaitForSeconds(1f / speed);

                    if (site == null) break;

                    bool finished = site.AddBuildProgress(1f);
                    _unit.ReduceDurability(true, 1);

                    if (finished)
                    {
                        SetState(UnitState.Idling);
                        yield break;
                    }
                }
                else
                {

                    // --- РЕСУРСОВ НЕТ: ДОБЫВАЕМ ---
                    ResourceNode targetNode = FindNearestResource(missingRes);
                    ResourceType requiredTool = ResourceType.Wood;
                    if (targetNode.resourceType.ToString().Contains("Wood")) requiredTool = ResourceType.WoodenAxe;
                    else requiredTool = ResourceType.WoodenPickaxe;

                    // Если инструмента нет, но он есть на складе -> Идем за ним
                    if (!_unit.IsToolSuitable(_unit.Tool, targetNode.resourceType.ToString()))
                    {
                        if (_unit.HasBetterToolInStock(requiredTool))
                        {
                            SetState(UnitState.FetchingTool);
                            var storage = ToolStorageManager.Instance.GetNearestStorage(transform.position);
                            if (storage != null)
                            {
                                _motor.MoveTo(storage.transform.position);
                                while (Vector3.Distance(transform.position, storage.transform.position) > 2f && _motor.IsMoving)
                                    yield return null;

                                _motor.StopMoving();
                                _unit.EquipBestTool(requiredTool);
                                yield return new WaitForSeconds(0.5f);

                                // После экипировки возвращаемся к состоянию Foraging
                                SetState(UnitState.Foraging);
                            }
                        }
                    }

                    if (targetNode != null)
                    {
                        SetState(UnitState.Foraging);
                        if (targetNode.TryReserve())
                        {
                            // Идем к ресурсу
                            while (targetNode != null && Vector3.Distance(transform.position, targetNode.transform.position) > 1.5f)
                            {
                                _motor.MoveTo(targetNode.transform.position);
                                yield return null;
                            }
                            _motor.StopMoving();

                            // Добываем, пока ресурс не появится на складе (5 ед.) или нода не кончится
                            while (targetNode != null && _unit.OwningKingdom.GetResourceAmount(missingRes) < 5)
                            {
                                _unit.SetFacingDirection(targetNode.transform.position);
                                _unit.PlayAttackVisualsClientRpc(targetNode.transform.position);

                                float speed = _unit.GetToolSpeedMultiplier(targetNode.resourceType.ToString());
                                yield return new WaitForSeconds(1f / speed);

                                if (targetNode == null) break;

                                int amount = targetNode.TakeHit();
                                if (amount > 0)
                                {
                                    _unit.OwningKingdom.AddResource(targetNode.resourceType, amount);
                                    _unit.ReduceDurability(true, 1);
                                }
                            }
                            if (targetNode != null) targetNode.Unreserve();
                        }
                        else
                        {
                            yield return new WaitForSeconds(1f); // Ждем место
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Нет ресурсов типа {missingRes} для стройки!");
                        yield return new WaitForSeconds(2f);
                    }
                }
            }
            SetState(UnitState.Idling);
        }

        private ResourceNode FindNearestResource(ResourceType type)
        {
            var nodes = FindObjectsOfType<ResourceNode>();
            ResourceNode nearest = null;
            float minDst = float.MaxValue;

            foreach (var node in nodes)
            {
                bool match = false;
                if (type == ResourceType.Wood && node.resourceType.ToString().Contains("Wood")) match = true;
                if (type == ResourceType.Stone && node.resourceType == ResourceType.Stone) match = true;

                if (match && node.CanReserve())
                {
                    float dst = Vector3.Distance(transform.position, node.transform.position);
                    if (dst < minDst)
                    {
                        minDst = dst;
                        nearest = node;
                    }
                }
            }
            return nearest;
        }

        private void OnTakeDamage(int current, int max)
        {
            if (!IsServer) return;
            float hpPercent = (float)current / max * 100f;

            if (CurrentState == UnitState.Garrisoning || CurrentState == UnitState.Fleeing) return;

            if (hpPercent < fleeThreshold)
            {
                if (CurrentState == UnitState.Fighting && _fighter != null && _fighter.HasTarget()) return;
                if (CurrentState == UnitState.Fleeing) { _fleeTimer = 3.0f; return; }
                StartFleeingLogic();
                return;
            }

            if (CurrentState != UnitState.Fighting) ScanForEnemies();
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

            if (CurrentState == UnitState.MovingToTarget && !_motor.IsMoving) SetState(UnitState.Idling);

            if (CurrentState == UnitState.Fighting && _unit.Stance == UnitStance.Defensive)
            {
                if (Vector3.Distance(transform.position, _guardPosition) > defensiveChaseLimit)
                {
                    CancelAction();
                    _motor.MoveTo(_guardPosition);
                    SetState(UnitState.ReturningToPost);
                }
            }

            if (CurrentState == UnitState.ReturningToPost)
            {
                if (!_motor.IsMoving || Vector3.Distance(transform.position, _guardPosition) < 0.5f) SetState(UnitState.Idling);
            }

            if (CurrentState == UnitState.Fleeing)
            {
                if (IsOwner)
                {
                    _fleeTimer -= Time.deltaTime;
                    if (_fleeTimer > 0) { Vector3 runPoint = transform.position + _fleeDirection * 3f; _motor.MoveTo(runPoint); }
                    else { SetState(UnitState.Idling); _motor.StopMoving(); }
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

            if (IsServer)
            {
                if (CurrentState != UnitState.SeekingFood &&
                    CurrentState != UnitState.Garrisoning &&
                    CurrentState != UnitState.Training &&
                    CurrentState != UnitState.Fleeing &&
                    CurrentState != UnitState.ReturningToPost)
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

                    if (DiplomacyManager.Instance != null)
                    {
                        DiplomacyManager.Instance.TriggerSurpriseWar(_unit.ownerKingdomID.Value, other.ownerKingdomID.Value);
                    }

                    float dist = Vector3.Distance(transform.position, hit.transform.position);
                    Vector2 dirToEnemy = (hit.transform.position - transform.position).normalized;
                    float angle = Vector2.Angle(_facingDirection, dirToEnemy);

                    if (angle < viewAngle / 2f || dist < proximitySenseRange)
                    {
                        var hp = hit.GetComponent<Health>();
                        if (hp != null && hp.CurrentHealth > 0)
                        {
                            if (_unit.Stance == UnitStance.Hold)
                            {
                                bool isRanged = _unit.Weapon.ToString().Contains("Bow");
                                float engageDist = isRanged ? 6f : 1.5f;
                                if (dist > engageDist) continue;
                            }

                            if (CurrentState != UnitState.Fighting)
                            {
                                CancelAction();
                                SetState(UnitState.Fighting);
                                if (_fighter) _fighter.Attack(hp);
                            }
                            return;
                        }
                    }
                }
            }
        }

        private IEnumerator GoToBarracksRoutine()
        {
            if (!_targetBarracks) yield break;
            _motor.MoveTo(_targetBarracks.transform.position);
            while (_targetBarracks && Vector3.Distance(transform.position, _targetBarracks.transform.position) > 1.5f) yield return null;
            _motor.StopMoving();
            if (_targetBarracks) _targetBarracks.TrainUnitServerRpc(_unit.NetworkObjectId, _pendingWeapon);
        }
    }
}