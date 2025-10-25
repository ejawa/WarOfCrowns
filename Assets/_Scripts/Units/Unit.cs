using UnityEngine;

namespace WarOfCrowns.Units
{
    public class Unit : MonoBehaviour
    {
        [SerializeField] private GameObject selectionIndicator;

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