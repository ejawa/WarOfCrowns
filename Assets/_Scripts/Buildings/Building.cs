using System.Collections.Generic;
using UnityEngine;
using WarOfCrowns.Core;

namespace WarOfCrowns.Buildings
{
    // Класс для стоимости. Теперь он использует enum ResourceType.
    [System.Serializable]
    public class BuildingCost
    {
        public ResourceType resourceType;
        public int amount;
    }

    // Универсальный компонент для всех зданий
    public class Building : MonoBehaviour
    {
        [Header("Информация")]
        public string buildingName;
        public Sprite buildingIcon;

        [Header("Стоимость Постройки (настраивается на ФУНДАМЕНТЕ)")]
        public List<BuildingCost> costs;

        [Header("Экономика (настраивается на ФИНАЛЬНОМ здании)")]
        public int populationBonus = 0;

        [Header("Принадлежность")]
        [HideInInspector]
        public Kingdom OwningKingdom;

        private void Start()
        {
            if (GetComponent<ConstructionSite>() == null && populationBonus > 0)
                if (PopulationManager.Instance != null)
                    PopulationManager.Instance.AddPopulationCap(populationBonus);
        }

        private void OnDestroy()
        {
            if (GetComponent<ConstructionSite>() == null && populationBonus > 0)
                if (PopulationManager.Instance != null)
                    PopulationManager.Instance.AddPopulationCap(-populationBonus);
        }
    }
}