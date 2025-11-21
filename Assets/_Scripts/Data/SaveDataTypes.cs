using System;
using System.Collections.Generic;
using WarOfCrowns.Core;

namespace WarOfCrowns.Data
{
    // --- 1. İÊÎÍÎÌÈÊÀ ---
    [Serializable] public class KingdomSaveData { public List<ResourceSaveEntry> inventory = new List<ResourceSaveEntry>(); }
    [Serializable] public class ResourceSaveEntry { public ResourceType type; public int amount; public ResourceSaveEntry(ResourceType t, int a) { type = t; amount = a; } }

    // --- 2. ŞÍÈÒÛ ---
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

        // --- ÍÎÂÛÅ ÏÎËß ---
        public string targetResourceID; // ID ğåñóğñà, êîòîğûé ìû ğóáèì
        public bool isMoving;           // Äâèãàåìñÿ ëè ìû?
        public float moveTargetX, moveTargetY, moveTargetZ; // Êóäà ìû èäåì?
    }

    // --- 3. ÇÄÀÍÈß ---
    [Serializable] public class BuildingListWrapper { public List<BuildingSaveData> buildings = new List<BuildingSaveData>(); }
    [Serializable]
    public class BuildingSaveData
    {
        public string uniqueID;
        public string prefabName;
        public float posX, posY, posZ;
        public bool isConstructionSite;
        public float constructionProgress;
        public float productionProgress;
    }

    // --- 4. ĞÅÑÓĞÑÛ ÌÈĞÀ ---
    [Serializable]
    public class WorldResourceListWrapper
    {
        public List<ResourceNodeSaveData> activeResources = new List<ResourceNodeSaveData>();
        public List<RespawnSaveData> respawningResources = new List<RespawnSaveData>();
    }

    [Serializable]
    public class ResourceNodeSaveData
    {
        public string uniqueID; // <-- ÄÎÁÀÂÈËÈ ID
        public string prefabName;
        public float posX, posY, posZ;
        public int hitsLeft;
        public float accumulated;
        public int givenOut;
    }

    [Serializable] public class RespawnSaveData { public string emptyPrefabName; public string fullPrefabName; public float timeRemaining; public float posX, posY, posZ; }
}