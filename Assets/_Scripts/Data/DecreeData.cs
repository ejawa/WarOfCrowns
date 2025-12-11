using UnityEngine;
using System.Collections.Generic;
using WarOfCrowns.Core; // ƒл€ ResourceType

namespace WarOfCrowns.Data
{
    [System.Serializable]
    public struct DecreeCost
    {
        public ResourceType resourceType;
        public int amount;
    }

    [CreateAssetMenu(fileName = "NewDecree", menuName = "WarOfCrowns/Decree Data")]
    public class DecreeData : ScriptableObject
    {
        public string id; // ”никальный ID (например, "harvest_fest")
        public string title;
        [TextArea] public string description;
        public Sprite icon;

        [Header("Ёкономика")]
        public List<DecreeCost> costs;
        public float cooldown = 600f; // 10 минут

        [Header("Ёффект")]
        public float legitimacyBoost = 10f;
    }
}