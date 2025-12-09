using System.Collections;
using UnityEngine;
using Unity.Netcode;
using WarOfCrowns.Core;
using WarOfCrowns.Data;

namespace WarOfCrowns.Units
{
    [RequireComponent(typeof(UnitMotor), typeof(Unit))]
    public class Fighter : NetworkBehaviour
    {
        [Header("Дальний бой")]
        [SerializeField] private GameObject arrowPrefab;

        [Header("Боевые параметры")]
        [SerializeField] private float baseParryChance = 0.3f; // 30% шанс блока

        private UnitMotor _motor;
        private Health _targetHealth;
        private Unit _unit;
        private UnitVisuals _visuals;
        private UnitAI _ai;
        private Coroutine _attackCoroutine;

        public Health CurrentTarget => _targetHealth;

        private void Awake()
        {
            _motor = GetComponent<UnitMotor>();
            _visuals = GetComponent<UnitVisuals>();
            _unit = GetComponent<Unit>();
            _ai = GetComponent<UnitAI>();
        }

        public void SetTarget(Health target) => Attack(target);
        public bool HasTarget() => _targetHealth != null && _targetHealth.CurrentHealth > 0;

        public void Attack(Health target)
        {
            if (_targetHealth == target && _attackCoroutine != null) return;
            Cancel();
            _targetHealth = target;
            _attackCoroutine = StartCoroutine(AttackRoutine());
        }

        public void Cancel()
        {
            if (_attackCoroutine != null) StopCoroutine(_attackCoroutine);
            _attackCoroutine = null;
            _targetHealth = null;
            if (_motor) _motor.StopMoving();
        }

        public void TryTakeHit(int damage, Unit attacker)
        {
            if (!IsServer) return;
            if (Random.value < baseParryChance)
            {
                ShowParryEffectClientRpc();
                return;
            }
            var h = GetComponent<Health>();
            if (h != null) h.TakeDamage(damage);
        }

        [ClientRpc]
        private void ShowParryEffectClientRpc()
        {
            if (_visuals) _visuals.TriggerParryEffect();
        }

        public void PerformAttackVisuals(Vector3 targetPos)
        {
            if (_visuals != null)
            {
                _visuals.FaceTarget(targetPos);
                _visuals.TriggerAttackAnimation();
            }
        }

        private IEnumerator AttackRoutine()
        {
            // Получаем коллайдер цели, чтобы не бежать в центр здания
            Collider2D targetCollider = _targetHealth.GetComponent<Collider2D>();

            while (_targetHealth != null && _targetHealth.CurrentHealth > 0)
            {
                ResourceType currentWeapon = _unit.Weapon;
                WeaponData stats = new WeaponData { damage = 5, attackSpeed = 1.5f, range = 1f };
                if (WorldState.Instance != null && WorldState.Instance.WeaponDB != null)
                    stats = WorldState.Instance.WeaponDB.GetWeaponStats(currentWeapon);

                Vector3 myPos = transform.position;

                // --- ВАЖНОЕ ИЗМЕНЕНИЕ: Куда бежать? ---
                // Если у цели есть коллайдер (Здание), бежим к краю. Если нет - к центру.
                Vector3 targetPoint = targetCollider != null
                    ? (Vector3)targetCollider.ClosestPoint(myPos)
                    : _targetHealth.transform.position;
                // -------------------------------------

                float dist = Vector3.Distance(myPos, targetPoint);

                // Дистанция атаки + небольшой запас (0.1f), чтобы не дергался
                if (dist > stats.range + 0.1f)
                {
                    if (_unit.Stance == UnitStance.Hold)
                    {
                        _motor.StopMoving();
                        yield return new WaitForSeconds(0.2f);
                        continue;
                    }
                    else
                    {
                        // Двигаемся к точке соприкосновения
                        _motor.MoveTo(targetPoint);
                    }
                }
                else
                {
                    // Мы пришли - АТАКА
                    _motor.StopMoving();

                    // Поворачиваемся к центру объекта
                    _unit.SetFacingDirection(_targetHealth.transform.position);
                    _unit.PlayAttackVisualsClientRpc(targetPoint); // Эффект удара в точку касания
                    _unit.ReduceDurability(false, 1);

                    if (stats.isRanged && stats.projectilePrefab)
                    {
                        GameObject arrow = Instantiate(stats.projectilePrefab, transform.position, Quaternion.identity);
                        if (arrow.TryGetComponent(out NetworkObject netArrow)) netArrow.Spawn();
                        arrow.GetComponent<Projectile>().Initialize(targetPoint, stats.damage);
                    }
                    else
                    {
                        yield return new WaitForSeconds(0.2f);
                        if (_targetHealth != null)
                        {
                            var enemyFighter = _targetHealth.GetComponent<Fighter>();
                            if (enemyFighter != null) enemyFighter.TryTakeHit(stats.damage, _unit);
                            else _targetHealth.TakeDamage(stats.damage); // Бьем здание или крестьянина без оружия
                        }
                    }
                    yield return new WaitForSeconds(stats.attackSpeed);
                }
                yield return null;
            }
            Cancel();
            if (_ai) _ai.SetState(UnitState.Idling);
        }
    }
}