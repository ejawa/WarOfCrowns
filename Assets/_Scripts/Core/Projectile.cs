using UnityEngine;
using System.Collections;

namespace WarOfCrowns.Core
{
    public class Projectile : MonoBehaviour
    {
        [Header("Настройки")]
        [SerializeField] private float speed = 10f;
        [SerializeField] private float arcHeight = 2.0f; // Высота дуги
        [SerializeField] private GameObject hitEffectPrefab;

        private Vector3 _startPos;
        private Vector3 _targetPos;
        private int _damage;
        private float _progress; // От 0 до 1
        private bool _isInitialized = false;

        // Мы больше не храним Health target, мы храним ТОЧКУ
        public void Initialize(Vector3 targetPosition, int damage)
        {
            _startPos = transform.position;
            _targetPos = targetPosition;
            _damage = damage;
            _progress = 0f;
            _isInitialized = true;

            // Рассчитываем поворот сразу на цель (для старта)
            RotateTowards(_targetPos);
        }

        private void Update()
        {
            if (!_isInitialized) return;

            // Двигаем прогресс линейно в зависимости от скорости и расстояния
            float distance = Vector3.Distance(_startPos, _targetPos);
            if (distance <= 0.01f) distance = 0.01f; // Защита от деления на 0

            // step = скорость / расстояние * время
            _progress += (speed / distance) * Time.deltaTime;

            if (_progress >= 1.0f)
            {
                Impact();
                return;
            }

            // --- МАТЕМАТИКА ПАРАБОЛЫ ---
            // 1. Линейная позиция (по прямой)
            Vector3 currentPos = Vector3.Lerp(_startPos, _targetPos, _progress);

            // 2. Добавляем высоту (дугу)
            // Mathf.Sin(Mathf.PI * _progress) дает дугу от 0 до 1 и обратно до 0
            float height = Mathf.Sin(Mathf.PI * _progress) * arcHeight;

            // В 2D Top-Down "высота" - это ось Y. Но так как мы летим по карте, 
            // нам нужно просто визуально сместить спрайт. 
            // НО! Если мы сместим transform.position.y, это собьет физику.
            // Самый простой способ в 2D - просто прибавить это к Y.
            currentPos.y += height;

            // 3. Поворот (смотрим туда, куда летим в следующем кадре)
            Vector3 nextPosLinear = Vector3.Lerp(_startPos, _targetPos, _progress + 0.05f);
            float nextHeight = Mathf.Sin(Mathf.PI * (_progress + 0.05f)) * arcHeight;
            Vector3 nextPos = nextPosLinear;
            nextPos.y += nextHeight;

            RotateTowards(nextPos);

            transform.position = currentPos;
        }

        private void RotateTowards(Vector3 target)
        {
            Vector3 dir = target - transform.position;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }

        private void Impact()
        {
            // Мы прилетели в точку назначения.
            // Проверяем, есть ли там кто-нибудь (урон по области или точечный)

            // Ищем врагов в радиусе 0.5м от точки падения
            Collider2D hit = Physics2D.OverlapCircle(_targetPos, 0.5f);

            if (hit != null && hit.TryGetComponent<Health>(out var enemyHealth))
            {
                // Наносим урон
                enemyHealth.TakeDamage(_damage);
            }

            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, _targetPos, Quaternion.identity);
            }

            Destroy(gameObject);
        }
    }
}