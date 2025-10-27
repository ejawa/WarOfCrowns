using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WarOfCrowns.Core;
using WarOfCrowns.Core.Items;
using WarOfCrowns.Buildings;
using TMPro;

namespace WarOfCrowns.UI
{
    public class WarehouseUI : MonoBehaviour
    {
        [Header("—Ò˚ÎÍË Ì‡ UI")]
        [SerializeField] private GameObject itemSlotPrefab;
        [SerializeField] private Transform contentParent;
        [SerializeField] private Button closeButton;

        [Header(" ÌÓÔÍË-¬ÍÎ‡‰ÍË")]
        [SerializeField] private Button tabAllButton;
        [SerializeField] private Button tabFoodButton;
        [SerializeField] private Button tabMaterialsButton;

        [Header("¡‡Á‡ ‰‡ÌÌ˚ı")]
        [SerializeField] private ItemDatabase itemDatabase;

        private Warehouse _currentWarehouse;
        private Dictionary<ItemDefinition, GameObject> _spawnedSlots = new Dictionary<ItemDefinition, GameObject>();

        private void Start()
        {
            if (tabAllButton != null) tabAllButton.onClick.AddListener(() => FilterInventory("All"));
            if (tabFoodButton != null) tabFoodButton.onClick.AddListener(() => FilterInventory("Food"));
            if (tabMaterialsButton != null) tabMaterialsButton.onClick.AddListener(() => FilterInventory("Materials"));
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
        }

        public void Show(Warehouse warehouse)
        {
            gameObject.SetActive(true);
            _currentWarehouse = warehouse;

            ResourceManager.OnResourceChanged += UpdateResourceSlot;
            ResourceManager.OnItemChanged += UpdateItemSlot;

            DrawAllSlots();
        }

        public void Hide()
        {
            if (_currentWarehouse != null)
            {
                ResourceManager.OnResourceChanged -= UpdateResourceSlot;
                ResourceManager.OnItemChanged -= UpdateItemSlot;
            }
            gameObject.SetActive(false);
        }

        private void DrawAllSlots()
        {
            foreach (Transform child in contentParent) Destroy(child.gameObject);
            _spawnedSlots.Clear();

            // --- œ–¿¬»À‹Õ¿ﬂ ÀŒ√» ¿ ---

            // 1. –ËÒÛÂÏ –≈—”–—€ ËÁ ResourceManager
            foreach (var resourcePair in ResourceManager.Instance.GetAllResources())
            {
                ItemDefinition itemDef = itemDatabase.GetItemByName(resourcePair.Key.ToString());
                if (itemDef != null && resourcePair.Value > 0)
                {
                    CreateOrUpdateSlot(itemDef, resourcePair.Value);
                }
            }

            // 2. –ËÒÛÂÏ œ–≈ƒÃ≈“€ ËÁ ResourceManager
            foreach (var itemPair in ResourceManager.Instance.GetAllItems())
            {
                ItemDefinition itemDef = itemDatabase.GetItemByName(itemPair.Key.ToString());
                if (itemDef != null && itemPair.Value > 0)
                {
                    CreateOrUpdateSlot(itemDef, itemPair.Value);
                }
            }
        }

        private void UpdateResourceSlot(ResourceType type, int amount)
        {
            ItemDefinition itemDef = itemDatabase.GetItemByName(type.ToString());
            if (itemDef != null) CreateOrUpdateSlot(itemDef, amount);
        }

        private void UpdateItemSlot(ItemType type, int amount)
        {
            ItemDefinition itemDef = itemDatabase.GetItemByName(type.ToString());
            if (itemDef != null) CreateOrUpdateSlot(itemDef, amount);
        }

        private void CreateOrUpdateSlot(ItemDefinition item, int amount)
        {
            if (_spawnedSlots.ContainsKey(item))
            {
                if (amount > 0) _spawnedSlots[item].transform.Find("Amount_Text").GetComponent<TextMeshProUGUI>().text = amount.ToString();
                else { Destroy(_spawnedSlots[item]); _spawnedSlots.Remove(item); }
            }
            else if (amount > 0)
            {
                GameObject newSlot = Instantiate(itemSlotPrefab, contentParent);
                newSlot.SetActive(true);
                newSlot.transform.Find("Icon").GetComponent<Image>().sprite = item.itemIcon;
                newSlot.transform.Find("ItemName_Text").GetComponent<TextMeshProUGUI>().text = item.itemName;
                newSlot.transform.Find("Amount_Text").GetComponent<TextMeshProUGUI>().text = amount.ToString();
                _spawnedSlots[item] = newSlot;
            }
        }

        private void FilterInventory(string category)
        {
            foreach (var slotPair in _spawnedSlots)
            {
                bool shouldBeActive = false;
                if (category == "All")
                {
                    shouldBeActive = true;
                }
                else if (category == "Food" && slotPair.Key is FoodDefinition)
                {
                    shouldBeActive = true;
                }
                else if (category == "Materials" && !(slotPair.Key is FoodDefinition))
                {
                    shouldBeActive = true;
                }
                slotPair.Value.SetActive(shouldBeActive);
            }
        }
    }
}