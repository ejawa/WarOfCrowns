using UnityEngine;
using UnityEngine.InputSystem;
using WarOfCrowns.Buildings;
using System.Collections.Generic;
using WarOfCrowns.Core; // <-- Добавили using для Kingdom

namespace WarOfCrowns.Core
{
    public class BuildManager : MonoBehaviour
    {
        [Header("Настройки")]
        [SerializeField] private GameObject buildMenuPanel;
        [Tooltip("Список ВСЕХ префабов фундаментов, доступных для постройки")]
        public List<GameObject> buildableFoundations;

        private GameObject _ghostInstance;
        private GameObject _foundationToBuild;
        private bool _isBuildingMode;

        private void Update()
        {
            if (_isBuildingMode)
            {
                Vector3 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                mousePos.z = 0;
                if (_ghostInstance != null)
                {
                    _ghostInstance.transform.position = mousePos;
                }

                if (Mouse.current.leftButton.wasPressedThisFrame && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    PlaceFoundation();
                }

                if (Mouse.current.rightButton.wasPressedThisFrame)
                {
                    ExitBuildMode();
                }
            }
        }

        public void ToggleBuildMenu()
        {
            if (buildMenuPanel != null)
            {
                buildMenuPanel.SetActive(!buildMenuPanel.activeSelf);
            }
        }

        public void EnterBuildMode(GameObject foundationPrefab)
        {
            if (_isBuildingMode)
            {
                ExitBuildMode();
            }

            _foundationToBuild = foundationPrefab;
            if (_foundationToBuild == null) return;

            _isBuildingMode = true;

            // Создаем призрака
            _ghostInstance = Instantiate(_foundationToBuild);

            // --- ИСПРАВЛЕНИЕ: НАСТРОЙКА ВНЕШНЕГО ВИДА ПРИЗРАКА ---

            // 1. Красим основной спрайт (сам фундамент) в зеленый
            if (_ghostInstance.TryGetComponent<SpriteRenderer>(out var mainRenderer))
            {
                mainRenderer.color = new Color(0, 1, 0, 0.5f);
            }

            // 2. ВЫКЛЮЧАЕМ все дочерние спрайты (ту самую иконку/белый квадрат)
            // Мы ищем все рендереры внутри призрака
            SpriteRenderer[] allRenderers = _ghostInstance.GetComponentsInChildren<SpriteRenderer>();
            foreach (var r in allRenderers)
            {
                // Если это НЕ основной объект (не сам фундамент), а дочерний (иконка)
                if (r.gameObject != _ghostInstance)
                {
                    r.gameObject.SetActive(false); // Скрываем его
                }
            }
            // -----------------------------------------------------

            // Удаляем лишние компоненты, чтобы призрак не работал как здание
            if (_ghostInstance.GetComponent<Collider2D>() != null) Destroy(_ghostInstance.GetComponent<Collider2D>());
            if (_ghostInstance.GetComponent<ConstructionSite>() != null) Destroy(_ghostInstance.GetComponent<ConstructionSite>());

            if (buildMenuPanel != null) buildMenuPanel.SetActive(false);
        }

        private void PlaceFoundation()
        {
            if (_foundationToBuild == null)
            {
                ExitBuildMode();
                return;
            }

            // --- ИЗМЕНЕНИЕ: УБРАЛИ ПРОВЕРКУ РЕСУРСОВ ---
            // Мы просто ставим фундамент. Проверять ресурсы будет строитель.

            GameObject foundationInstance = Instantiate(_foundationToBuild, _ghostInstance.transform.position, Quaternion.identity);

            // Передаем гражданство на стройплощадку
            if (foundationInstance.TryGetComponent<ConstructionSite>(out var site))
            {
                site.OwningKingdom = Kingdom.PlayerKingdom;
            }

            ExitBuildMode();
        }

        private void ExitBuildMode()
        {
            _isBuildingMode = false;
            _foundationToBuild = null;
            if (_ghostInstance != null)
            {
                Destroy(_ghostInstance);
            }
        }
    }
}