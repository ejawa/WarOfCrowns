using System.Collections;
using UnityEngine;
using WarOfCrowns.Core;
using WarOfCrowns.World;

namespace WarOfCrowns.Units
{
    [RequireComponent(typeof(UnitMotor), typeof(Unit))]
    public class UnitGatherer : MonoBehaviour
    {
        private Unit _unit;
        private UnitMotor _motor;
        private UnitVisuals _visuals; // Ссылка на визуал
        private ResourceNode _currentTarget;
        private Coroutine _gatherCoroutine;

        [SerializeField] private float gatherDistance = 0.5f; // Дистанция от края коллайдера
        [SerializeField] private float gatherRate = 1f;

        // Свойство для сохранения
        public ResourceNode CurrentTarget => _currentTarget;

        private void Awake()
        {
            _unit = GetComponent<Unit>();
            _motor = GetComponent<UnitMotor>();
            _visuals = GetComponent<UnitVisuals>();
        }

        public void SetTarget(ResourceNode resourceNode)
        {
            StopGathering();
            _currentTarget = resourceNode;
            _gatherCoroutine = StartCoroutine(GatherRoutine());
        }

        public void StopGathering()
        {
            if (_gatherCoroutine != null) StopCoroutine(_gatherCoroutine);
            _gatherCoroutine = null;
            _currentTarget = null;
        }

        private IEnumerator GatherRoutine()
        {
            if (_currentTarget == null || _unit.OwningKingdom == null) yield break;

            // Получаем коллайдер ресурса
            Collider2D targetCollider = _currentTarget.GetComponent<Collider2D>();

            // --- 1. ДВИЖЕНИЕ К КРАЮ РЕСУРСА ---
            while (true)
            {
                if (_currentTarget == null) yield break;

                Vector3 targetPoint;
                if (targetCollider != null)
                {
                    // Идем к ближайшей точке на краю коллайдера
                    targetPoint = targetCollider.ClosestPoint(transform.position);
                }
                else
                {
                    // Если коллайдера нет (редкость), идем в центр
                    targetPoint = _currentTarget.transform.position;
                }

                // Проверяем дистанцию
                float distance = Vector3.Distance(transform.position, targetPoint);
                if (distance <= gatherDistance)
                {
                    break; // Пришли!
                }

                _motor.MoveTo(targetPoint);
                yield return null;
            }

            _motor.MoveTo(transform.position); // Стоп

            // --- 2. ПРОЦЕСС СБОРА ---
            while (_currentTarget != null)
            {
                // Поворачиваемся к ресурсу перед ударом
                if (_visuals != null)
                {
                    _visuals.FaceTarget(_currentTarget.transform.position);
                    _visuals.TriggerAttackAnimation();
                }

                yield return new WaitForSeconds(gatherRate); // Замах / Время добычи

                if (_currentTarget == null) yield break;

                // Наносим удар
                int gatheredAmount = _currentTarget.TakeHit();

                if (gatheredAmount > 0)
                {
                    ResourceType gatheredType = _currentTarget.resourceType;

                    // Кладем на склад
                    _unit.OwningKingdom.AddResource(gatheredType, gatheredAmount);

                    // Если это еда (Ягоды), FoodConverter сам это увидит через события Kingdom
                    Debug.Log($"Gathered {gatheredAmount} {gatheredType}");
                }
            }

            _currentTarget = null;
        }
    }
}