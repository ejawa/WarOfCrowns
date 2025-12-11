using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WarOfCrowns.Core;
using WarOfCrowns.Buildings;
using WarOfCrowns.Units;
using System;
using System.Text;

namespace WarOfCrowns.UI
{
    [Serializable]
    public class IconMapping { public ResourceType resourceType; public Sprite icon; }

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

        [Header("Вкладки (Доп. панели)")]
        [SerializeField] private List<UIBottomTab> bottomTabs;
        [SerializeField] private Color activeTabColor = Color.white;
        [SerializeField] private Color inactiveTabColor = Color.gray;

        [Header("НИЖНЯЯ ПАНЕЛЬ (ГЛАВНАЯ)")]
        [SerializeField] private GameObject bottomBarPanel;

        [Header("Кнопки Действий")]
        [SerializeField] private Button buildButton;
        [SerializeField] private Button infoToolButton;
        [SerializeField] private Button actionBarDemolishButton;
        [SerializeField] private Button actionBarBordersButton;
        [SerializeField] private Button toggleIconsButton;
        [SerializeField] private Button diplomacyButton;
        [SerializeField] private Button debugFoodButton;

        [Header("Окна")]
        [SerializeField] private GameObject buildMenuPanel;
        [SerializeField] private GameObject diplomacyPanel;
        [SerializeField] private UnitInfoUI unitInfoPanel;
        [SerializeField] private BuildingDetailUI buildingInfoPanel;

        [Header("Верхний Стат-бар")]
        [SerializeField] private GameObject topBarSlotPrefab;
        [SerializeField] private Transform topBarParent;
        [SerializeField] private List<ResourceType> topBarResources;
        [SerializeField] private Sprite populationIcon;
        [SerializeField] private Sprite totalFoodIcon;
        [SerializeField] private Sprite legitimacyIcon;

        [Header("Панель Склада")]
        [SerializeField] private GameObject warehousePanel;
        [SerializeField] private GameObject warehouseSlotPrefab;
        [SerializeField] private Transform warehouseContentParent;
        [SerializeField] private Button warehouseCloseButton;

        [Header("Меню Строительства (Контент)")]
        [SerializeField] private BuildManager buildManager;
        [SerializeField] private GameObject buildSlotPrefab;
        [SerializeField] private Transform buildGridParent;

        [Header("База Иконок")]
        [SerializeField] private List<IconMapping> iconMappings;

        private Dictionary<ResourceType, TextMeshProUGUI> _topBarTexts = new Dictionary<ResourceType, TextMeshProUGUI>();
        private TextMeshProUGUI _populationText;
        private TextMeshProUGUI _totalFoodText;
        private TextMeshProUGUI _legitimacyText;
        private Dictionary<ResourceType, GameObject> _warehouseSlots = new Dictionary<ResourceType, GameObject>();
        private Dictionary<ResourceType, Sprite> _iconMap = new Dictionary<ResourceType, Sprite>();

        private bool _isInitialized = false;
        private int _currentTabIndex = -1;
        private int _lastActiveTabIndex = 0; // Запоминаем последнюю вкладку

        private void Awake() { Instance = this; }

        private IEnumerator Start()
        {
            while (Kingdom.PlayerKingdom == null || PopulationManager.Instance == null)
            {
                if (buildManager == null) buildManager = FindObjectOfType<BuildManager>();
                yield return null;
            }
            _playerKingdom = Kingdom.PlayerKingdom;
            Initialize();

            _playerKingdom.OnLegitimacyChanged += UpdateLegitimacyUI;
            UpdateLegitimacyUI(_playerKingdom.legitimacy.Value);

            InvokeRepeating(nameof(RefreshResourcesForce), 0.1f, 0.5f);

            // --- ИСПРАВЛЕНИЕ: Открываем первую вкладку по умолчанию ---
            SelectTab(0);
        }

        private void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            foreach (var mapping in iconMappings)
                if (!_iconMap.ContainsKey(mapping.resourceType)) _iconMap.Add(mapping.resourceType, mapping.icon);

            if (debugFoodButton != null) debugFoodButton.onClick.AddListener(OnDebugCheckFoodButtonClicked);

            // Вкладки
            for (int i = 0; i < bottomTabs.Count; i++)
            {
                int index = i;
                if (bottomTabs[i].tabButton != null)
                    bottomTabs[i].tabButton.onClick.AddListener(() => SelectTab(index));

                if (bottomTabs[i].panelObject) bottomTabs[i].panelObject.SetActive(false);
            }

            // Кнопки
            if (buildButton) buildButton.onClick.AddListener(ToggleBuildMenu);
            if (infoToolButton) infoToolButton.onClick.AddListener(() => {
                if (buildMenuPanel) buildMenuPanel.SetActive(false);
                SetBottomBarVisible(true);
                if (InfoToolManager.Instance) InfoToolManager.Instance.ToggleInfoMode();
            });

            if (actionBarDemolishButton) actionBarDemolishButton.onClick.AddListener(() => {
                if (buildMenuPanel) buildMenuPanel.SetActive(false);
                SetBottomBarVisible(true);
                DemolishManager.Instance.ToggleDemolishMode();
            });

            if (actionBarBordersButton)
            {
                actionBarBordersButton.onClick.AddListener(() => { if (borderVisualizer) borderVisualizer.ToggleVisibility(); });
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
                    if (active) { CloseEverythingForBuildingView(); }
                    else { SetBottomBarVisible(true); }
                }
            });

            if (warehouseCloseButton) warehouseCloseButton.onClick.AddListener(CloseWarehousePanel);

            CreateTopBar();
            CreateWarehouseSlots();
            GenerateBuildButtons();

            if (warehousePanel) warehousePanel.SetActive(false);
            if (unitInfoPanel) unitInfoPanel.gameObject.SetActive(false);
            if (diplomacyPanel) diplomacyPanel.SetActive(false);
            if (buildMenuPanel) buildMenuPanel.SetActive(false);

            SubscribeToEvents();
        }

        public void OnDebugCheckFoodButtonClicked()
        {
            if (Kingdom.PlayerKingdom != null) Kingdom.PlayerKingdom.Debug_RequestFoodAmountServerRpc();
        }

        // --- ИСПРАВЛЕНИЕ: Управление баром ---
        public void SetBottomBarVisible(bool visible)
        {
            if (bottomBarPanel != null)
                bottomBarPanel.SetActive(visible);

            // Если включаем бар - восстанавливаем последнюю вкладку
            if (visible && _lastActiveTabIndex >= 0 && _lastActiveTabIndex < bottomTabs.Count)
            {
                SelectTab(_lastActiveTabIndex);
            }
        }

        private void ToggleBuildMenu()
        {
            if (buildMenuPanel == null) return;
            bool isActive = buildMenuPanel.activeSelf;

            if (isActive)
            {
                buildMenuPanel.SetActive(false);
                SetBottomBarVisible(true);
            }
            else
            {
                CloseAllTabsUIOnly(); // Скрываем панели вкладок, но не сбрасываем индекс
                if (diplomacyPanel) diplomacyPanel.SetActive(false);
                if (DemolishManager.Instance && DemolishManager.Instance.IsDemolishMode) DemolishManager.Instance.ExitDemolishMode();
                if (InfoToolManager.Instance && InfoToolManager.Instance.IsInfoMode) InfoToolManager.Instance.SetInfoMode(false);

                buildMenuPanel.SetActive(true);
                if (bottomBarPanel) bottomBarPanel.SetActive(false); // Скрываем сам бар
            }
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

                // --- ИСПРАВЛЕНИЕ: Добавляем описание ---
                var descText = slot.transform.Find("Description_Text")?.GetComponent<TextMeshProUGUI>();
                if (descText != null)
                {
                    descText.text = bData.description;
                }
                // -------------------------------------

                StringBuilder costText = new StringBuilder();
                foreach (var c in bData.costs) costText.Append($"{c.resourceType}: {c.amount} ");
                slot.transform.Find("Cost_Text").GetComponent<TextMeshProUGUI>().text = costText.ToString();

                slot.GetComponent<Button>().onClick.AddListener(() => {
                    buildManager.EnterBuildMode(foundation);
                    ToggleBuildMenu();
                });
            }
        }

        public void OpenWarehousePanel()
        {
            if (warehousePanel)
            {
                CloseAllTabsUIOnly();
                warehousePanel.SetActive(true);
                if (bottomBarPanel) bottomBarPanel.SetActive(false);
                RefreshResourcesForce();
            }
        }

        public void CloseWarehousePanel()
        {
            if (warehousePanel)
            {
                warehousePanel.SetActive(false);
                SetBottomBarVisible(true);
            }
        }

        private void UpdateLegitimacyUI(float val)
        {
            if (_legitimacyText != null)
            {
                _legitimacyText.text = $"{Mathf.FloorToInt(val)}%";
                _legitimacyText.color = (val >= 50) ? Color.green : Color.red;
            }
        }

        private void OnUnitSelectionChanged(List<Unit> selectedUnits)
        {
            if (selectedUnits == null || selectedUnits.Count == 0)
            {
                if (unitInfoPanel) unitInfoPanel.Close();
                return;
            }

            if (unitInfoPanel)
            {
                unitInfoPanel.SetTarget(selectedUnits);
                if (buildingInfoPanel) buildingInfoPanel.Close();
                if (diplomacyPanel) diplomacyPanel.SetActive(false);
                if (buildMenuPanel)
                {
                    buildMenuPanel.SetActive(false);
                    SetBottomBarVisible(true);
                }
            }
        }

        public void SelectTab(int index)
        {
            _currentTabIndex = index;
            _lastActiveTabIndex = index; // Запоминаем выбор

            for (int i = 0; i < bottomTabs.Count; i++)
            {
                bool isActive = (i == index);
                var tab = bottomTabs[i];
                var btnImage = tab.tabButton.GetComponent<Image>();
                if (btnImage) btnImage.color = isActive ? activeTabColor : inactiveTabColor;
                if (tab.panelObject != null) tab.panelObject.SetActive(isActive);
            }

            if (buildMenuPanel) { buildMenuPanel.SetActive(false); }
            if (bottomBarPanel) bottomBarPanel.SetActive(true);
        }

        private void CloseAllTabsUIOnly()
        {
            foreach (var tab in bottomTabs)
            {
                if (tab.panelObject) tab.panelObject.SetActive(false);
                if (tab.tabButton.GetComponent<Image>()) tab.tabButton.GetComponent<Image>().color = inactiveTabColor;
            }
        }

        public void CloseAllTabs()
        {
            _currentTabIndex = -1;
            CloseAllTabsUIOnly();
        }

        public void CloseEverythingForBuildingView()
        {
            CloseAllTabsUIOnly();
            if (unitInfoPanel) unitInfoPanel.Close();
            if (diplomacyPanel) diplomacyPanel.SetActive(false);
            if (buildMenuPanel) buildMenuPanel.SetActive(false);
        }

        public void CloseEverythingForUnitView()
        {
            if (buildingInfoPanel) buildingInfoPanel.Close();
            if (diplomacyPanel) diplomacyPanel.SetActive(false);
            if (buildMenuPanel) { buildMenuPanel.SetActive(false); SetBottomBarVisible(true); }
        }

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
            if (_playerKingdom != null) _playerKingdom.OnLegitimacyChanged -= UpdateLegitimacyUI;
            CancelInvoke(nameof(RefreshResourcesForce));
        }

        private void UpdatePopulationUI() { if (_populationText && PopulationManager.Instance) _populationText.text = $"{PopulationManager.Instance.CurrentPopulation}/{PopulationManager.Instance.PopulationCap}"; }
        private void UpdateTotalFoodDisplay() { if (_playerKingdom == null || _totalFoodText == null) return; int total = 0; foreach (var food in allFoodTypesInGame) total += _playerKingdom.GetResourceAmount(food); _totalFoodText.text = total.ToString(); }

        private void CreateTopBar()
        {
            foreach (var res in topBarResources)
            {
                var slot = Instantiate(topBarSlotPrefab, topBarParent);
                slot.transform.localScale = Vector3.one;
                if (_iconMap.ContainsKey(res)) slot.transform.Find("Icon").GetComponent<Image>().sprite = _iconMap[res];
                var txt = slot.transform.Find("Value_Text").GetComponent<TextMeshProUGUI>();
                txt.text = "0";
                _topBarTexts[res] = txt;
            }
            // (Остальной код топбара без изменений...)
            var foodSlot = Instantiate(topBarSlotPrefab, topBarParent); foodSlot.transform.localScale = Vector3.one; foodSlot.transform.Find("Icon").GetComponent<Image>().sprite = totalFoodIcon; _totalFoodText = foodSlot.transform.Find("Value_Text").GetComponent<TextMeshProUGUI>(); _totalFoodText.text = "0";
            var popSlot = Instantiate(topBarSlotPrefab, topBarParent); popSlot.transform.localScale = Vector3.one; popSlot.transform.Find("Icon").GetComponent<Image>().sprite = populationIcon; _populationText = popSlot.transform.Find("Value_Text").GetComponent<TextMeshProUGUI>(); _populationText.text = "0/0";
            var legSlot = Instantiate(topBarSlotPrefab, topBarParent); legSlot.transform.localScale = Vector3.one; legSlot.transform.Find("Icon").GetComponent<Image>().sprite = legitimacyIcon; _legitimacyText = legSlot.transform.Find("Value_Text").GetComponent<TextMeshProUGUI>(); _legitimacyText.text = "100%";
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

        public Sprite GetIconForResourceType(ResourceType type) { return _iconMap.TryGetValue(type, out Sprite icon) ? icon : null; }
        private void UpdateWarehouseSlot(ResourceType type, int amount) { if (!_warehouseSlots.ContainsKey(type)) return; GameObject slot = _warehouseSlots[type]; if (amount > 0) { slot.SetActive(true); slot.transform.Find("Amount_Text").GetComponent<TextMeshProUGUI>().text = amount.ToString(); } else { slot.SetActive(false); } }
        private bool IsCivilianItem(ResourceType type) { string n = type.ToString(); if (type == ResourceType.Food) return false; if (n.Contains("Sword") || n.Contains("Spear") || n.Contains("Bow") || n.Contains("Armor")) return false; return true; }
    }
}