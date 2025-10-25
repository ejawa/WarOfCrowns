using UnityEngine;
using UnityEngine.InputSystem;

namespace WarOfCrowns.Core
{
    public class BuildManager : MonoBehaviour
    {
        public static BuildManager Instance;

        [SerializeField] private GameObject houseFoundationPrefab;

        private GameObject _ghostInstance;
        private bool _isBuildingMode;

        private void Awake() { Instance = this; }

        private void Update()
        {
            if (_isBuildingMode)
            {
                // Move ghost
                Vector3 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                mousePos.z = 0;
                _ghostInstance.transform.position = mousePos;

                // Place with left click
                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    Instantiate(houseFoundationPrefab, _ghostInstance.transform.position, Quaternion.identity);
                    ExitBuildMode();
                }

                // Cancel with right click
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
            _ghostInstance = Instantiate(houseFoundationPrefab); // We can use the foundation as a ghost for now
            _ghostInstance.GetComponent<SpriteRenderer>().color = new Color(0, 1, 0, 0.5f); // Make it green and transparent
            Destroy(_ghostInstance.GetComponent<BoxCollider2D>());
        }

        private void ExitBuildMode()
        {
            _isBuildingMode = false;
            Destroy(_ghostInstance);
        }
    }
}