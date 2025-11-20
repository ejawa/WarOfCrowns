using UnityEngine;
using WarOfCrowns.Core;

namespace WarOfCrowns.World
{
    public class ResourceNode : MonoBehaviour
    {
        // --- ВОТ ЭТОГО НЕ ХВАТАЛО ---
        public enum DepletionBehaviour { Destroy, Respawn }
        // ---------------------------

        [Header("Настройки Ресурса")]
        public ResourceType resourceType;
        public int maxAmount = 250;

        [HideInInspector]
        public int currentAmount;

        [Header("Настройки Истощения")]
        public DepletionBehaviour depletionBehaviour;

        [Tooltip("Префаб ПУСТОГО куста (для ягод).")]
        [SerializeField] private GameObject depletedPrefab;

        [Tooltip("Имя файла ПОЛНОГО куста в папке Resources (например 'BerryBush_Full').")]
        [SerializeField] private string resourcePrefabName;

        [SerializeField] private float respawnTime = 60f;

        private void Awake()
        {
            currentAmount = maxAmount;
        }

        public int Gather(int requestedAmount)
        {
            int amountToGive = Mathf.Min(requestedAmount, currentAmount);
            currentAmount -= amountToGive;

            if (currentAmount <= 0)
            {
                Deplete();
            }

            return amountToGive;
        }

        private void Deplete()
        {
            switch (depletionBehaviour)
            {
                case DepletionBehaviour.Destroy:
                    // Для дерева и камня - просто уничтожаем
                    Destroy(gameObject);
                    break;

                case DepletionBehaviour.Respawn:
                    // Для ягод - создаем пустой куст и запускаем таймер
                    if (depletedPrefab != null)
                    {
                        GameObject emptyBush = Instantiate(depletedPrefab, transform.position, transform.rotation);

                        // Чтобы респаун сработал, нам нужно имя файла в папке Resources.
                        // Если ты забыл написать его в инспекторе, попробуем угадать имя объекта.
                        string nameToLoad = !string.IsNullOrEmpty(resourcePrefabName) ? resourcePrefabName : gameObject.name.Replace("(Clone)", "");

                        // Добавляем контроллер респауна на пустой куст
                        emptyBush.AddComponent<RespawnController>().StartRespawning(nameToLoad, respawnTime);
                    }
                    Destroy(gameObject); // Удаляем полный куст
                    break;
            }
        }
    }
}