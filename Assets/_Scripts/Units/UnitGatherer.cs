using System.Collections;
using UnityEngine;
using WarOfCrowns.Core;
using WarOfCrowns.World;

namespace WarOfCrowns.Units
{
    [RequireComponent(typeof(UnitMotor))]
    public class UnitGatherer : MonoBehaviour
    {
        private UnitMotor _motor;
        private ResourceNode _currentTarget;
        private Coroutine _gatherCoroutine;

        [SerializeField] private float gatherDistance = 2f;
        [SerializeField] private float gatherRate = 1f;
        [SerializeField] private int gatherAmountPerTick = 10;

        private void Awake()
        {
            _motor = GetComponent<UnitMotor>();
        }

        public void SetTarget(ResourceNode resourceNode)
        {
            StopGathering(); // Всегда останавливаем предыдущую задачу
            _currentTarget = resourceNode;
            _gatherCoroutine = StartCoroutine(GatherRoutine());
        }

        public void StopGathering()
        {
            if (_gatherCoroutine != null)
            {
                StopCoroutine(_gatherCoroutine);
                _gatherCoroutine = null;
            }
        }

        private IEnumerator GatherRoutine()
        {
            if (_currentTarget == null) yield break;

            _motor.MoveTo(_currentTarget.transform.position);

            while (Vector3.Distance(transform.position, _currentTarget.transform.position) > gatherDistance)
            {
                if (_currentTarget == null) yield break;
                yield return null;
            }

            _motor.MoveTo(transform.position); // Stop moving

            while (_currentTarget != null && _currentTarget.currentAmount > 0)
            {
                yield return new WaitForSeconds(gatherRate);
                int gatheredAmount = _currentTarget.Gather(gatherAmountPerTick);
                if (gatheredAmount > 0)
                {
                    ResourceManager.Instance.AddResource(_currentTarget.resourceType, gatheredAmount);
                }
                else
                {
                    yield break;
                }
            }
        }
    }
}