using System;
using System.Collections.Generic;
using WarOfCrowns.Core; // На всякий случай

namespace WarOfCrowns.Data
{
    [Serializable]
    public class GameSaveData
    {
        public string saveName;
        public string timestamp;
        public WorldSaveData world;
        public KingdomSaveData hostKingdom;

        public List<UnitSaveData> units = new List<UnitSaveData>();
        public List<BuildingSaveData> buildings = new List<BuildingSaveData>();
        public List<ResourceNodeSaveData> resources = new List<ResourceNodeSaveData>();
    }

    [Serializable]
    public class WorldSaveData
    {
        public string seed;
    }

    [Serializable]
    public class KingdomSaveData
    {
        public List<ResourceSaveEntry> inventory = new List<ResourceSaveEntry>();
    }

    [Serializable]
    public class ResourceSaveEntry
    {
        public string type;
        public int amount;
        public ResourceSaveEntry(string t, int a) { type = t; amount = a; }
    }

    // --- ВОТ ЗДЕСЬ Я ВЕРНУЛ ВСЕ ПОЛЯ ---
    [Serializable]
    public class UnitSaveData
    {
        // Основные
        public string uniqueID;
        public string unitName;
        public string prefabName;
        public int ownerID;       // Нужно для сети (0 или 1)
        public int gender;
        public string profession;

        // Позиция
        public float posX, posY, posZ;

        // Характеристики
        public float currentHealth;
        public float currentHunger;

        // Состояние ИИ
        public int aiState;
        public bool isMoving;
        public float moveTargetX;
        public float moveTargetY;
        public float moveTargetZ;

        // Связи
        public string workplaceID;
        public string targetResourceID;

        // Экипировка (enum как int)
        public int weaponType;
        public int armorType;
        public int toolType;

        // Визуал (имена спрайтов)
        public string bodySpriteName;
        public string headSpriteName;
        public string clothesSpriteName;
    }

    [Serializable]
    public class BuildingSaveData
    {
        public string uniqueID;
        public string prefabName;
        public int ownerID; // Нужно для сети
        public float posX, posY, posZ;

        public bool isConstructionSite;
        public float constructionProgress;

        public float productionProgress; // Для рабочих зданий
        public int activeRecipeIndex;    // Для кузницы
        public float craftingTimer;      // Для кузницы
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

    [Serializable]
    public class RespawnSaveData
    {
        public string emptyPrefabName;
        public string fullPrefabName;
        public float timeRemaining;
        public float posX, posY, posZ;
    }
}