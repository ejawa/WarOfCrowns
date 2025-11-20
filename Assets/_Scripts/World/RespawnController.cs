using System.Collections;
using UnityEngine;

namespace WarOfCrowns.World
{
    public class RespawnController : MonoBehaviour
    {
        public void StartRespawning(string prefabName, float time)
        {
            StartCoroutine(RespawnRoutine(prefabName, time));
        }

        private IEnumerator RespawnRoutine(string prefabName, float time)
        {
            // Ждем указанное время (60 секунд)
            yield return new WaitForSeconds(time);

            // Загружаем префаб полного куста из папки Resources
            GameObject prefabToRespawn = Resources.Load<GameObject>(prefabName);

            if (prefabToRespawn != null)
            {
                // Создаем полный куст на том же месте
                Instantiate(prefabToRespawn, transform.position, transform.rotation);
            }
            else
            {
                Debug.LogError($"RespawnController: Не могу найти префаб с именем '{prefabName}' в папке Assets/Resources!");
            }

            // Уничтожаем пустой куст (себя)
            Destroy(gameObject);
        }
    }
}