using System;
using System.Collections.Generic;
using WarOfCrowns.Core;

namespace WarOfCrowns.Data
{
    // --- 1. ЭКОНОМИКА ---
    [Serializable] public class KingdomSaveData { public List<ResourceSaveEntry> inventory = new List<ResourceSaveEntry>(); }
    [Serializable] public class ResourceSaveEntry { public ResourceType type; public int amount; public ResourceSaveEntry(ResourceType t, int a) { type = t; amount = a; } }

    // --- 2. ЮНИТЫ ---
    [Serializable] public class UnitListWrapper { public List<UnitSaveData> units = new List<UnitSaveData>(); }

    [Serializable]
    public class UnitSaveData
    {
        public string uniqueID;
        public string unitName;
        public string prefabName;
        public int gender;

        public float posX, posY, posZ;
        public float currentHealth;
        public float currentHunger;

        public string profession;
        public int aiState;
        public string workplaceID;

        // Действия
        public string targetResourceID;
        public bool isMoving;
        public float moveTargetX, moveTargetY, moveTargetZ;

        // --- НОВЫЕ ПОЛЯ: ВНЕШНОСТЬ (Для сохранения скинов) ---
        public string bodySpriteName;
        public string headSpriteName;
        public string clothesSpriteName;

        // --- НОВЫЕ ПОЛЯ: ЭКИПИРОВКА (Сохраняем enum как int) ---
        public int weaponType;
        public int armorType;
        public int toolType;
    }

    // --- 3. ЗДАНИЯ ---
    [Serializable] public class BuildingListWrapper { public List<BuildingSaveData> buildings = new List<BuildingSaveData>(); }
    [Serializable]
    public class BuildingSaveData
    {
        public string uniqueID;
        public string prefabName;
        public float posX, posY, posZ;
        public bool isConstructionSite;
        public float constructionProgress;
        public float productionProgress; // Для Фермы/Мельницы

        // --- НОВЫЕ ПОЛЯ ДЛЯ КУЗНИЦЫ ---
        public int activeRecipeIndex = -1; // Какой рецепт делается (-1 = ничего)
        public float craftingTimer = 0f;   // Сколько времени осталось
    }

    // --- 4. РЕСУРСЫ МИРА ---
    [Serializable]
    public class WorldResourceListWrapper
    {
        public List<ResourceNodeSaveData> activeResources = new List<ResourceNodeSaveData>();
        public List<RespawnSaveData> respawningResources = new List<RespawnSaveData>();
    }

    [Serializable]
    public class ResourceNodeSaveData
    {
        public string uniqueID;
        public string prefabName;
        public float posX, posY, posZ;
        public int hitsLeft;
        public float accumulated;
        public int givenOut;
    }

    [Serializable] public class RespawnSaveData { public string emptyPrefabName; public string fullPrefabName; public float timeRemaining; public float posX, posY, posZ; }
}