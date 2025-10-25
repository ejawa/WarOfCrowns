using System.Collections;
using UnityEngine;

namespace WarOfCrowns.World
{
    public class RespawnController : MonoBehaviour
    {
        // ИЗМЕНЕНИЕ ЗДЕСЬ: Теперь мы принимаем string, а не GameObject
        public void StartRespawning(string prefabName, float time)
        {
            StartCoroutine(RespawnRoutine(prefabName, time));
        }

        // И ИЗМЕНЕНИЕ ЗДЕСЬ
        private IEnumerator RespawnRoutine(string prefabName, float time)
        {
            yield return new WaitForSeconds(time);

            // Загружаем префаб из папки Resources по его имени
            GameObject prefabToRespawn = Resources.Load<GameObject>(prefabName);

            if (prefabToRespawn != null)
            {
                // Создаем копию загруженного префаба
                Instantiate(prefabToRespawn, transform.position, transform.rotation);
            }
            else
            {
                Debug.LogError($"Не удалось найти префаб с именем '{prefabName}' в папке Resources.");
            }

            // Уничтожаем этот пустой куст
            Destroy(gameObject);
        }
    }
}