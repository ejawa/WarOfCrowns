using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WarOfCrowns.Core;
using WarOfCrowns.Buildings;
using WarOfCrowns.Units;
using System;

// Указываем, какой Unit мы используем
using Unit = WarOfCrowns.Units.Unit;
using System.Text;

namespace WarOfCrowns.UI
{
    [Serializable]
    public class IconMapping
    {
        public ResourceType resourceType;
        public Sprite icon;
    }

    [Serializable]
    public class UIBottomTab
    {
        public string name;
        public Button tabButton;
        public GameObject panelObject;
    }

    public class MainUIController : MonoBehaviour
    {
        public static MainUIController Instance { get; private set; }

        [Header("Главное")]
        private Kingdom _playerKingdom;
        [SerializeField] private List<ResourceType> allFoodTypesInGame;
        [SerializeField] private BorderVisualizer borderVisualizer;

        [Header("Вкладки (Нижняя панель)")]
        [SerializeField] private List<UIBottomTab> bottomTabs;
        [SerializeField] private Color activeTabColor = Color.white;
        [SerializeField] private Color inactiveTabColor = Color.gray;

        [Header("Кнопки Действий")]
        [SerializeField] private Button infoToolButton;
        [SerializeField] private Button actionBarDemolishButton;
        [SerializeField] private Button actionBarBordersButton;
        [SerializeField] private Button toggleIconsButton;
        [SerializeField] private Button diplomacyButton;

        [Header("Окна")]
        [SerializeField] private GameObject diplomacyPanel;
        [SerializeField] private UnitInfoUI unitInfoPanel;
        [SerializeField] private BuildingDetailUI buildingInfoPanel;

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

        [Header("Меню Строительства")]
        [SerializeField] private BuildManager buildManager;
        [SerializeField] private GameObject buildSlotPrefab;
        [SerializeField] private Transform buildGridParent;

        [Header("База Иконок")]
        [SerializeField] private List<IconMapping> iconMappings;

        // Приватные
        private Dictionary<ResourceType, TextMeshProUGUI> _topBarTexts = new Dictionary<ResourceType, TextMeshProUGUI>();
        private TextMeshProUGUI _populationText;
        private TextMeshProUGUI _totalFoodText;
        private Dictionary<ResourceType, GameObject> _warehouseSlots = new Dictionary<ResourceType, GameObject>();
        private Dictionary<ResourceType, Sprite> _iconMap = new Dictionary<ResourceType, Sprite>();
        private bool _isInitialized = false;
        private int _currentTabIndex = -1;

        private void Awake()
        {
            Instance = this;
        }

        private IEnumerator Start()
        {
            while (Kingdom.PlayerKingdom == null || PopulationManager.Instance == null)
            {
                if (buildManager == null) buildManager = FindObjectOfType<BuildManager>();
                yield return null;
            }

            _playerKingdom = Kingdom.PlayerKingdom;
            Initialize();

            InvokeRepeating(nameof(RefreshResourcesForce), 0.1f, 0.5f);
        }

        private void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            foreach (var mapping in iconMappings)
                if (!_iconMap.ContainsKey(mapping.resourceType)) _iconMap.Add(mapping.resourceType, mapping.icon);

            // Вкладки
            for (int i = 0; i < bottomTabs.Count; i++)
            {
                int index = i;
                if (bottomTabs[i].tabButton != null)
                {
                    bottomTabs[i].tabButton.onClick.AddListener(() => SelectTab(index));
                }
                if (bottomTabs[i].panelObject) bottomTabs[i].panelObject.SetActive(false);
            }

            // Кнопки
            if (infoToolButton)
            {
                infoToolButton.onClick.AddListener(() => {
                    if (InfoToolManager.Instance) InfoToolManager.Instance.ToggleInfoMode();
                });
            }

            if (actionBarDemolishButton) actionBarDemolishButton.onClick.AddListener(() => DemolishManager.Instance.ToggleDemolishMode());

            if (actionBarBordersButton)
            {
                actionBarBordersButton.onClick.AddListener(() => {
                    if (borderVisualizer) borderVisualizer.ToggleVisibility();
                });
                if (borderVisualizer == null) borderVisualizer = FindObjectOfType<BorderVisualizer>();
            }

            if (toggleIconsButton)
            {
                toggleIconsButton.onClick.AddListener(() => {
                    UnitVisuals.ShowStanceIcons = !UnitVisuals.ShowStanceIcons;
                    foreach (var u in FindObjectsOfType<Unit>()) u.GetComponent<UnitVisuals>().UpdateStanceVisual(u.Stance);
                });
            }

            if (diplomacyButton) diplomacyButton.onClick.AddListener(() => {
                if (diplomacyPanel)
                {
                    bool active = !diplomacyPanel.activeSelf;
                    diplomacyPanel.SetActive(active);
                    // Дипломатия перекрывает всё
                    if (active)
                    {
                        CloseAllTabs();
                        if (unitInfoPanel) unitInfoPanel.Close();
                    }
                }
            });

            // Склад
            if (warehouseCloseButton) warehouseCloseButton.onClick.AddListener(CloseWarehousePanel);

            // Генерация
            CreateTopBar();
            CreateWarehouseSlots();
            GenerateBuildButtons();

            // Скрытие
            if (warehousePanel) warehousePanel.SetActive(false);
            if (unitInfoPanel) unitInfoPanel.gameObject.SetActive(false);
            if (diplomacyPanel) diplomacyPanel.SetActive(false);

            SubscribeToEvents();

            // Открываем первую вкладку по умолчанию
            SelectTab(0);
        }

        // --- ИСПРАВЛЕННАЯ РЕАКЦИЯ НА ВЫДЕЛЕНИЕ ---
        private void OnUnitSelectionChanged(List<Unit> selectedUnits)
        {
            if (selectedUnits == null || selectedUnits.Count == 0)
            {
                // Если сняли выделение - закрываем инфо
                if (unitInfoPanel) unitInfoPanel.Close();
                return;
            }

            // Если выделили юнитов:
            if (unitInfoPanel)
            {
                // 1. Открываем панель юнита
                unitInfoPanel.SetTarget(selectedUnits);

                // 2. Закрываем конфликтующие окна (Здания, Дипломатия)
                // НО НЕ ЗАКРЫВАЕМ НИЖНИЕ ВКЛАДКИ (CloseAllTabs убрано)
                if (buildingInfoPanel) buildingInfoPanel.Close();
                if (diplomacyPanel) diplomacyPanel.SetActive(false);
            }
        }

        // --- УПРАВЛЕНИЕ ВКЛАДКАМИ ---
        public void SelectTab(int index)
        {
            if (_currentTabIndex == index)
            {
                CloseAllTabs();
                return;
            }

            _currentTabIndex = index;

            for (int i = 0; i < bottomTabs.Count; i++)
            {
                bool isActive = (i == index);
                var tab = bottomTabs[i];

                var btnImage = tab.tabButton.GetComponent<Image>();
                if (btnImage) btnImage.color = isActive ? activeTabColor : inactiveTabColor;

                if (tab.panelObject != null)
                {
                    tab.panelObject.SetActive(isActive);
                }
            }
        }

        public void CloseAllTabs()
        {
            _currentTabIndex = -1;
            foreach (var tab in bottomTabs)
            {
                if (tab.panelObject) tab.panelObject.SetActive(false);
                if (tab.tabButton.GetComponent<Image>())
                    tab.tabButton.GetComponent<Image>().color = inactiveTabColor;
            }
        }

        // Метод, вызываемый при открытии Здания
        public void CloseEverythingForBuildingView()
        {
            // Здание перекрывает юнитов, но вкладки можно оставить? 
            // Обычно окно здания большое, лучше закрыть вкладки, чтобы не мешали.
            // Если хочешь оставить вкладки - закомментируй CloseAllTabs()
            CloseAllTabs();

            if (unitInfoPanel) unitInfoPanel.Close();
            if (diplomacyPanel) diplomacyPanel.SetActive(false);
        }

        // Метод, вызываемый при открытии Юнита (через Лупу или Выделение)
        public void CloseEverythingForUnitView()
        {
            // При открытии юнита МЫ НЕ ЗАКРЫВАЕМ ВКЛАДКИ
            // CloseAllTabs(); <--- УБРАНО

            if (buildingInfoPanel) buildingInfoPanel.Close();
            if (diplomacyPanel) diplomacyPanel.SetActive(false);
        }

        // --- СКЛАД И РЕСУРСЫ (Без изменений) ---
        public void OpenWarehousePanel()
        {
            if (warehousePanel)
            {
                CloseAllTabs(); // Склад обычно большой, закрываем вкладки
                warehousePanel.SetActive(true);
                RefreshResourcesForce();
            }
        }

        public void CloseWarehousePanel() { if (warehousePanel) warehousePanel.SetActive(false); }
        public void ToggleWarehousePanel() { if (warehousePanel.activeSelf) CloseWarehousePanel(); else OpenWarehousePanel(); }

        public void RefreshResourcesForce()
        {
            if (_playerKingdom == null) return;
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                int amount = _playerKingdom.GetResourceAmount(type);
                if (_topBarTexts.ContainsKey(type)) _topBarTexts[type].text = amount.ToString();
                UpdateWarehouseSlot(type, amount);
            }
            UpdatePopulationUI();
            UpdateTotalFoodDisplay();
        }

        private void SubscribeToEvents()
        {
            if (PopulationManager.Instance != null) PopulationManager.OnPopulationChanged += UpdatePopulationUI;
            UnitSelectionController.OnSelectionChanged += OnUnitSelectionChanged;
        }

        private void OnDestroy()
        {
            if (PopulationManager.Instance != null) PopulationManager.OnPopulationChanged -= UpdatePopulationUI;
            UnitSelectionController.OnSelectionChanged -= OnUnitSelectionChanged;
            CancelInvoke(nameof(RefreshResourcesForce));
        }

        // --- ГЕНЕРАЦИЯ UI ---
        private void UpdatePopulationUI() { if (_populationText && PopulationManager.Instance) _populationText.text = $"{PopulationManager.Instance.CurrentPopulation}/{PopulationManager.Instance.PopulationCap}"; }
        private void UpdateTotalFoodDisplay() { if (_playerKingdom == null || _totalFoodText == null) return; int total = 0; foreach (var food in allFoodTypesInGame) total += _playerKingdom.GetResourceAmount(food); _totalFoodText.text = total.ToString(); }

        private void CreateTopBar()
        {
            foreach (var res in topBarResources) { var slot = Instantiate(topBarSlotPrefab, topBarParent); slot.transform.localScale = Vector3.one; if (_iconMap.ContainsKey(res)) slot.transform.Find("Icon").GetComponent<Image>().sprite = _iconMap[res]; var txt = slot.transform.Find("Value_Text").GetComponent<TextMeshProUGUI>(); txt.text = "0"; _topBarTexts[res] = txt; }
            var foodSlot = Instantiate(topBarSlotPrefab, topBarParent); foodSlot.transform.localScale = Vector3.one; foodSlot.transform.Find("Icon").GetComponent<Image>().sprite = totalFoodIcon; _totalFoodText = foodSlot.transform.Find("Value_Text").GetComponent<TextMeshProUGUI>(); _totalFoodText.text = "0";
            var popSlot = Instantiate(topBarSlotPrefab, topBarParent); popSlot.transform.localScale = Vector3.one; popSlot.transform.Find("Icon").GetComponent<Image>().sprite = populationIcon; _populationText = popSlot.transform.Find("Value_Text").GetComponent<TextMeshProUGUI>(); _populationText.text = "0/0";
        }

        private void CreateWarehouseSlots()
        {
            if (!warehouseSlotPrefab || !warehouseContentParent) return;
            foreach (Transform child in warehouseContentParent) Destroy(child.gameObject);
            _warehouseSlots.Clear();
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                if (!IsCivilianItem(type)) continue;
                var slot = Instantiate(warehouseSlotPrefab, warehouseContentParent);
                slot.name = $"Slot_{type}";
                slot.transform.localScale = Vector3.one;
                var icon = slot.transform.Find("Icon");
                if (icon && _iconMap.ContainsKey(type)) icon.GetComponent<Image>().sprite = _iconMap[type];
                if (slot.transform.Find("ItemName_Text")) slot.transform.Find("ItemName_Text").GetComponent<TextMeshProUGUI>().text = type.ToString();
                slot.transform.Find("Amount_Text").GetComponent<TextMeshProUGUI>().text = "0";
                slot.SetActive(false);
                _warehouseSlots[type] = slot;
            }
        }

        private void UpdateWarehouseSlot(ResourceType type, int amount)
        {
            if (!_warehouseSlots.ContainsKey(type)) return;
            GameObject slot = _warehouseSlots[type];
            if (amount > 0) { slot.SetActive(true); slot.transform.Find("Amount_Text").GetComponent<TextMeshProUGUI>().text = amount.ToString(); }
            else { slot.SetActive(false); }
        }

        private void GenerateBuildButtons()
        {
            if (!buildManager || !buildGridParent || !buildSlotPrefab) return;
            foreach (Transform child in buildGridParent) Destroy(child.gameObject);
            foreach (var foundation in buildManager.buildableFoundations)
            {
                if (!foundation) continue;
                var bData = foundation.GetComponent<Building>();
                if (!bData) continue;
                var slot = Instantiate(buildSlotPrefab, buildGridParent);
                slot.transform.localScale = Vector3.one;
                slot.transform.Find("Icon").GetComponent<Image>().sprite = bData.buildingIcon;
                slot.transform.Find("Name_Text").GetComponent<TextMeshProUGUI>().text = bData.buildingName;
                StringBuilder costText = new StringBuilder();
                foreach (var c in bData.costs) costText.Append($"{c.resourceType}: {c.amount}  ");
                slot.transform.Find("Cost_Text").GetComponent<TextMeshProUGUI>().text = costText.ToString();
                slot.GetComponent<Button>().onClick.AddListener(() => {
                    buildManager.EnterBuildMode(foundation);
                });
            }
        }

        private bool IsCivilianItem(ResourceType type)
        {
            string n = type.ToString();
            if (type == ResourceType.Food) return false;
            if (n.Contains("Sword") || n.Contains("Spear") || n.Contains("Bow") || n.Contains("Armor")) return false;
            return true;
        }
    }
}