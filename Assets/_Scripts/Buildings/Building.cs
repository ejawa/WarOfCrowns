using System.Collections.Generic;
using UnityEngine;
using WarOfCrowns.Core;
using WarOfCrowns.Data;

namespace WarOfCrowns.Buildings
{
    [System.Serializable]
    public class BuildingCost
    {
        public ResourceType resourceType;
        public int amount;
    }

    public class Building : MonoBehaviour
    {
        [Header("Информация")]
        public string buildingName;
        public Sprite buildingIcon;
        [TextArea(3, 5)] public string description;

        [Header("Стоимость")]
        public List<BuildingCost> costs;

        [Header("Экономика")]
        public int populationBonus = 0;

        [HideInInspector] public Kingdom OwningKingdom;

        // --- НОВОЕ: УНИКАЛЬНЫЙ ID ---
        public string uniqueID;

        private void Awake()
        {
            // Если ID нет (новое здание), генерируем его
            if (string.IsNullOrEmpty(uniqueID))
            {
                uniqueID = System.Guid.NewGuid().ToString();
            }
        }

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

        public BuildingSaveData GetSaveData()
        {
            BuildingSaveData data = new BuildingSaveData();
            data.uniqueID = this.uniqueID;
            data.prefabName = gameObject.name.Replace("(Clone)", "").Trim();
            data.posX = transform.position.x;
            data.posY = transform.position.y;
            data.posZ = transform.position.z;

            // Сохраняем прогресс стройки
            if (TryGetComponent<ConstructionSite>(out var site))
            {
                data.isConstructionSite = true;
                data.constructionProgress = site.GetProgress();
            }
            // Сохраняем прогресс работы (если это не стройка)
            else if (TryGetComponent<JobBuilding>(out var job))
            {
                data.isConstructionSite = false;
                data.productionProgress = job.GetProgress();
            }
            else if (TryGetComponent<Smithy>(out var smithy))
            {
                data.isConstructionSite = false;
                data.activeRecipeIndex = smithy.GetCurrentRecipeIndex();
                data.craftingTimer = smithy.GetCurrentTimer();
            }

            return data;
        }

        public void LoadFromData(BuildingSaveData data)
        {
            this.uniqueID = data.uniqueID;

            // Восстанавливаем прогресс
            if (data.isConstructionSite)
            {
                if (TryGetComponent<ConstructionSite>(out var site))
                    site.SetProgress(data.constructionProgress);
            }
            else
            {
                if (TryGetComponent<JobBuilding>(out var job))
                    job.SetProgress(data.productionProgress);
            }
            if (TryGetComponent<Smithy>(out var smithy))
            {
                smithy.LoadState(data.activeRecipeIndex, data.craftingTimer);
            }
        }

        
        
    }
}