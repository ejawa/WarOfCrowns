using UnityEngine;
using WarOfCrowns.Core;

namespace WarOfCrowns.Buildings
{
    public class House : MonoBehaviour
    {
        private void Start()
        {
            if (PopulationManager.Instance != null)
                PopulationManager.Instance.AddPopulationCap(5);
            Debug.Log("House built, adding population cap!");
        }

        private void OnDestroy()
        {
            if (PopulationManager.Instance != null)
                PopulationManager.Instance.AddPopulationCap(-5);
        }
    }
}