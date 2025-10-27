using UnityEngine;
using UnityEngine.InputSystem;
using WarOfCrowns.Buildings; // �������, ��� ���� using ����

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
                // ������� �������� �� ������
                Vector3 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                mousePos.z = 0;
                _ghostInstance.transform.position = mousePos;

                // ��������� �� ������ �����
                if (Mouse.current.leftButton.wasPressedThisFrame && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    PlaceFoundation();
                }

                // �������� �� ������� �����
                if (Mouse.current.rightButton.wasPressedThisFrame)
                {
                    ExitBuildMode();
                }
            }
        }

        // ��� ��������� ������� ���������� �������� UI
        public void EnterBuildMode(GameObject foundationPrefab)
        {
            if (_isBuildingMode)
            {
                ExitBuildMode(); // ���� ��� ������, �������� ������
            }

            _foundationToBuild = foundationPrefab;
            _isBuildingMode = true;

            // ������� �������� �� ������� ����������
            _ghostInstance = Instantiate(_foundationToBuild);
            _ghostInstance.GetComponent<SpriteRenderer>().color = new Color(0, 1, 0, 0.5f); // ������ ������� � ����������

            // ��������� �� �������� ���, ��� ����� �������� (���������, �������)
            if (_ghostInstance.GetComponent<Collider2D>() != null) Destroy(_ghostInstance.GetComponent<Collider2D>());
            if (_ghostInstance.GetComponent<ConstructionSite>() != null) Destroy(_ghostInstance.GetComponent<ConstructionSite>());
        }

        // --- ��� ����������� ������� ---

        // ��� ������� ��������� ��������� ���������
        private void PlaceFoundation()
        {
            if (_foundationToBuild != null)
            {
                Instantiate(_foundationToBuild, _ghostInstance.transform.position, Quaternion.identity);
            }
            ExitBuildMode();
        }

        // ��� ������� ������� �� ������ �������������
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