using UnityEngine;
using WarOfCrowns.Core; // Для PopulationManager

namespace WarOfCrowns.Buildings
{
    public class BuildingData : MonoBehaviour
    {
        [Tooltip("Сколько мест для жилья дает это здание.")]
        [SerializeField] private int populationBonus = 0;

        private void Start()
        {
            if (populationBonus > 0 && PopulationManager.Instance != null)
            {
                PopulationManager.Instance.AddPopulationCap(populationBonus);
            }
        }

        private void OnDestroy()
        {
            if (populationBonus > 0 && PopulationManager.Instance != null)
            {
                PopulationManager.Instance.AddPopulationCap(-populationBonus);
            }
        }
    }
}