using System.Collections.Generic;
using UnityEngine;



namespace WarOfCrowns.Core.Items
{
    [System.Serializable]
    public class CraftingIngredient
    {
        public ItemDefinition item;
        public int amount;
    }

    [System.Serializable]
    public class Recipe
    {
        public ItemDefinition resultingItem;
        public int resultingAmount;
        public List<CraftingIngredient> ingredients;
    }
}