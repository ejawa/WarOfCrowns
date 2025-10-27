using UnityEngine;
using UnityEngine.UI;
using WarOfCrowns.Buildings;
using WarOfCrowns.UI; // <-- �����

namespace WarOfCrowns.Buildings
{
    public class SelectableBuilding : MonoBehaviour
    {
        [Header("UI Settings")]
        [Tooltip("The PREFAB of the UI Panel to show on selection.")]
        [SerializeField] private GameObject selectionUIPrefab;

        private GameObject _uiInstance;

        public void Select()
        {
            if (_uiInstance == null && selectionUIPrefab != null)
            {
                // ������� UI �� ������� � ������ ��� �������� � �������� Canvas
                _uiInstance = Instantiate(selectionUIPrefab, FindObjectOfType<Canvas>().transform);

                // --- �������������� ��������� ---
                // ����������� ������ ������������, ���� ��� �����
                if (_uiInstance.GetComponentInChildren<Button>() != null && TryGetComponent<TownHall>(out var townHall))
                {
                    _uiInstance.GetComponentInChildren<Button>().onClick.AddListener(townHall.TryProducePeasant);
                }

                // ����������� ����������� ���������, ���� ��� �����
                if (_uiInstance.TryGetComponent<WarehouseUI>(out var warehouseUI) && TryGetComponent<Warehouse>(out var warehouse))
                {
                    warehouseUI.Show(warehouse);
                }
            }

            if (_uiInstance != null)
            {
                _uiInstance.SetActive(true);
            }
        }

        public void Deselect()
        {
            if (_uiInstance != null)
            {
                _uiInstance.SetActive(false);
            }
        }
    }
}