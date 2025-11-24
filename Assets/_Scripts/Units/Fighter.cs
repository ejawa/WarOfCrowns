using System.Collections;
using UnityEngine;
using WarOfCrowns.Core;

namespace WarOfCrowns.Units
{
    [RequireComponent(typeof(UnitMotor))]
    public class Fighter : MonoBehaviour
    {
        [Header("Параметры")]
        [SerializeField] private int baseDamage = 10;
        [SerializeField] private float attackSpeed = 1.5f;

        [Header("Дальний бой")]
        [SerializeField] private GameObject arrowPrefab; // Префаб стрелы
        [SerializeField] private float rangedRange = 6.0f;

        private UnitMotor _motor;
        private Health _target;
        private Coroutine _attackCoroutine;
        private UnitVisuals _visuals;
        private Unit _unit; // Чтобы знать свое оружие

        private void Awake()
        {
            _motor = GetComponent<UnitMotor>();
            _visuals = GetComponent<UnitVisuals>();
            _unit = GetComponent<Unit>();
        }

        public void Attack(Health target)
        {
            Cancel();
            _target = target;
            _attackCoroutine = StartCoroutine(AttackRoutine());
        }

        public void Cancel()
        {
            if (_attackCoroutine != null) StopCoroutine(_attackCoroutine);
            _target = null;
        }

        private IEnumerator AttackRoutine()
        {
            if (attackSpeed <= 0.1f) attackSpeed = 0.5f;
            Collider2D targetCollider = _target.GetComponent<Collider2D>();

            while (_target != null)
            {
                // 1. Определяем режим боя
                bool isRanged = false;
                float currentAttackRange = 0.5f; // Ближний бой (в упор)
                int currentDamage = baseDamage;

                if (_unit != null)
                {
                    string weaponName = _unit.currentWeapon.ToString();
                    if (weaponName.Contains("Bow"))
                    {
                        isRanged = true;
                        currentAttackRange = rangedRange;
                    }
                    else if (weaponName.Contains("Sword") || weaponName.Contains("Spear"))
                    {
                        currentDamage += 10; // Бонус за оружие ближнего боя
                    }
                }

                // 2. Движение к цели
                Vector3 targetPoint = targetCollider != null ?
                    targetCollider.ClosestPoint(transform.position) :
                    _target.transform.position;

                float distance = Vector3.Distance(transform.position, targetPoint);

                if (distance > currentAttackRange)
                {
                    _motor.MoveTo(targetPoint);
                }
                else
                {
                    // 3. Атака
                    _motor.StopMoving();

                    if (_visuals != null)
                    {
                        _visuals.FaceTarget(_target.transform.position);
                        _visuals.TriggerAttackAnimation();
                    }

                    if (isRanged && arrowPrefab != null)
                    {
                        // Выстрел
                        GameObject arrow = Instantiate(arrowPrefab, transform.position, Quaternion.identity);
                        arrow.GetComponent<Projectile>().Initialize(_target.transform.position, currentDamage);
                    }
                    else
                    {
                        // Удар
                        _target.TakeDamage(currentDamage);
                    }

                    yield return new WaitForSeconds(attackSpeed);
                }

                yield return null;
            }
            Cancel();
        }
    }
}