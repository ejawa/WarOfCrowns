using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WarOfCrowns.Core;
using WarOfCrowns.Buildings;
using WarOfCrowns.Units; // <-- ВАЖНО: Добавили это
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
        [SerializeField] private List<ResourceType> allFoodTypesInGame;

        // --- ВОТ ЭТО БЫЛО ПРОПУЩЕНО ---
        [Header("Инфо-Панель Юнита")]
        [SerializeField] private UnitInfoUI unitInfoPanel;
        // ------------------------------

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

        #region Initialization
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

            foreach (var mapping in iconMappings) _iconMap[mapping.resourceType] = mapping.icon;

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
            // UnitInfoPanel управляет своей видимостью сама, ее скрывать не обязательно, но можно
            if (unitInfoPanel != null) unitInfoPanel.gameObject.SetActive(false);

            SubscribeToEvents();
        }

        private void OnDestroy() { if (_isInitialized) UnsubscribeFromEvents(); }

        private void SubscribeToEvents()
        {
            if (_playerKingdom != null) _playerKingdom.OnResourceChanged += UpdateResourceUI;
            if (PopulationManager.Instance != null) PopulationManager.OnPopulationChanged += UpdatePopulationUI;

            // --- ВАЖНО: ПОДПИСКА НА ВЫДЕЛЕНИЕ ---
            UnitSelectionController.OnSelectionChanged += OnUnitSelectionChanged;
            // ------------------------------------
        }

        private void UnsubscribeFromEvents()
        {
            if (_playerKingdom != null) _playerKingdom.OnResourceChanged -= UpdateResourceUI;
            if (PopulationManager.Instance != null) PopulationManager.OnPopulationChanged -= UpdatePopulationUI;

            UnitSelectionController.OnSelectionChanged -= OnUnitSelectionChanged;
        }
        #endregion

        // --- ОБРАБОТЧИК ВЫДЕЛЕНИЯ ---
        private void OnUnitSelectionChanged(List<Unit> selectedUnits)
        {
            if (unitInfoPanel != null)
            {
                unitInfoPanel.SetTarget(selectedUnits);
            }
        }
        // ----------------------------

        #region UI Panel Logic
        // 1. ЗАМЕНИ ЭТОТ МЕТОД
        public void ToggleWarehousePanel()
        {
            if (warehousePanel == null) return;

            bool isActive = !warehousePanel.activeSelf;
            warehousePanel.SetActive(isActive);

            // --- ИСПРАВЛЕНИЕ: Принудительное обновление при открытии ---
            if (isActive)
            {
                RefreshWarehouseUI();
            }
        }

        // 2. ДОБАВЬ ЭТОТ МЕТОД (если его нет)
        private void RefreshWarehouseUI()
        {
            // Берем наше локальное королевство
            Kingdom myKingdom = Kingdom.PlayerKingdom;
            if (myKingdom == null) return;

            // Проходимся по ВСЕМ возможным типам ресурсов
            foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
            {
                int currentAmount = myKingdom.GetResourceAmount(type);
                // Вызываем стандартный метод обновления слота
                UpdateWarehouseSlot(type, currentAmount);
            }
        }
        public void ToggleBuildMenu()
        {
            if (buildMenuPanel != null) buildMenuPanel.SetActive(!buildMenuPanel.activeSelf);
        }
        #endregion

        #region Updates
        private void UpdateResourceUI(ResourceType type, int amount)
        {
            if (_topBarTexts.ContainsKey(type)) _topBarTexts[type].text = amount.ToString();
            bool isFood = false;
            foreach (var food in allFoodTypesInGame) { if (food == type) { isFood = true; break; } }
            if (isFood) UpdateTotalFoodDisplay();
            if (warehousePanel != null && warehousePanel.activeSelf) UpdateWarehouseSlot(type, amount);
        }

        private void UpdatePopulationUI()
        {
            if (_populationText != null && PopulationManager.Instance != null)
                _populationText.text = $"{PopulationManager.Instance.CurrentPopulation}/{PopulationManager.Instance.PopulationCap}";
        }

        private void UpdateTotalFoodDisplay()
        {
            int total = 0;
            if (_playerKingdom != null) { foreach (var food in allFoodTypesInGame) total += _playerKingdom.GetResourceAmount(food); }
            if (_totalFoodText != null) _totalFoodText.text = total.ToString();
        }
        #endregion

        #region Drawing (Warehouse & TopBar & Build)
        private void CreateTopBar()
        { /* ... код без изменений, скопируй из прошлого или оставь как есть, если ты не менял структуру ... */
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
            if (warehouseSlotPrefab == null || warehouseContentParent == null) return;

            // Очистка
            foreach (Transform child in warehouseContentParent) Destroy(child.gameObject);
            _warehouseSlots.Clear();

            // Проходим по ВСЕМ ресурсам
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                // --- ФИЛЬТРЫ ---

                // 1. Проверяем, гражданский ли это предмет
                if (!IsCivilianItem(type)) continue;

                // 2. Проверяем количество (скрываем нули)
                int amount = _playerKingdom.GetResourceAmount(type);
                if (amount <= 0) continue;

                // ----------------

                // --- СОЗДАНИЕ И НАСТРОЙКА (Твой "большой" код) ---

                GameObject newSlot = Instantiate(warehouseSlotPrefab, warehouseContentParent);
                newSlot.name = $"Slot_{type}";

                // Сброс масштаба
                newSlot.transform.localScale = Vector3.one;
                newSlot.transform.localPosition = Vector3.zero;

                // Иконка
                Transform iconTr = newSlot.transform.Find("Icon");
                if (iconTr != null)
                {
                    if (_iconMap.ContainsKey(type))
                    {
                        Image img = iconTr.GetComponent<Image>();
                        img.sprite = _iconMap[type];
                        img.color = Color.white; // Делаем видимым
                    }
                    else
                    {
                        iconTr.GetComponent<Image>().color = Color.red; // Красим в красный если нет иконки
                    }
                }

                // Имя
                Transform nameTr = newSlot.transform.Find("ItemName_Text");
                if (nameTr != null) nameTr.GetComponent<TextMeshProUGUI>().text = type.ToString();

                // Количество
                Transform amountTr = newSlot.transform.Find("Amount_Text");
                if (amountTr != null)
                {
                    amountTr.GetComponent<TextMeshProUGUI>().text = amount.ToString();
                }

                // Сохраняем в словарь
                _warehouseSlots[type] = newSlot;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(warehouseContentParent.GetComponent<RectTransform>());
        }
        private bool IsCivilianItem(ResourceType type)
        {
            string name = type.ToString();
            // Если это оружие или броня - скрываем
            if (name.Contains("Sword") || name.Contains("Spear") || name.Contains("Bow") || name.Contains("Armor"))
                return false;

            // Инструменты ты просил оставить
            // if (name.Contains("Pickaxe") || name.Contains("Axe")) return false; // Раскомментируй, если хочешь скрыть и их

            // Еду (Сытость) тоже скрываем, она в топ-баре
            if (type == ResourceType.Food) return false;

            return true;
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
                foreach (var cost in buildableData.costs) costText.Append($"{cost.resourceType}: {cost.amount} ");
                newSlot.transform.Find("Cost_Text").GetComponent<TextMeshProUGUI>().text = costText.ToString();
                if (newSlot.transform.Find("Description_Text") != null) newSlot.transform.Find("Description_Text").GetComponent<TextMeshProUGUI>().text = buildableData.description;
                Button button = newSlot.GetComponent<Button>();
                button.onClick.AddListener(() => {
                    buildManager.EnterBuildMode(foundationPrefab);
                    if (buildMenuPanel != null) buildMenuPanel.SetActive(false);
                });
            }
        }

        private void UpdateWarehouseSlot(ResourceType type, int amount)
        {
            // Фильтр
            if (!IsCivilianItem(type)) return;

            // Вариант 1: Плашка уже есть -> Обновляем или удаляем
            if (_warehouseSlots.ContainsKey(type))
            {
                if (amount > 0)
                {
                    // Обновляем текст
                    _warehouseSlots[type].transform.Find("Amount_Text").GetComponent<TextMeshProUGUI>().text = amount.ToString();
                }
                else
                {
                    // Стало 0 -> Удаляем
                    Destroy(_warehouseSlots[type]);
                    _warehouseSlots.Remove(type);
                }
            }
            // Вариант 2: Плашки нет, но ресурс появился (> 0) -> Создаем новую
            else if (amount > 0)
            {
                GameObject newSlot = Instantiate(warehouseSlotPrefab, warehouseContentParent);
                newSlot.name = $"Slot_{type}";
                newSlot.transform.localScale = Vector3.one;
                newSlot.transform.localPosition = Vector3.zero;

                // Настройка Иконки
                Transform iconTr = newSlot.transform.Find("Icon");
                if (iconTr != null && _iconMap.ContainsKey(type))
                {
                    iconTr.GetComponent<Image>().sprite = _iconMap[type];
                    iconTr.GetComponent<Image>().color = Color.white;
                }

                // Настройка Текста
                if (newSlot.transform.Find("ItemName_Text") != null)
                    newSlot.transform.Find("ItemName_Text").GetComponent<TextMeshProUGUI>().text = type.ToString();

                newSlot.transform.Find("Amount_Text").GetComponent<TextMeshProUGUI>().text = amount.ToString();

                _warehouseSlots[type] = newSlot;
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
        #endregion
    }
}