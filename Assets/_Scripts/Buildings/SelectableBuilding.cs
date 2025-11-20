using UnityEngine;
using UnityEngine.UI;
using WarOfCrowns.UI; // Важно для MainUIController

namespace WarOfCrowns.Buildings
{
    public class SelectableBuilding : MonoBehaviour
    {
        [Header("Настройки UI")]
        [Tooltip("Если это СКЛАД, поставь эту галочку. Он откроет глобальное меню.")]
        [SerializeField] private bool opensGlobalWarehouse = false;

        [Tooltip("Если это МЭРИЯ, перетащи сюда префаб её маленького меню.")]
        [SerializeField] private GameObject selectionUIPrefab;

        private GameObject _uiInstance;
        private MainUIController _mainUI;

        private void Start()
        {
            // Находим главный контроллер заранее
            _mainUI = FindObjectOfType<MainUIController>();
        }

        public void Select()
        {
            // 1. Склад
            if (opensGlobalWarehouse)
            {
                if (_mainUI != null) _mainUI.ToggleWarehousePanel();
                return;
            }

            // 2. Мэрия (или другие здания с личным UI)
            if (selectionUIPrefab != null)
            {
                if (_uiInstance == null)
                {
                    Canvas mainCanvas = FindObjectOfType<Canvas>();
                    if (mainCanvas != null)
                    {
                        _uiInstance = Instantiate(selectionUIPrefab, mainCanvas.transform);

                        // --- ВОТ НОВАЯ ЛОГИКА ---
                        // Ищем наш новый скрипт-посредник на созданном UI
                        var uiProxy = _uiInstance.GetComponent<TownHallUI>();

                        // Ищем скрипт Мэрии на себе
                        var myTownHall = GetComponent<TownHall>();

                        // Соединяем их
                        if (uiProxy != null && myTownHall != null)
                        {
                            uiProxy.Initialize(myTownHall);
                        }
                        // ------------------------
                    }
                }

                if (_uiInstance != null) _uiInstance.SetActive(true);
            }
        }

        public void Deselect()
        {
            // Если это Склад - закрываем глобальную панель
            if (opensGlobalWarehouse)
            {
                // При деселекте склада мы можем принудительно закрыть панель, 
                // но лучше проверить, открыта ли она
                // Для простоты пока оставим так: деселект склада не закрывает окно автоматически, 
                // игрок закроет его крестиком. Или можно вызвать Toggle.
                // _mainUI.ToggleWarehousePanel(); // Раскомментируй, если хочешь авто-закрытие
            }
            // Если это Мэрия - скрываем её личный UI
            else if (_uiInstance != null)
            {
                _uiInstance.SetActive(false);
            }
        }
    }
}