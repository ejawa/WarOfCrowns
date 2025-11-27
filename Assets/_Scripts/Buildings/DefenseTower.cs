using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode; // Важно
using WarOfCrowns.Units;
using WarOfCrowns.Core;

namespace WarOfCrowns.Buildings
{
    [RequireComponent(typeof(Building))]
    public class DefenseTower : NetworkBehaviour
    {
        public int maxGarrison = 3;
        [SerializeField] private Transform entrancePoint;
        [SerializeField] private Transform shootPoint;

        [Header("Боевые Параметры")]
        [SerializeField] private float range = 8f;
        [SerializeField] private float fireRate = 2f;
        [SerializeField] private int damagePerUnit = 15;
        [SerializeField] private GameObject projectilePrefab;

        // Список гарнизона (только на сервере)
        private List<Unit> _garrison = new List<Unit>();
        private float _fireTimer;

        public bool CanEnter() => _garrison.Count < maxGarrison;
        public int GetGarrisonCount() => _garrison.Count;

        private void Update()
        {
            if (!IsServer) return; // Стреляет только сервер

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

        // --- ЛОГИКА ВХОДА (НОВОЕ) ---

        // Вызывается юнитом, когда он подошел к двери
        public void RequestEnter(Unit unit)
        {
            // Отправляем запрос на сервер
            EnterTowerServerRpc(unit.GetComponent<NetworkObject>());
        }

        [ServerRpc(RequireOwnership = false)]
        private void EnterTowerServerRpc(NetworkObjectReference unitRef)
        {
            if (unitRef.TryGet(out NetworkObject unitNetObj))
            {
                Unit unit = unitNetObj.GetComponent<Unit>();
                if (unit != null && CanEnter())
                {
                    AddUnitToGarrison(unit);
                }
            }
        }

        private void AddUnitToGarrison(Unit unit)
        {
            _garrison.Add(unit);

            // Телепортируем юнита в башню и выключаем его
            unit.transform.position = transform.position;

            // Отключаем визуально и логически для всех
            ToggleUnitActiveStateClientRpc(unit.GetComponent<NetworkObject>(), false);
        }

        // --- ЛОГИКА ВЫХОДА ---

        public void EjectAll()
        {
            EjectAllServerRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        private void EjectAllServerRpc()
        {
            foreach (var unit in _garrison)
            {
                if (entrancePoint != null) unit.transform.position = entrancePoint.position;
                else unit.transform.position = transform.position + Vector3.down;

                // Включаем обратно
                ToggleUnitActiveStateClientRpc(unit.GetComponent<NetworkObject>(), true);

                if (unit.TryGetComponent<UnitAI>(out var ai))
                    ai.SetState(UnitState.Idling);
            }
            _garrison.Clear();
        }

        [ClientRpc]
        private void ToggleUnitActiveStateClientRpc(NetworkObjectReference unitRef, bool isActive)
        {
            if (unitRef.TryGet(out NetworkObject unitNetObj))
            {
                unitNetObj.gameObject.SetActive(isActive);
            }
        }

        // ... (Методы TryShoot и VolleyFire оставляем как были, только убедись, что они работают на IsServer) ...
        // (Скопируй их из старой версии, если нужно, или я могу дать полные)
        private void TryShoot()
        {
            // Ищем врага (Physics2D.OverlapCircleAll)
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range);
            Health nearestEnemy = null;
            float minDst = float.MaxValue;

            foreach (var hit in hits)
            {
                if (hit.CompareTag("Enemy"))
                {
                    // Проверка: не стрелять в своих (нужна система команд/ID, пока просто тег)
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
            for (int i = 0; i < _garrison.Count; i++)
            {
                if (target == null) break;
                if (projectilePrefab != null && shootPoint != null)
                {
                    // Спавним стрелу (Сетевую!)
                    // Важно: у стрелы должен быть NetworkObject и она должна быть в списке префабов
                    GameObject arrow = Instantiate(projectilePrefab, shootPoint.position, Quaternion.identity);
                    arrow.GetComponent<NetworkObject>().Spawn();
                    arrow.GetComponent<Projectile>().Initialize(target.transform.position, damagePerUnit);
                }
                yield return new WaitForSeconds(0.2f);
            }
        }
    }
}