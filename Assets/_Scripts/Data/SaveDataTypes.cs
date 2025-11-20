using System;
using System.Collections.Generic;
using WarOfCrowns.Core; // Чтобы видеть ResourceType

namespace WarOfCrowns.Data
{
    // --- 1. ДАННЫЕ КОРОЛЕВСТВА (resources.json) ---
    [Serializable]
    public class KingdomSaveData
    {
        // Мы не можем сохранить Dictionary в JSON напрямую, поэтому используем список
        public List<ResourceSaveEntry> inventory = new List<ResourceSaveEntry>();

        // Сюда позже добавим:
        // public float legitimacy;
        // public int populationCap;
    }

    [Serializable]
    public class ResourceSaveEntry
    {
        public ResourceType type;
        public int amount;

        // Конструктор для удобства
        public ResourceSaveEntry(ResourceType t, int a)
        {
            type = t;
            amount = a;
        }
    }

    // --- 2. ДАННЫЕ ЮНИТОВ (units.json) ---
    [Serializable]
    public class UnitListWrapper
    {
        public List<UnitSaveData> units = new List<UnitSaveData>();
    }

    [Serializable]
    public class UnitSaveData
    {
        public string unitName;      // Имя (Боб, Джек)
        public string prefabName;    // Имя префаба для загрузки (Peasant_Prototype)

        // Позиция (Vector3 не всегда хорошо сериализуется, надежнее хранить отдельно)
        public float posX;
        public float posY;
        public float posZ;

        public float currentHealth;
        public float currentHunger;

        // public string job; // На будущее
    }

    // --- 3. ДАННЫЕ ЗДАНИЙ (buildings.json) ---
    [Serializable]
    public class BuildingListWrapper
    {
        public List<BuildingSaveData> buildings = new List<BuildingSaveData>();
    }

    [Serializable]
    public class BuildingSaveData
    {
        public string buildingID;    // Уникальный ID (на будущее)
        public string prefabName;    // Имя префаба (House_Building, Warehouse_Building)

        public float posX;
        public float posY;
        public float posZ;

        public float currentHealth;
        public bool isConstructionSite; // Это фундамент или готовое здание?
        public float constructionProgress; // Если фундамент
    }
}