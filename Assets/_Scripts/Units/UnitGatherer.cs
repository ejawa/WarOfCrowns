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
        private ResourceNode _currentTarget;
        private Coroutine _gatherCoroutine;

        [SerializeField] private float gatherDistance = 2f;
        [SerializeField] private float gatherRate = 1f;

        // --- СВОЙСТВО ДЛЯ СОХРАНЕНИЯ ---
        public ResourceNode CurrentTarget => _currentTarget;

        private void Awake()
        {
            _unit = GetComponent<Unit>();
            _motor = GetComponent<UnitMotor>();
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
            _currentTarget = null; // Важно сбросить цель
        }

        private IEnumerator GatherRoutine()
        {
            if (_currentTarget == null || _unit.OwningKingdom == null) yield break;

            _motor.MoveTo(_currentTarget.transform.position);

            while (Vector3.Distance(transform.position, _currentTarget.transform.position) > gatherDistance)
            {
                if (_currentTarget == null) yield break;
                yield return null;
            }

            _motor.MoveTo(transform.position);

            while (_currentTarget != null)
            {
                yield return new WaitForSeconds(gatherRate);
                if (_currentTarget == null) yield break;

                int gatheredAmount = _currentTarget.TakeHit();
                if (gatheredAmount > 0)
                {
                    _unit.OwningKingdom.AddResource(_currentTarget.resourceType, gatheredAmount);
                }
            }
            // Ресурс кончился - сбрасываем
            _currentTarget = null;
        }
    }
}