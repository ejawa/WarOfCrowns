using UnityEngine;
using WarOfCrowns.Core; // ��� PopulationManager

namespace WarOfCrowns.Buildings
{
    public class BuildingData : MonoBehaviour
    {
        [Tooltip("������� ���� ��� ����� ���� ��� ������.")]
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