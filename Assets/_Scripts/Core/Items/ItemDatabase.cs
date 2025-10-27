using System.Collections.Generic;
using UnityEngine;


namespace WarOfCrowns.Core.Items
{
    [CreateAssetMenu(fileName = "ItemDatabase", menuName = "WarOfCrowns/Item Database")]
    public class ItemDatabase : ScriptableObject
    {
        // Здесь мы будем хранить ВСЕ предметы, чтобы ссылаться на них
        public List<ItemDefinition> allMaterials;
        public List<FoodDefinition> allFoods;

        public List<Recipe> allRecipes;

        // Метод для поиска предмета по имени (очень полезно)
        public ItemDefinition GetItemByName(string name)
        {
            foreach (var item in allMaterials) { if (item.itemName == name) return item; }
            foreach (var item in allFoods) { if (item.itemName == name) return item; }
            return null;
        }
    }
}