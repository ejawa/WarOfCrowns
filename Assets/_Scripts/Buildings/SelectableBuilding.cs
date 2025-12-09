using UnityEngine;
using WarOfCrowns.UI;

namespace WarOfCrowns.Buildings
{
    public class SelectableBuilding : MonoBehaviour
    {
        [Header("Настройки")]
        [Tooltip("Поставь галочку, если это СКЛАД.")]
        [SerializeField] private bool opensGlobalWarehouse;
        [Tooltip("Префаб UI для этого здания (Ферма, Казарма, Мэрия).")]
        [SerializeField] private GameObject selectionUIPrefab;

        // --- ИЗМЕНЕНИЯ ЗДЕСЬ ---
        private GameObject _uiInstance; // Ссылка на наш UI
        private MainUIController _mainUI;

        private void Start()
        {
            _mainUI = FindObjectOfType<MainUIController>();
        }

        public void Select()
        {
            // Логика Склада (без изменений)
            if (opensGlobalWarehouse)
            {
                if (_mainUI != null) _mainUI.OpenWarehousePanel(); // ЯВНО ОТКРЫВАЕМ
                return;
            }

            // --- НОВАЯ ЛОГИКА ДЛЯ ВСЕХ ОСТАЛЬНЫХ ЗДАНИЙ ---
            if (selectionUIPrefab == null) return;

            // 1. Создаем UI, если его еще нет
            if (_uiInstance == null)
            {
                Canvas mainCanvas = FindObjectOfType<Canvas>();
                if (mainCanvas == null) return;
                _uiInstance = Instantiate(selectionUIPrefab, mainCanvas.transform);
            }

            // 2. Включаем UI
            _uiInstance.SetActive(true);

            // 3. "ЗНАКОМИМ" UI со ЗДАНИЕМ (самый важный шаг)
            LinkUIToBuilding();
        }

        public void Deselect()
        {
            if (opensGlobalWarehouse)
            {
                if (_mainUI != null) _mainUI.CloseWarehousePanel(); // ЯВНО ЗАКРЫВАЕМ
                return;
            }

            // Скрываем личный UI
            if (_uiInstance != null)
            {
                _uiInstance.SetActive(false);
            }
        }

        // Новый метод для "знакомства"
        private void LinkUIToBuilding()
        {
            // А. Если это Мэрия
            if (TryGetComponent<TownHall>(out var townHall) && _uiInstance.TryGetComponent<TownHallUI>(out var townHallUI))
            {
                townHallUI.Initialize(townHall);
            }
            // Б. Если это Рабочее здание (Ферма, и т.д.)
            else if (TryGetComponent<JobBuilding>(out var jobBuilding) && _uiInstance.TryGetComponent<JobUI>(out var jobUI))
            {
                jobUI.Initialize(jobBuilding);
            }
            // В. Если это Казарма
            else if (TryGetComponent<Barracks>(out var barracks) && _uiInstance.TryGetComponent<BarracksUI>(out var barracksUI))
            {
                barracksUI.Initialize(barracks);
            }
            // Г. Если это Кузница
            else if (TryGetComponent<Smithy>(out var smithy) && _uiInstance.TryGetComponent<SmithyUI>(out var smithyUI))
            {
                smithyUI.Initialize(smithy);
            }
            // Д. --- НОВОЕ: Если это Башня ---
            else if (TryGetComponent<DefenseTower>(out var tower) && _uiInstance.TryGetComponent<DefenseTowerUIProxy>(out var towerProxy))
            {
                towerProxy.Initialize(tower);
            }
        }
    }
}