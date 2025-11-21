using UnityEngine;

namespace WarOfCrowns.World
{
    public class RespawnController : MonoBehaviour
    {
        private string _fullPrefabName;
        private float _timeRemaining;
        private bool _isInitialized = false;

        public void StartRespawning(string fullPrefabName, float time)
        {
            _fullPrefabName = fullPrefabName;
            _timeRemaining = time;
            _isInitialized = true;
        }

        private void Update()
        {
            if (!_isInitialized) return;

            _timeRemaining -= Time.deltaTime;

            if (_timeRemaining <= 0)
            {
                Respawn();
            }
        }

        private void Respawn()
        {
            GameObject prefabToRespawn = Resources.Load<GameObject>(_fullPrefabName);

            if (prefabToRespawn != null)
            {
                Instantiate(prefabToRespawn, transform.position, transform.rotation);
            }
            else
            {
                Debug.LogError($"RespawnController: Не могу найти префаб '{_fullPrefabName}' в Resources!");
            }

            Destroy(gameObject);
        }

        // --- МЕТОДЫ ДЛЯ СОХРАНЕНИЯ ---
        public WarOfCrowns.Data.RespawnSaveData GetSaveData()
        {
            var data = new WarOfCrowns.Data.RespawnSaveData();
            // Получаем имя текущего объекта (пустого куста), убирая (Clone)
            data.emptyPrefabName = gameObject.name.Replace("(Clone)", "").Trim();
            data.fullPrefabName = _fullPrefabName;
            data.timeRemaining = _timeRemaining;
            data.posX = transform.position.x;
            data.posY = transform.position.y;
            data.posZ = transform.position.z;
            return data;
        }

        // Метод для загрузки
        public void LoadFromData(WarOfCrowns.Data.RespawnSaveData data)
        {
            _fullPrefabName = data.fullPrefabName;
            _timeRemaining = data.timeRemaining;
            _isInitialized = true;
        }
    }
}