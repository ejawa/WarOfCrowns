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
            // ВАЖНО: Префаб должен лежать в папке с названием "Resources" (Assets/Resources/...)
            GameObject prefabToRespawn = Resources.Load<GameObject>(_fullPrefabName);

            if (prefabToRespawn != null)
            {
                // 1. Создаем объект
                GameObject newObj = Instantiate(prefabToRespawn, transform.position, transform.rotation);

                // 2. ОБЯЗАТЕЛЬНО спавним его в сеть!
                var netObj = newObj.GetComponent<Unity.Netcode.NetworkObject>();
                if (netObj != null)
                {
                    netObj.Spawn();
                }

                // Восстанавливаем ID для сохранения (если нужно), но для новых объектов лучше генерить новый
                // Если у ResourceNode есть уникальный ID для сейвов, он сгенерируется в Awake() нового объекта.
            }
            else
            {
                Debug.LogError($"RespawnController: Не могу найти префаб '{_fullPrefabName}' в папке Resources!");
            }

            // Удаляем "пустой" объект (контроллер респавна)
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