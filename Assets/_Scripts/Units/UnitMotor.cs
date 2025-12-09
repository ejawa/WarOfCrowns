using UnityEngine;
using Unity.Netcode;
using WarOfCrowns.World; // Нужно для WorldGenerator

namespace WarOfCrowns.Units
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Unit))]
    public class UnitMotor : NetworkBehaviour
    {
        [SerializeField] private float moveSpeed = 4f;
        private Rigidbody2D _rb;
        private Vector3 _targetPosition;

        public bool IsMoving { get; private set; }
        public Vector3 TargetPosition => _targetPosition;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        public override void OnNetworkSpawn()
        {
            _targetPosition = transform.position;
        }

        private void FixedUpdate()
        {
            if (!IsOwner) return;

            if (IsMoving)
            {
                // Рассчитываем следующую позицию
                Vector2 nextPos = Vector2.MoveTowards(_rb.position, _targetPosition, moveSpeed * Time.fixedDeltaTime);

                // --- ПРОВЕРКА ПРОХОДИМОСТИ ---
                if (WorldGenerator.Instance != null)
                {
                    string biome = WorldGenerator.Instance.GetBiomeAt(nextPos);
                    // Если следующий шаг в гору/скалу (вода допустима, т.к. есть плавание)
                    // Но если ты хочешь запретить и воду без лодок - добавь Water сюда.
                    if (biome.Contains("Mountain") || biome.Contains("Rock"))
                    {
                        // Уперлись в стену
                        StopMoving();
                        return;
                    }
                }
                // -----------------------------

                _rb.MovePosition(nextPos);

                if (Vector2.Distance(_rb.position, _targetPosition) < 0.05f)
                {
                    StopMoving();
                }
            }
            else
            {
                _rb.velocity = Vector2.zero;
            }
        }

        public void MoveTo(Vector3 destination)
        {
            destination.z = transform.position.z;
            if (IsOwner)
            {
                StartMovingLocally(destination);
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
                _targetPosition = transform.position;
                _rb.velocity = Vector2.zero;
            }
            else if (IsServer)
            {
                StopMovingClientRpc();
            }
        }

        private void StartMovingLocally(Vector3 pos) { _targetPosition = pos; IsMoving = true; }
        [ServerRpc] private void MoveToServerRpc(Vector3 pos) { _targetPosition = pos; IsMoving = true; }
        [ClientRpc] private void MoveToClientRpc(Vector3 pos) { if (IsOwner) StartMovingLocally(pos); }
        [ClientRpc] private void StopMovingClientRpc() { if (IsOwner) StopMoving(); }
    }
}