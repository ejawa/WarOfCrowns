using UnityEngine;

namespace WarOfCrowns.Units
{
    // Требуем Rigidbody для физики
    [RequireComponent(typeof(Rigidbody2D))]
    public class UnitMotor : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;

        private Rigidbody2D _rb;
        private Vector3 _targetPosition;
        private bool _isMoving;

        public bool IsMoving => _isMoving;
        public Vector3 TargetPosition => _targetPosition;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _targetPosition = _rb.position; // Инициализируем текущей позицией
        }

        private void FixedUpdate()
        {
            if (!_isMoving)
            {
                // Если мы не должны двигаться, но нас толкнули - тормозим
                _rb.velocity = Vector2.zero;
                return;
            }

            // Проверка дистанции
            float distance = Vector2.Distance(_rb.position, _targetPosition);

            // Если мы очень близко - стоп
            if (distance < 0.1f)
            {
                StopMoving();
                return;
            }

            // Физическое движение
            Vector2 direction = ((Vector2)_targetPosition - _rb.position).normalized;
            Vector2 newPos = _rb.position + direction * moveSpeed * Time.fixedDeltaTime;

            _rb.MovePosition(newPos);
        }

        public void MoveTo(Vector3 destination)
        {
            destination.z = 0;
            _targetPosition = destination;
            _isMoving = true;
        }

        // Принудительная остановка
        public void StopMoving()
        {
            _isMoving = false;
            _rb.velocity = Vector2.zero; // Мгновенная остановка физики
            _rb.angularVelocity = 0f;
            _targetPosition = transform.position; // Сброс цели
        }
    }
}