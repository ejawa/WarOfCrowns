using UnityEngine;

namespace WarOfCrowns.Units
{
    public class UnitMotor : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;

        private Vector3 _targetPosition;
        private bool _isMoving;

        private void Awake()
        {
            // Set initial target to current position to avoid moving at start
            _targetPosition = transform.position;
        }

        private void Update()
        {
            if (!_isMoving) return;

            // If we are very close to the target, stop moving
            if (Vector3.Distance(transform.position, _targetPosition) < 0.1f)
            {
                _isMoving = false;
                return;
            }

            // Move towards the target position
            transform.position = Vector3.MoveTowards(transform.position, _targetPosition, moveSpeed * Time.deltaTime);
        }

        public void MoveTo(Vector3 destination)
        {
            // We force the destination to be on the same Z plane as the unit
            destination.z = transform.position.z; // <-- ÃËÀÂÍÎÅ ÈÇÌÅÍÅÍÈÅ

            // We also keep ignoring Y in case of any weird physics interactions
            //destination.y = transform.position.y;

            _targetPosition = destination;
            _isMoving = true;
        }
    }
}