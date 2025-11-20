using UnityEngine;

namespace WarOfCrowns.Core
{
    public class Health : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 100;
        private int _currentHealth;

        // Свойство, чтобы другие скрипты могли узнать текущее здоровье (для сохранения)
        public int CurrentHealth => _currentHealth;

        private void Start()
        {
            _currentHealth = maxHealth;
        }

        public void TakeDamage(int damageAmount)
        {
            _currentHealth -= damageAmount;
            // Debug.Log($"{gameObject.name} took {damageAmount} damage. Current HP: {_currentHealth}/{maxHealth}");

            if (_currentHealth <= 0)
            {
                Die();
            }
        }

        // Метод для восстановления здоровья при загрузке
        public void SetHealth(float amount)
        {
            _currentHealth = (int)amount;
            if (_currentHealth <= 0) Die();
        }

        private void Die()
        {
            // Debug.Log($"{gameObject.name} has died.");

            // Важно: Если это юнит, он сам сообщит о смерти через OnDestroy.
            // Если это здание или враг - тоже просто уничтожаем.
            Destroy(gameObject);
        }
    }
}