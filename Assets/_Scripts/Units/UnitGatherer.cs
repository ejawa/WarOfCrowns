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

        [SerializeField] private float gatherDistance = 2f;  // How close the unit needs to be to gather
        [SerializeField] private float gatherRate = 1f;      // Seconds per gather tick
        [SerializeField] private int gatherAmount = 10;      // Amount gathered per tick

        private void Awake()
        {
            _motor = GetComponent<UnitMotor>();
        }

        public void SetTarget(ResourceNode resourceNode)
        {
            _currentTarget = resourceNode;

            // Stop any previous gathering
            if (_gatherCoroutine != null)
            {
                StopCoroutine(_gatherCoroutine);
            }

            _gatherCoroutine = StartCoroutine(GatherRoutine());
        }

        private IEnumerator GatherRoutine()
        {
            // Step 1: Move to the target
            _motor.MoveTo(_currentTarget.transform.position);

            // Step 2: Wait until we are close enough
            while (Vector3.Distance(transform.position, _currentTarget.transform.position) > gatherDistance)
            {
                yield return null; // Wait for the next frame
            }

            // We have arrived, stop moving
            _motor.MoveTo(transform.position); // Command to stop

            // Step 3: Start gathering in a loop until the resource is depleted or we get a new order
            while (_currentTarget != null && _currentTarget.amount > 0)
            {
                yield return new WaitForSeconds(gatherRate); // Wait for gather time

                ResourceManager.Instance.AddResource(_currentTarget.resourceType, gatherAmount);
                _currentTarget.amount -= gatherAmount;

                // Optional: Destroy the node when it's empty
                if (_currentTarget.amount <= 0)
                {
                    Destroy(_currentTarget.gameObject);
                }
            }
        }
    }
}