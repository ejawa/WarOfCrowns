using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using WarOfCrowns.Core;

namespace WarOfCrowns.Units
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Unit))]
    public class UnitMotor : NetworkBehaviour
    {
        [SerializeField] private float moveSpeed = 4f;

        [Header("Плавность движения")]
        [Tooltip("Скорость поворота (чем меньше, тем шире дуга). Оптимально 10-15.")]
        [SerializeField] private float turnSpeed = 12f;
        [SerializeField] private float stopDistance = 0.2f;

        [Header("Живое движение (Синусоида)")]
        [SerializeField] private bool enableWobble = true;
        [SerializeField] private float wobbleFrequency = 5f;
        [SerializeField] private float wobbleAmplitude = 0.1f;

        private Rigidbody2D _rb;
        private List<Vector3> _currentPath;
        private int _currentPathIndex;
        private Vector3 _currentVelocityVector; // Текущий вектор движения

        public bool IsMoving { get; private set; }
        // Возвращаем точку чуть впереди для плавности поворота визуалов
        public Vector3 TargetPosition => IsMoving ? transform.position + _currentVelocityVector : transform.position;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        private void FixedUpdate()
        {
            if (!IsOwner) return;

            if (IsMoving && _currentPath != null && _currentPathIndex < _currentPath.Count)
            {
                Vector3 targetNode = _currentPath[_currentPathIndex];

                // --- 1. Виляние (Wobble) ---
                Vector3 wobbleOffset = Vector3.zero;
                if (enableWobble)
                {
                    Vector3 direction = (targetNode - transform.position).normalized;
                    Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0);
                    wobbleOffset = perpendicular * Mathf.Sin(Time.time * wobbleFrequency) * wobbleAmplitude;
                }
                Vector3 finalTarget = targetNode + wobbleOffset;
                // ---------------------------

                // --- 2. Расчет вектора ---
                Vector3 dirToTarget = (finalTarget - transform.position).normalized;
                Vector3 desiredVelocity = dirToTarget * moveSpeed;

                // --- 3. Плавный поворот вектора скорости (БЕЗ ТОРМОЖЕНИЯ) ---
                // Lerp плавно меняет текущий вектор на желаемый.
                // Это создает дугу, но сохраняет скорость.
                _currentVelocityVector = Vector3.Lerp(_currentVelocityVector, desiredVelocity, turnSpeed * Time.fixedDeltaTime);

                // Применяем движение
                _rb.MovePosition(transform.position + _currentVelocityVector * Time.fixedDeltaTime);

                // --- 4. Проверка достижения точки ---
                float dist = Vector2.Distance(transform.position, targetNode);

                // Если это последняя точка - тормозим точнее
                bool isLastPoint = _currentPathIndex >= _currentPath.Count - 1;
                float currentStopDist = isLastPoint ? 0.05f : stopDistance;

                if (dist < currentStopDist)
                {
                    _currentPathIndex++;
                    if (_currentPathIndex >= _currentPath.Count)
                    {
                        StopMoving();
                    }
                }
            }
            else
            {
                IsMoving = false;
                _rb.velocity = Vector2.zero;
            }
        }

        public void MoveTo(Vector3 destination)
        {
            destination.z = transform.position.z;

            if (IsOwner)
            {
                StartPathfindingLocally(destination);
                if (!IsServer) MoveToServerRpc(destination);
            }
            else if (IsServer)
            {
                MoveToClientRpc(destination);
            }
        }

        public void StopMoving()
        {
            if (IsOwner)
            {
                IsMoving = false;
                _currentPath = null;
                _rb.velocity = Vector2.zero;
                _currentVelocityVector = Vector3.zero; // Сброс инерции
            }
            else if (IsServer)
            {
                StopMovingClientRpc();
            }
        }

        private void StartPathfindingLocally(Vector3 destination)
        {
            if (Pathfinder.Instance == null)
            {
                _currentPath = new List<Vector3> { destination };
            }
            else
            {
                _currentPath = Pathfinder.Instance.FindPath(transform.position, destination);
                if (_currentPath == null || _currentPath.Count == 0)
                {
                    StopMoving();
                    return;
                }
            }

            _currentPathIndex = 0;
            IsMoving = true;
            // Инициализируем вектор скорости сразу в сторону цели, чтобы не было рывка на старте
            if (_currentPath.Count > 0)
                _currentVelocityVector = (_currentPath[0] - transform.position).normalized * moveSpeed;
        }

        [ServerRpc] private void MoveToServerRpc(Vector3 pos) { StartPathfindingLocally(pos); }
        [ClientRpc] private void MoveToClientRpc(Vector3 pos) { if (IsOwner) StartPathfindingLocally(pos); }
        [ClientRpc] private void StopMovingClientRpc() { if (IsOwner) StopMoving(); }
    }
}