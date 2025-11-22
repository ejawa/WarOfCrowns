using System.Collections;
using UnityEngine;
using WarOfCrowns.Core;

namespace WarOfCrowns.Units
{
    [RequireComponent(typeof(UnitMotor))]
    public class Fighter : MonoBehaviour
    {
        [SerializeField] private int damage = 10;
        [SerializeField] private float attackSpeed = 1f;
        [SerializeField] private float attackRange = 1.5f; // Дистанция удара

        private UnitMotor _motor;
        private Health _target;
        private Coroutine _attackCoroutine;
        private UnitVisuals _visuals;

        private void Awake()
        {
            _motor = GetComponent<UnitMotor>();
            _visuals = GetComponent<UnitVisuals>();
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

            // Получаем коллайдер врага, чтобы знать, где его край
            Collider2D targetCollider = _target.GetComponent<Collider2D>();

            while (_target != null)
            {
                // Определяем точку, куда бить (Край или Центр)
                Vector3 targetPoint;
                if (targetCollider != null)
                {
                    targetPoint = targetCollider.ClosestPoint(transform.position);
                }
                else
                {
                    targetPoint = _target.transform.position;
                }

                float distance = Vector3.Distance(transform.position, targetPoint);

                // Если мы дальше, чем радиус атаки - идем ближе
                if (distance > attackRange) // attackRange можно поставить маленьким, например 0.5
                {
                    _motor.MoveTo(targetPoint);
                }
                else
                {
                    // Мы на расстоянии удара
                    _motor.MoveTo(transform.position); // Стоп

                    // Поворот к врагу
                    if (_visuals != null)
                    {
                        _visuals.FaceTarget(_target.transform.position);
                        _visuals.TriggerAttackAnimation();
                    }

                    // Урон
                    _target.TakeDamage(damage);

                    yield return new WaitForSeconds(attackSpeed);
                }

                yield return null;
            }

            Cancel();
        }
    }
}