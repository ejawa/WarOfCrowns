using System.Collections.Generic;
using UnityEngine;
using WarOfCrowns.Core;

namespace WarOfCrowns.Buildings
{
    [System.Serializable]
    public class CraftingRecipe
    {
        public string recipeName;        // Название (например, "Iron Ingot")
        public ResourceType outputItem;  // Что получится
        public int outputAmount = 1;     // Сколько получится
        public float craftTime = 5f;     // Время изготовления

        [Header("Требуемые Ресурсы")]
        public List<BuildingCost> inputs; // Используем BuildingCost как ингредиент (Тип + Кол-во)
    }
}