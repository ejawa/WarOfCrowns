using UnityEngine;
using WarOfCrowns.Core; // <-- �������� using

namespace WarOfCrowns.Units
{
    public class Unit : MonoBehaviour
    {
        [SerializeField] private GameObject selectionIndicator;

        // --- ���������� ����� ---
        private void Start()
        {
            if (PopulationManager.Instance != null)
            {
                PopulationManager.Instance.AddUnit();
            }
        }

        private void OnDestroy()
        {
            if (PopulationManager.Instance != null)
            {
                PopulationManager.Instance.RemoveUnit();
            }
        }
        // --- ����� ���������� ---

        public void Select()
        {
            selectionIndicator.SetActive(true);
        }

        public void Deselect()
        {
            selectionIndicator.SetActive(false);
        }
    }
}