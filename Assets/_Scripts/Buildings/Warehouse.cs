using System; // <-- Для Action
using System.Collections.Generic;
using UnityEngine;
using WarOfCrowns.Core.Items;

namespace WarOfCrowns.Buildings
{
    public class Warehouse : MonoBehaviour
    {
        // Событие теперь НЕ статическое. Оно принадлежит этому конкретному складу.
        public event Action<ItemDefinition, int> OnInventoryChanged;

        // Здесь будет храниться инвентарь: <ItemDefinition, количество>
        private Dictionary<ItemDefinition, int> _inventory = new Dictionary<ItemDefinition, int>();

        // Метод, чтобы добавить/убрать предмет
        public void AddItem(ItemDefinition item, int amount)
        {
            if (!_inventory.ContainsKey(item)) _inventory[item] = 0;
            _inventory[item] += amount;

            // "Кричим" о том, что инвентарь изменился
            OnInventoryChanged?.Invoke(item, _inventory[item]);

            Debug.Log($"Changed {item.itemName} by {amount}. New total: {_inventory[item]}");
        }

        // Метод, чтобы UI мог получить полное состояние инвентаря при открытии
        public Dictionary<ItemDefinition, int> GetInventory()
        {
            return _inventory;
        }

        // Метод, чтобы проверить, достаточно ли ингредиентов для крафта
        public bool HasItems(List<CraftingIngredient> ingredients)
        {
            foreach (var ingredient in ingredients)
            {
                if (!_inventory.ContainsKey(ingredient.item) || _inventory[ingredient.item] < ingredient.amount)
                {
                    return false; // Не хватает ингредиента
                }
            }
            return true;
        }

        // Метод, чтобы забрать ингредиенты со склада
        public void ConsumeItems(List<CraftingIngredient> ingredients)
        {
            foreach (var ingredient in ingredients)
            {
                AddItem(ingredient.item, -ingredient.amount); // Используем AddItem с отрицательным числом, чтобы событие тоже сработало
            }
        }
    }
}