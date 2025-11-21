using UnityEngine;

namespace WarOfCrowns.Units
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class UnitMotor : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;

        private Rigidbody2D _rb;
        private Vector3 _targetPosition;
        private bool _isMoving;

        // --- ÑÂÎÉÑÒÂÀ ÄËß ÑÎÕÐÀÍÅÍÈß ---
        public bool IsMoving => _isMoving;
        public Vector3 TargetPosition => _targetPosition;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _targetPosition = transform.position;
        }

        private void FixedUpdate()
        {
            if (!_isMoving)
            {
                _rb.velocity = Vector2.zero;
                return;
            }

            float distance = Vector3.Distance(transform.position, _targetPosition);
            if (distance < 0.1f)
            {
                _isMoving = false;
                _rb.velocity = Vector2.zero;
                return;
            }

            Vector2 direction = (_targetPosition - transform.position).normalized;
            _rb.MovePosition(_rb.position + direction * moveSpeed * Time.fixedDeltaTime);
        }

        public void MoveTo(Vector3 destination)
        {
            destination.z = 0;
            _targetPosition = destination;
            _isMoving = true;
        }
    }
}