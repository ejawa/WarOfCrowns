using UnityEngine;
using WarOfCrowns.Core; // <-- Äîáàâèëè using

namespace WarOfCrowns.Units
{
    public class Unit : MonoBehaviour
    {
        [SerializeField] private GameObject selectionIndicator;

        // --- ÈÍÒÅÃÐÀÖÈß ÇÄÅÑÜ ---
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
        // --- ÊÎÍÅÖ ÈÍÒÅÃÐÀÖÈÈ ---

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