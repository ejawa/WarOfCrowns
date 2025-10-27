using UnityEngine;
using UnityEngine.InputSystem;
using WarOfCrowns.Buildings; // Убедись, что этот using есть

namespace WarOfCrowns.Core
{
    public class BuildManager : MonoBehaviour
    {
        private GameObject _ghostInstance;
        private GameObject _foundationToBuild;
        private bool _isBuildingMode;

        private void Update()
        {
            if (_isBuildingMode)
            {
                // Двигаем призрака за мышкой
                Vector3 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                mousePos.z = 0;
                _ghostInstance.transform.position = mousePos;

                // Размещаем по левому клику
                if (Mouse.current.leftButton.wasPressedThisFrame && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    PlaceFoundation();
                }

                // Отменяем по правому клику
                if (Mouse.current.rightButton.wasPressedThisFrame)
                {
                    ExitBuildMode();
                }
            }
        }

        // Эта публичная функция вызывается кнопками UI
        public void EnterBuildMode(GameObject foundationPrefab)
        {
            if (_isBuildingMode)
            {
                ExitBuildMode(); // Если уже строим, отменяем старое
            }

            _foundationToBuild = foundationPrefab;
            _isBuildingMode = true;

            // Создаем призрака из префаба фундамента
            _ghostInstance = Instantiate(_foundationToBuild);
            _ghostInstance.GetComponent<SpriteRenderer>().color = new Color(0, 1, 0, 0.5f); // Делаем зеленым и прозрачным

            // Отключаем на призраке все, что может помешать (коллайдер, скрипты)
            if (_ghostInstance.GetComponent<Collider2D>() != null) Destroy(_ghostInstance.GetComponent<Collider2D>());
            if (_ghostInstance.GetComponent<ConstructionSite>() != null) Destroy(_ghostInstance.GetComponent<ConstructionSite>());
        }

        // --- ВОТ НЕДОСТАЮЩИЕ ФУНКЦИИ ---

        // Эта функция размещает настоящий фундамент
        private void PlaceFoundation()
        {
            if (_foundationToBuild != null)
            {
                Instantiate(_foundationToBuild, _ghostInstance.transform.position, Quaternion.identity);
            }
            ExitBuildMode();
        }

        // Эта функция выходит из режима строительства
        private void ExitBuildMode()
        {
            _isBuildingMode = false;
            if (_ghostInstance != null)
            {
                Destroy(_ghostInstance);
            }
        }
    }
}