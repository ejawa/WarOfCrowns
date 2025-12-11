using System.Collections;
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

        // Ссылка на королевство больше не нужна для клиента
        [HideInInspector] public Kingdom OwningKingdom;

        private void Start()
        {
            // На сервере ссылка все еще полезна
            if (IsServer)
            {
                var b = GetComponent<Building>();
                if (b != null) OwningKingdom = b.OwningKingdom;
            }
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                var b = GetComponent<Building>();
                if (b != null) OwningKingdom = b.OwningKingdom;
            }
        }

        // --- ПОЛНОСТЬЮ ПЕРЕПИСАННЫЙ МЕТОД ---
        public void TryProducePeasant()
        {
            if (Kingdom.PlayerKingdom != null)
            {
                // Вызываем RPC на нашем объекте Kingdom, передавая ID этой ратуши
                // Добавляем ServerRpcParams, хотя в этом случае они не обязательны, но это хорошая практика
                Kingdom.PlayerKingdom.RequestProducePeasantServerRpc(NetworkObjectId);
            }
            else
            {
                Debug.LogError("Не найдено PlayerKingdom! Невозможно отправить запрос на производство.");
            }
        }


        public void StartProductionRoutine(ulong ownerClientId)
        {
            // Запускаем корутину производства
            StartCoroutine(ProductionRoutine(ownerClientId));
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
                    netObj.SpawnWithOwnership(ownerClientId);
                }

                var unit = peasantInstance.GetComponent<Unit>();
                if (unit != null)
                {
                    unit.ownerKingdomID.Value = GetComponent<Building>().ownerKingdomID.Value;
                }
            }
        }
    }
}