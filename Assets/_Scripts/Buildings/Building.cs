using System.Collections.Generic;
using UnityEngine;
using WarOfCrowns.Core;
using WarOfCrowns.Data;

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
        public BuildingSaveData GetSaveData()
        {
            BuildingSaveData data = new BuildingSaveData();

            // Важно: Имя префаба должно совпадать с именем файла в папке Resources (или в списке SaveManager)
            // Мы будем использовать "очищенное" имя объекта (без "(Clone)")
            data.prefabName = gameObject.name.Replace("(Clone)", "").Trim();

            data.posX = transform.position.x;
            data.posY = transform.position.y;
            data.posZ = transform.position.z;

            // Проверяем, стройка это или нет
            ConstructionSite site = GetComponent<ConstructionSite>();
            if (site != null)
            {
                data.isConstructionSite = true;
                // data.constructionProgress = site.CurrentProgress; // Нужно добавить свойство в ConstructionSite
            }
            else
            {
                data.isConstructionSite = false;
            }

            return data;
        }

        private void OnDestroy()
        {
            if (GetComponent<ConstructionSite>() == null && populationBonus > 0)
                if (PopulationManager.Instance != null)
                    PopulationManager.Instance.AddPopulationCap(-populationBonus);
        }
    }
}