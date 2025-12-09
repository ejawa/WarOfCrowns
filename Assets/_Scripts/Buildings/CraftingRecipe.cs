using System.Collections.Generic;
using UnityEngine;
using WarOfCrowns.Core;

namespace WarOfCrowns.Buildings
{
    // Детальные категории
    public enum RecipeCategory
    {
        Sword,      // Мечи
        Bow,        // Луки
        Spear,      // Копья

        Pickaxe,    // Кирки
        Axe,        // Топоры
        Hammer,     // Молоты

        Armor,      // Броня
        Material    // Слитки и прочее
    }

    [System.Serializable]
    public class CraftingRecipe
    {
        public string recipeName;
        public ResourceType outputItem;
        public int outputAmount = 1;
        public float craftTime = 5f;

        [Header("Визуал")]
        public Sprite icon;
        public RecipeCategory category; // Выбираешь конкретный тип здесь

        [Header("Требуемые Ресурсы")]
        public List<BuildingCost> inputs;
    }
}