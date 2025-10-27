using UnityEngine;

namespace WarOfCrowns.Core.Items
{
    [System.Serializable]
    public class ItemDefinition
    {
        public string itemName;
        public Sprite itemIcon;
    }

    [System.Serializable]
    public class FoodDefinition : ItemDefinition
    {
        public int satietyValue;
    }
}