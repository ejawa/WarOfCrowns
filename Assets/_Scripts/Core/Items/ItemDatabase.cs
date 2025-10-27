using System.Collections.Generic;
using UnityEngine;


namespace WarOfCrowns.Core.Items
{
    [CreateAssetMenu(fileName = "ItemDatabase", menuName = "WarOfCrowns/Item Database")]
    public class ItemDatabase : ScriptableObject
    {
        // ����� �� ����� ������� ��� ��������, ����� ��������� �� ���
        public List<ItemDefinition> allMaterials;
        public List<FoodDefinition> allFoods;

        public List<Recipe> allRecipes;

        // ����� ��� ������ �������� �� ����� (����� �������)
        public ItemDefinition GetItemByName(string name)
        {
            foreach (var item in allMaterials) { if (item.itemName == name) return item; }
            foreach (var item in allFoods) { if (item.itemName == name) return item; }
            return null;
        }
    }
}