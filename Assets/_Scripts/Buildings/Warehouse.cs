using System; // <-- ��� Action
using System.Collections.Generic;
using UnityEngine;
using WarOfCrowns.Core.Items;

namespace WarOfCrowns.Buildings
{
    public class Warehouse : MonoBehaviour
    {
        // ������� ������ �� �����������. ��� ����������� ����� ����������� ������.
        public event Action<ItemDefinition, int> OnInventoryChanged;

        // ����� ����� ��������� ���������: <ItemDefinition, ����������>
        private Dictionary<ItemDefinition, int> _inventory = new Dictionary<ItemDefinition, int>();

        // �����, ����� ��������/������ �������
        public void AddItem(ItemDefinition item, int amount)
        {
            if (!_inventory.ContainsKey(item)) _inventory[item] = 0;
            _inventory[item] += amount;

            // "������" � ���, ��� ��������� ���������
            OnInventoryChanged?.Invoke(item, _inventory[item]);

            Debug.Log($"Changed {item.itemName} by {amount}. New total: {_inventory[item]}");
        }

        // �����, ����� UI ��� �������� ������ ��������� ��������� ��� ��������
        public Dictionary<ItemDefinition, int> GetInventory()
        {
            return _inventory;
        }

        // �����, ����� ���������, ���������� �� ������������ ��� ������
        public bool HasItems(List<CraftingIngredient> ingredients)
        {
            foreach (var ingredient in ingredients)
            {
                if (!_inventory.ContainsKey(ingredient.item) || _inventory[ingredient.item] < ingredient.amount)
                {
                    return false; // �� ������� �����������
                }
            }
            return true;
        }

        // �����, ����� ������� ����������� �� ������
        public void ConsumeItems(List<CraftingIngredient> ingredients)
        {
            foreach (var ingredient in ingredients)
            {
                AddItem(ingredient.item, -ingredient.amount); // ���������� AddItem � ������������� ������, ����� ������� ���� ���������
            }
        }
    }
}