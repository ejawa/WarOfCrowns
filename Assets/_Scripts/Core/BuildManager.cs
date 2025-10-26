using UnityEngine;
using UnityEngine.InputSystem;
using WarOfCrowns.Buildings; // <-- ВОТ ОНО

namespace WarOfCrowns.Core
{
    public class BuildManager : MonoBehaviour
    {
        [SerializeField] private GameObject houseFoundationPrefab;
        private GameObject _ghostInstance;
        private bool _isBuildingMode;

        private void Update()
        {
            if (_isBuildingMode)
            {
                Vector3 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                mousePos.z = 0;
                _ghostInstance.transform.position = mousePos;

                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    Instantiate(houseFoundationPrefab, _ghostInstance.transform.position, Quaternion.identity);
                    ExitBuildMode();
                }
                if (Mouse.current.rightButton.wasPressedThisFrame)
                {
                    ExitBuildMode();
                }
            }
        }

        public void EnterBuildMode()
        {
            if (_isBuildingMode) return;
            _isBuildingMode = true;
            _ghostInstance = Instantiate(houseFoundationPrefab);
            _ghostInstance.GetComponent<SpriteRenderer>().color = new Color(0, 1, 0, 0.5f);
            Destroy(_ghostInstance.GetComponent<BoxCollider2D>());

            // Теперь компилятор знает, что такое ConstructionSite
            if (_ghostInstance.GetComponent<ConstructionSite>() != null)
                Destroy(_ghostInstance.GetComponent<ConstructionSite>());
        }

        private void ExitBuildMode()
        {
            _isBuildingMode = false;
            Destroy(_ghostInstance);
        }
    }
}