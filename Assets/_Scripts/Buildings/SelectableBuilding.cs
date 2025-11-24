using UnityEngine;
using UnityEngine.UI;
using WarOfCrowns.UI; // Здесь живут MainUIController и Proxy скрипты

namespace WarOfCrowns.Buildings
{
    public class SelectableBuilding : MonoBehaviour
    {
        [Header("Настройки")]
        [Tooltip("Поставь галочку, если это СКЛАД. Он откроет общее окно склада.")]
        [SerializeField] private bool opensGlobalWarehouse;

        [Tooltip("Префаб UI для других зданий (Ферма, Казарма, Мэрия). Для Склада оставь пустым.")]
        [SerializeField] private GameObject selectionUIPrefab;

        private GameObject _uiInstance;
        private MainUIController _mainUI;

        private void Start()
        {
            // Находим главный контроллер интерфейса
            _mainUI = FindObjectOfType<MainUIController>();
        }

        public void Select()
        {
            // --- ЛОГИКА ДЛЯ СКЛАДА ---
            if (opensGlobalWarehouse)
            {
                if (_mainUI != null)
                {
                    _mainUI.ToggleWarehousePanel();
                }
                else
                {
                    Debug.LogError("SelectableBuilding: MainUIController not found on scene!");
                }
                return; // Выходим, так как склад обрабатывается отдельно
            }

            // --- ЛОГИКА ДЛЯ ОСТАЛЬНЫХ ЗДАНИЙ ---
            if (selectionUIPrefab == null) return;

            if (_uiInstance == null)
            {
                Canvas mainCanvas = FindObjectOfType<Canvas>();
                if (mainCanvas == null) return;

                _uiInstance = Instantiate(selectionUIPrefab, mainCanvas.transform);

                // --- Инициализация Прокси (Связь UI и Логики) ---

                // 1. Мэрия
                if (TryGetComponent<TownHall>(out var townHall) && _uiInstance.TryGetComponent<TownHall_UIProxy>(out var thProxy))
                {
                    thProxy.LinkToTownHall(townHall);
                }

                // 2. Рабочее здание (Ферма, Мельница...)
                if (TryGetComponent<JobBuilding>(out var jobBuilding) && _uiInstance.TryGetComponent<JobUIProxy>(out var jobProxy))
                {
                    jobProxy.Initialize(jobBuilding);
                }

                // 3. Казарма
                if (TryGetComponent<Barracks>(out var barracks) && _uiInstance.TryGetComponent<BarracksUIProxy>(out var barracksProxy))
                {
                    barracksProxy.Initialize(barracks);
                }

                // 4. Кузница (Плавильня)
                if (TryGetComponent<Smithy>(out var smithy) && _uiInstance.TryGetComponent<SmithyUIProxy>(out var smithyProxy))
                {
                    smithyProxy.Initialize(smithy);
                }

            }

            _uiInstance.SetActive(true);
        }

        public void Deselect()
        {
            // Если это склад - закрываем панель через контроллер
            if (opensGlobalWarehouse)
            {
                if (_mainUI != null)
                {
                    // Можно принудительно закрыть, если хочешь:
                    // _mainUI.CloseWarehousePanel(); 
                    // Но пока оставим как есть (Toggle), или просто ничего не делаем, пусть игрок сам закрывает.
                }
            }
            // Если это обычное здание - скрываем его личный UI
            else if (_uiInstance != null)
            {
                _uiInstance.SetActive(false);
            }
        }
    }
}