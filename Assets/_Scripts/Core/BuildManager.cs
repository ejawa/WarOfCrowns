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

            _ghostInstance = Instantiate(_foundationToBuild);

            if (_ghostInstance.TryGetComponent<SpriteRenderer>(out var renderer))
            {
                renderer.color = new Color(0, 1, 0, 0.5f);
            }
            if (_ghostInstance.GetComponent<Collider2D>() != null) Destroy(_ghostInstance.GetComponent<Collider2D>());
            if (_ghostInstance.GetComponent<ConstructionSite>() != null) Destroy(_ghostInstance.GetComponent<ConstructionSite>());

            if (buildMenuPanel != null && buildMenuPanel.activeSelf)
                ToggleBuildMenu();
        }

        private void PlaceFoundation()
        {
            if (_foundationToBuild == null) return;

            Building buildingData = _foundationToBuild.GetComponent<Building>();

            // --- ИЗМЕНЕНИЕ ЗДЕСЬ ---
            // Мы НЕ вызываем TrySpendResources. Мы делаем "фейковую" проверку вручную.
            bool canAfford = true;
            if (buildingData != null)
            {
                foreach (var cost in buildingData.costs)
                {
                    if (Kingdom.PlayerKingdom.GetResourceAmount(cost.resourceType) < cost.amount)
                    {
                        canAfford = false;
                        break;
                    }
                }
            }

            if (canAfford)
            {
                // Ресурсы НЕ тратим, просто ставим фундамент.
                // Тратить их будет ConstructionSite.cs по мере работы.
                GameObject foundationInstance = Instantiate(_foundationToBuild, _ghostInstance.transform.position, Quaternion.identity);

                if (foundationInstance.TryGetComponent<ConstructionSite>(out var site))
                {
                    site.OwningKingdom = Kingdom.PlayerKingdom;
                }

                // Присваиваем гражданство, если есть Building компонент
                if (foundationInstance.TryGetComponent<Building>(out var building))
                {
                    building.OwningKingdom = Kingdom.PlayerKingdom;
                }
            }
            else
            {
                Debug.Log("Not enough resources to start construction!");
            }
            // --- ---

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