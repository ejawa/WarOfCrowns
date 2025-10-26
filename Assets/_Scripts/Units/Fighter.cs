using System.Collections;
using UnityEngine;
using WarOfCrowns.Core;

namespace WarOfCrowns.Units
{
    [RequireComponent(typeof(UnitMotor))]
    public class Fighter : MonoBehaviour
    {
        [SerializeField] private int damage = 10;
        [SerializeField] private float attackSpeed = 1f; // Time between attacks
        [SerializeField] private float attackRange = 1.5f;

        private UnitMotor _motor;
        private Health _target;
        private Coroutine _attackCoroutine;

        private void Awake()
        {
            _motor = GetComponent<UnitMotor>();
        }

        public void Attack(Health target)
        {
            Cancel(); // Stop previous attack
            _target = target;
            _attackCoroutine = StartCoroutine(AttackRoutine());
        }

        public void Cancel()
        {
            if (_attackCoroutine != null)
            {
                StopCoroutine(_attackCoroutine);
            }
        }

        private IEnumerator AttackRoutine()
        {
            while (_target != null)
            {
                // Move towards target if not in range
                if (Vector3.Distance(transform.position, _target.transform.position) > attackRange)
                {
                    _motor.MoveTo(_target.transform.position);
                }
                else // If in range, stop and attack
                {
                    _motor.MoveTo(transform.position); // Stop moving

                    // Perform attack
                    Debug.Log($"Attacking target '{_target.name}'");
                    _target.TakeDamage(damage);

                    // Wait for attack speed cooldown
                    yield return new WaitForSeconds(attackSpeed);
                }
                yield return null; // Wait for next frame
            }
        }
    }
}