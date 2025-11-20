using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WarOfCrowns.Core;
using WarOfCrowns.Buildings;
using System.Text;
using System;

namespace WarOfCrowns.UI
{
    [System.Serializable]
    public class IconMapping
    {
        public ResourceType resourceType;
        public Sprite icon;
    }

    public class MainUIController : MonoBehaviour
    {
        #region Inspector Fields
        [Header("Главное")]
        private Kingdom _playerKingdom;
        [Tooltip("Список ВСЕХ типов ресурсов, которые считаются едой.")]
        [SerializeField] private List<ResourceType> allFoodTypesInGame;

        [Header("Верхний Стат-бар")]
        [SerializeField] private GameObject topBarSlotPrefab;
        [SerializeField] private Transform topBarParent;
        [SerializeField] private List<ResourceType> topBarResources;
        [SerializeField] private Sprite populationIcon;
        [SerializeField] private Sprite totalFoodIcon;

        [Header("Панель Склада")]
        [SerializeField] private GameObject warehousePanel;
        [SerializeField] private GameObject warehouseSlotPrefab;
        [SerializeField] private Transform warehouseContentParent;
        [SerializeField] private Button warehouseCloseButton;
        [SerializeField] private Button warehouseTabAllButton;
        [SerializeField] private Button warehouseTabFoodButton;
        [SerializeField] private Button warehouseTabMaterialsButton;

        [Header("Меню Строительства")]
        [SerializeField] private BuildManager buildManager;
        [SerializeField] private GameObject buildMenuPanel;
        [SerializeField] private Button openBuildMenuButton;
        [SerializeField] private GameObject buildSlotPrefab;
        [SerializeField] private Transform buildGridParent;

        [Header("База Иконок")]
        [SerializeField] private List<IconMapping> iconMappings;
        #endregion

        private Dictionary<ResourceType, TextMeshProUGUI> _topBarTexts = new Dictionary<ResourceType, TextMeshProUGUI>();
        private TextMeshProUGUI _populationText;
        private TextMeshProUGUI _totalFoodText;
        private Dictionary<ResourceType, GameObject> _warehouseSlots = new Dictionary<ResourceType, GameObject>();
        private Dictionary<ResourceType, Sprite> _iconMap = new Dictionary<ResourceType, Sprite>();
        private bool _isInitialized = false;

        IEnumerator Start()
        {
            while (Kingdom.PlayerKingdom == null || PopulationManager.Instance == null || buildManager == null)
            {
                yield return null;
            }
            _playerKingdom = Kingdom.PlayerKingdom;
            Initialize();
        }

        private void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            foreach (var mapping in iconMappings)
            {
                _iconMap[mapping.resourceType] = mapping.icon;
            }

            if (warehouseCloseButton != null) warehouseCloseButton.onClick.AddListener(ToggleWarehousePanel);
            if (openBuildMenuButton != null) openBuildMenuButton.onClick.AddListener(ToggleBuildMenu);

            if (warehouseTabAllButton != null) warehouseTabAllButton.onClick.AddListener(() => FilterWarehouse("All"));
            if (warehouseTabFoodButton != null) warehouseTabFoodButton.onClick.AddListener(() => FilterWarehouse("Food"));
            if (warehouseTabMaterialsButton != null) warehouseTabMaterialsButton.onClick.AddListener(() => FilterWarehouse("Materials"));

            CreateTopBar();
            CreateWarehouseSlots();
            GenerateBuildButtons();

            if (warehousePanel != null) warehousePanel.SetActive(false);
            if (buildMenuPanel != null) buildMenuPanel.SetActive(false);

            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            if (_isInitialized) UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            if (_playerKingdom != null) _playerKingdom.OnResourceChanged += UpdateResourceUI;
            if (PopulationManager.Instance != null) PopulationManager.OnPopulationChanged += UpdatePopulationUI;
        }

        private void UnsubscribeFromEvents()
        {
            if (_playerKingdom != null) _playerKingdom.OnResourceChanged -= UpdateResourceUI;
            if (PopulationManager.Instance != null) PopulationManager.OnPopulationChanged -= UpdatePopulationUI;
        }

        public void ToggleWarehousePanel()
        {
            if (warehousePanel != null)
                warehousePanel.SetActive(!warehousePanel.activeSelf);
        }

        public void ToggleBuildMenu()
        {
            if (buildMenuPanel != null)
                buildMenuPanel.SetActive(!buildMenuPanel.activeSelf);
        }

        private void UpdateResourceUI(ResourceType type, int amount)
        {
            if (_topBarTexts.ContainsKey(type)) _topBarTexts[type].text = amount.ToString();

            bool isFood = false;
            foreach (var food in allFoodTypesInGame) { if (food == type) { isFood = true; break; } }
            if (isFood) UpdateTotalFoodDisplay();

            if (_warehouseSlots.ContainsKey(type))
            {
                Transform amountTr = _warehouseSlots[type].transform.Find("Amount_Text");
                if (amountTr != null) amountTr.GetComponent<TextMeshProUGUI>().text = amount.ToString();
            }
        }

        private void UpdatePopulationUI()
        {
            if (_populationText != null && PopulationManager.Instance != null)
                _populationText.text = $"{PopulationManager.Instance.CurrentPopulation}/{PopulationManager.Instance.PopulationCap}";
        }

        private void UpdateTotalFoodDisplay()
        {
            int totalFoodAmount = 0;
            if (_playerKingdom != null)
            {
                foreach (var foodType in allFoodTypesInGame)
                    totalFoodAmount += _playerKingdom.GetResourceAmount(foodType);
            }
            if (_totalFoodText != null) _totalFoodText.text = totalFoodAmount.ToString();
        }

        private void CreateTopBar()
        {
            foreach (var resourceType in topBarResources)
            {
                GameObject newSlot = Instantiate(topBarSlotPrefab, topBarParent);
                newSlot.transform.localScale = Vector3.one;

                Image icon = newSlot.transform.Find("Icon").GetComponent<Image>();
                if (_iconMap.ContainsKey(resourceType)) icon.sprite = _iconMap[resourceType];

                TextMeshProUGUI text = newSlot.transform.Find("Value_Text").GetComponent<TextMeshProUGUI>();
                text.text = _playerKingdom.GetResourceAmount(resourceType).ToString();
                _topBarTexts[resourceType] = text;
            }

            GameObject foodSlot = Instantiate(topBarSlotPrefab, topBarParent);
            foodSlot.transform.localScale = Vector3.one;
            foodSlot.transform.Find("Icon").GetComponent<Image>().sprite = totalFoodIcon;
            _totalFoodText = foodSlot.transform.Find("Value_Text").GetComponent<TextMeshProUGUI>();
            UpdateTotalFoodDisplay();

            GameObject popSlot = Instantiate(topBarSlotPrefab, topBarParent);
            popSlot.transform.localScale = Vector3.one;
            popSlot.transform.Find("Icon").GetComponent<Image>().sprite = populationIcon;
            _populationText = popSlot.transform.Find("Value_Text").GetComponent<TextMeshProUGUI>();
            UpdatePopulationUI();
        }

        private void CreateWarehouseSlots()
        {
            // 1. Защита: проверяем ссылки
            if (warehouseSlotPrefab == null || warehouseContentParent == null)
            {
                Debug.LogError("ОШИБКА: Не назначен Warehouse Slot Prefab или Content Parent!");
                return;
            }

            // 2. Очистка старого
            foreach (Transform child in warehouseContentParent) Destroy(child.gameObject);
            _warehouseSlots.Clear();

            // 3. Проходим по ВСЕМ ресурсам (аналог списка в топ-баре, только тут берем всё подряд)
            foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
            {
                if (type == ResourceType.Food) continue; // Пропускаем сытость

                // --- СОЗДАНИЕ (Точно как в Топ-Баре) ---
                GameObject newSlot = Instantiate(warehouseSlotPrefab, warehouseContentParent);

                // ВАЖНО: Сброс масштаба, чтобы плашка не была микроскопической или огромной
                newSlot.transform.localScale = Vector3.one;
                newSlot.transform.localPosition = Vector3.zero;

                // --- НАСТРОЙКА ИКОНКИ ---
                // Ищем картинку с именем "Icon"
                Transform iconTr = newSlot.transform.Find("Icon");
                if (iconTr != null)
                {
                    Image icon = iconTr.GetComponent<Image>();
                    if (_iconMap.ContainsKey(type))
                    {
                        icon.sprite = _iconMap[type];
                        icon.color = Color.white; // Делаем видимой
                    }
                    else
                    {
                        // Если иконки нет в списке - ставим красную заглушку
                        icon.color = Color.red;
                    }
                }

                // --- НАСТРОЙКА ИМЕНИ ---
                // Ищем текст с именем "ItemName_Text"
                Transform nameTr = newSlot.transform.Find("ItemName_Text");
                if (nameTr != null)
                {
                    nameTr.GetComponent<TextMeshProUGUI>().text = type.ToString();
                }

                // --- НАСТРОЙКА КОЛИЧЕСТВА ---
                // Ищем текст с именем "Amount_Text" (в топ-баре это Value_Text)
                Transform amountTr = newSlot.transform.Find("Amount_Text");
                if (amountTr != null)
                {
                    TextMeshProUGUI amountText = amountTr.GetComponent<TextMeshProUGUI>();
                    // Берем данные из Kingdom, как и в Топ-Баре
                    amountText.text = _playerKingdom.GetResourceAmount(type).ToString();
                }

                // Сохраняем в словарь для обновлений
                _warehouseSlots[type] = newSlot;
            }

            // Заставляем Unity пересчитать размеры списка (иногда глючит без этого)
            LayoutRebuilder.ForceRebuildLayoutImmediate(warehouseContentParent.GetComponent<RectTransform>());
        }

        private void GenerateBuildButtons()
        {
            if (buildManager == null) return;
            foreach (Transform child in buildGridParent) Destroy(child.gameObject);

            foreach (var foundationPrefab in buildManager.buildableFoundations)
            {
                if (foundationPrefab == null) continue;
                GameObject newSlot = Instantiate(buildSlotPrefab, buildGridParent);
                newSlot.transform.localScale = Vector3.one;

                Building buildableData = foundationPrefab.GetComponent<Building>();
                if (buildableData == null) { newSlot.SetActive(false); continue; }

                newSlot.transform.Find("Icon").GetComponent<Image>().sprite = buildableData.buildingIcon;
                newSlot.transform.Find("Name_Text").GetComponent<TextMeshProUGUI>().text = buildableData.buildingName;

                StringBuilder costText = new StringBuilder();
                foreach (var cost in buildableData.costs)
                    costText.Append($"{cost.resourceType}: {cost.amount} ");
                newSlot.transform.Find("Cost_Text").GetComponent<TextMeshProUGUI>().text = costText.ToString();

                Button button = newSlot.GetComponent<Button>();
                button.onClick.AddListener(() => {
                    buildManager.EnterBuildMode(foundationPrefab);
                    if (buildMenuPanel != null) buildMenuPanel.SetActive(false);
                });
            }
        }

        private void FilterWarehouse(string category)
        {
            foreach (var slotPair in _warehouseSlots)
            {
                ResourceType type = slotPair.Key;
                GameObject slotObject = slotPair.Value;

                bool isFoodType = false;
                foreach (var food in allFoodTypesInGame) { if (food == type) { isFoodType = true; break; } }

                bool shouldBeActive = false;
                if (category == "All") shouldBeActive = true;
                else if (category == "Food" && isFoodType) shouldBeActive = true;
                else if (category == "Materials" && !isFoodType && type != ResourceType.Food) shouldBeActive = true;

                slotObject.SetActive(shouldBeActive);
            }
        }
    }
}