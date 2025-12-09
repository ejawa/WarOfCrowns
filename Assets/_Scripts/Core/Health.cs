using UnityEngine;
using Unity.Netcode;
using System;

namespace WarOfCrowns.Core
{
    public class Health : NetworkBehaviour
    {
        [Header("Настройки")]
        // Теперь ты можешь менять это число в Инспекторе у каждого префаба отдельно
        [SerializeField] private int maxHealth = 100;

        public NetworkVariable<int> currentHealth = new NetworkVariable<int>(100);

        public int MaxHealth => maxHealth; // Свойство для UI
        public int CurrentHealth => currentHealth.Value;

        public event Action<int, int> OnHealthChanged;
        public event Action OnDie;

        private bool _isDead = false;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                // СЕРВЕР: Берет число, которое ты написал в Инспекторе
                currentHealth.Value = maxHealth;
                _isDead = false;
            }

            currentHealth.OnValueChanged += (oldVal, newVal) =>
            {
                OnHealthChanged?.Invoke(newVal, maxHealth);
                if (newVal <= 0 && !_isDead)
                {
                    // Локальная реакция на смерть (если нужна)
                }
            };
        }

        public void TakeDamage(int damage)
        {
            if (!IsServer || _isDead) return;

            int newValue = currentHealth.Value - damage;
            currentHealth.Value = Mathf.Max(0, newValue);

            if (currentHealth.Value <= 0)
            {
                Die();
            }
        }

        public void SetHealth(int amount)
        {
            if (IsServer) currentHealth.Value = amount;
        }

        private void Die()
        {
            if (_isDead) return;
            _isDead = true;
            OnDie?.Invoke();

            if (TryGetComponent<NetworkObject>(out var netObj) && netObj.IsSpawned)
            {
                netObj.Despawn();
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}