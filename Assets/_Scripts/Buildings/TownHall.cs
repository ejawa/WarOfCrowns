using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using WarOfCrowns.Core;
using WarOfCrowns.Units;

namespace WarOfCrowns.Buildings
{
    public class TownHall : NetworkBehaviour
    {
        [Header("Настройки Производства")]
        [SerializeField] private GameObject peasantPrefab;
        [SerializeField] private float productionTime = 5f;
        [SerializeField] private Transform spawnPoint;

        [Header("Стоимость Юнита")]
        [Tooltip("Сколько 'единиц еды' стоит один юнит")]
        public int peasantFoodCost = 50;

        // Ссылка на королевство, к которому принадлежит здание
        [HideInInspector] public Kingdom OwningKingdom;

        // При старте находим компонент Building, чтобы получить ссылку на Королевство
        private void Start()
        {
            var b = GetComponent<Building>();
            if (b != null) OwningKingdom = b.OwningKingdom;
        }

        public override void OnNetworkSpawn()
        {
            // Подстраховка: обновляем ссылку при сетевом спавне
            var b = GetComponent<Building>();
            if (b != null) OwningKingdom = b.OwningKingdom;
        }

        // --- ГЛАВНОЕ ИЗМЕНЕНИЕ ЗДЕСЬ ---
        public void TryProducePeasant()
        {
            // 1. Получаем актуальную ссылку на Королевство (через Building, который синхронизировал ID)
            var buildingComp = GetComponent<Building>();
            if (buildingComp == null) return;

            // Важно: Находим локальное королевство, которое соответствует ID владельца здания
            // Если я Клиент (ID 1) и это здание мое (ID 1), я найду свое локальное королевство.
            Kingdom myKingdom = null;
            foreach (var k in FindObjectsOfType<Kingdom>())
            {
                if (k.kingdomID == buildingComp.ownerKingdomID.Value)
                {
                    myKingdom = k;
                    break;
                }
            }

            if (myKingdom == null)
            {
                Debug.LogError("TownHall: Не найдено королевство владельца для списания ресурсов!");
                return;
            }

            // 2. Проверяем лимит (Локально)
            if (PopulationManager.Instance != null && PopulationManager.Instance.IsCapReached())
            {
                Debug.Log("TownHall: Лимит населения достигнут.");
                return;
            }

            // 3. Тратим ресурсы (Локально)
            if (myKingdom.TrySpendFood(peasantFoodCost))
            {
                Debug.Log($"TownHall: Оплата прошла ({peasantFoodCost} еды). Отправляю запрос серверу.");
                // 4. Если оплатили - отправляем запрос на спавн
                ProducePeasantServerRpc();
            }
            else
            {
                Debug.Log("TownHall: Недостаточно еды.");
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void ProducePeasantServerRpc(ServerRpcParams rpcParams = default)
        {
            // Сервер просто запускает производство, так как ресурсы уже списаны у клиента
            Debug.Log("TownHall [Server]: Запрос получен. Начинаю производство.");
            StartCoroutine(ProductionRoutine(rpcParams.Receive.SenderClientId));
        }

        private IEnumerator ProductionRoutine(ulong ownerClientId)
        {
            yield return new WaitForSeconds(productionTime);

            if (spawnPoint != null && peasantPrefab != null)
            {
                GameObject peasantInstance = Instantiate(peasantPrefab, spawnPoint.position, Quaternion.identity);

                var netObj = peasantInstance.GetComponent<NetworkObject>();
                if (netObj != null)
                {
                    // Спавним и отдаем права тому, кто заказал
                    netObj.SpawnWithOwnership(ownerClientId);
                }

                var unit = peasantInstance.GetComponent<Unit>();
                if (unit != null)
                {
                    // Передаем ID королевства от здания к юниту
                    unit.ownerKingdomID.Value = GetComponent<Building>().ownerKingdomID.Value;
                }
            }
        }
    }
}