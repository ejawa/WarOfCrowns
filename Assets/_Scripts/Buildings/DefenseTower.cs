using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using WarOfCrowns.Core;
using WarOfCrowns.Units;

namespace WarOfCrowns.Buildings
{
    [RequireComponent(typeof(Building))]
    public class DefenseTower : MonoBehaviour
    {
        [Header("Настройки Гарнизона")]
        public int maxGarrison = 3;
        [SerializeField] private Transform entrancePoint;
        [SerializeField] private Transform shootPoint; // Верхушка башни, откуда летят стрелы

        [Header("Боевые Параметры")]
        [SerializeField] private float range = 8f;
        [SerializeField] private float fireRate = 2f;
        [SerializeField] private int damagePerUnit = 15;
        [SerializeField] private GameObject projectilePrefab; // Префаб стрелы

        private List<Unit> _garrison = new List<Unit>();
        private float _fireTimer;
        private Building _building;

        private void Awake() { _building = GetComponent<Building>(); }

        public bool CanEnter() => _garrison.Count < maxGarrison;

        private void Update()
        {
            // Башня стреляет, только если внутри кто-то есть
            if (_garrison.Count > 0)
            {
                _fireTimer -= Time.deltaTime;
                if (_fireTimer <= 0)
                {
                    _fireTimer = fireRate;
                    TryShoot();
                }
            }
        }

        public void AddUnit(Unit unit)
        {
            if (!CanEnter()) return;

            _garrison.Add(unit);
            unit.gameObject.SetActive(false); // Прячем юнита
            unit.transform.position = transform.position;

            // Debug.Log($"Tower: Unit {unit.unitName} entered. Garrison: {_garrison.Count}/{maxGarrison}");
        }

        public void EjectAll()
        {
            foreach (var unit in _garrison)
            {
                if (entrancePoint != null) unit.transform.position = entrancePoint.position;
                unit.gameObject.SetActive(true);
                if (unit.TryGetComponent<UnitAI>(out var ai)) ai.SetState(UnitState.Idling);
            }
            _garrison.Clear();
        }

        private void TryShoot()
        {
            // Ищем ближайшего врага
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range);
            Health nearestEnemy = null;
            float minDst = float.MaxValue;

            foreach (var hit in hits)
            {
                // Проверяем тег Enemy (убедись, что он назначен на врагах)
                if (hit.CompareTag("Enemy"))
                {
                    float dst = Vector3.Distance(transform.position, hit.transform.position);
                    if (dst < minDst)
                    {
                        minDst = dst;
                        nearestEnemy = hit.GetComponent<Health>();
                    }
                }
            }

            if (nearestEnemy != null)
            {
                StartCoroutine(VolleyFire(nearestEnemy));
            }
        }

        private IEnumerator VolleyFire(Health target)
        {
            // Стреляем столько раз, сколько людей в гарнизоне
            for (int i = 0; i < _garrison.Count; i++)
            {
                if (target == null) break; // Если враг умер во время залпа - стоп

                if (projectilePrefab != null && shootPoint != null)
                {
                    GameObject arrow = Instantiate(projectilePrefab, shootPoint.position, Quaternion.identity);

                    // --- ИСПРАВЛЕНИЕ ЗДЕСЬ ---
                    // Используем правильные имена переменных: target и damagePerUnit
                    arrow.GetComponent<Projectile>().Initialize(target.transform.position, damagePerUnit);
                    // -------------------------
                }

                yield return new WaitForSeconds(0.2f); // Задержка между выстрелами
            }
        }

        // Рисуем радиус в редакторе
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, range);
        }
    }
}