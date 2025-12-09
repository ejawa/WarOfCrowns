using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WarOfCrowns.Core;
using WarOfCrowns.Buildings;
using Unity.Netcode;
using System.Collections.Generic;

// Решение конфликта имен
using Unit = WarOfCrowns.Units.Unit;
using UnitStance = WarOfCrowns.Units.UnitStance;

namespace WarOfCrowns.UI
{
    public class UnitInfoUI : MonoBehaviour
    {
        [Header("Основные элементы")]
        [SerializeField] private Image portraitBody;
        [SerializeField] private Image portraitClothes;
        [SerializeField] private Image portraitHead;
        [SerializeField] private Image portraitArmor;
        [SerializeField] private Image portraitWeapon;
        [SerializeField] private Image portraitPlume;

        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI kingdomText; // Инфо о королевстве
        [SerializeField] private Button closeButton;

        [Header("Стойки (Кнопки)")]
        [SerializeField] private Button aggressiveBtn;
        [SerializeField] private Button defensiveBtn;
        [SerializeField] private Button holdBtn;
        [SerializeField] private Color selectedColor = Color.green;
        [SerializeField] private Color normalColor = Color.white;

        [Header("Характеристики (Скрываются при мульти-выборе)")]
        [SerializeField] private GameObject singleUnitStatsParent; // Родитель для полосок и инвентаря
        [SerializeField] private Slider hpSlider;
        [SerializeField] private TextMeshProUGUI hpText;
        [SerializeField] private Slider hungerSlider;
        [SerializeField] private TextMeshProUGUI hungerText;
        [SerializeField] private TextMeshProUGUI genderText;

        [Header("Инвентарь")]
        [SerializeField] private Image itemIcon;
        [SerializeField] private TextMeshProUGUI itemNameText;
        [SerializeField] private Image armorIcon;
        [SerializeField] private TextMeshProUGUI armorNameText;

        [Header("Дом")]
        [SerializeField] private Button homeButton;
        [SerializeField] private TextMeshProUGUI homeStatusText;

        [Header("Группа")]
        [SerializeField] private GameObject groupIconObj; // Иконка "Толпы", если выбрано много

        private Unit _targetSingleUnit;
        private List<Unit> _selectedUnits = new List<Unit>();

        private void Start()
        {
            if (closeButton) closeButton.onClick.AddListener(Close);

            // Привязка кнопок стоек
            if (aggressiveBtn) aggressiveBtn.onClick.AddListener(() => SetStanceForSelection(UnitStance.Aggressive));
            if (defensiveBtn) defensiveBtn.onClick.AddListener(() => SetStanceForSelection(UnitStance.Defensive));
            if (holdBtn) holdBtn.onClick.AddListener(() => SetStanceForSelection(UnitStance.Hold));
        }

        // --- ГЛАВНЫЙ МЕТОД (ВЫЗЫВАЕТСЯ ИЗ MAIN UI) ---
        public void SetTarget(List<Unit> units)
        {
            _selectedUnits = units;

            if (units == null || units.Count == 0)
            {
                Close();
                return;
            }

            // Открываем окно
            gameObject.SetActive(true);

            if (units.Count == 1)
            {
                // ОДИН ЮНИТ
                if (units[0] != null)
                {
                    _targetSingleUnit = units[0];
                    ShowSingleUnitInfo(_targetSingleUnit);
                }
            }
            else
            {
                // ГРУППА ЮНИТОВ
                _targetSingleUnit = null;
                ShowGroupInfo(units);
            }
        }
        // ---------------------------------------------

        public void Close()
        {
            if (_targetSingleUnit != null) _targetSingleUnit.Deselect();
            _targetSingleUnit = null;
            _selectedUnits = null;
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!gameObject.activeSelf) return;

            // Если был выбран один юнит, но он умер
            if (_targetSingleUnit == null && (_selectedUnits == null || _selectedUnits.Count <= 1))
            {
                Close();
                return;
            }

            if (_targetSingleUnit != null)
            {
                RefreshDynamicInfo();
            }

            // Обновляем визуал кнопок стоек (берем состояние первого юнита)
            if (_selectedUnits != null && _selectedUnits.Count > 0 && _selectedUnits[0] != null)
            {
                UpdateStanceButtonsVisual(_selectedUnits[0].Stance);
            }
        }

        // --- ОТОБРАЖЕНИЕ ОДНОГО ЮНИТА ---
        private void ShowSingleUnitInfo(Unit unit)
        {
            if (singleUnitStatsParent) singleUnitStatsParent.SetActive(true);
            if (groupIconObj) groupIconObj.SetActive(false);

            // Включаем портрет (части тела)
            TogglePortraitParts(true);

            // Имя
            nameText.text = unit.UnitName;
            if (genderText) genderText.text = unit.UnitGender.ToString();

            // Инфо о Королевстве
            UpdateKingdomInfo(unit);

            // Портрет
            UpdatePortrait(unit);

            // Дом
            UpdateHomeInfo(unit);

            // Динамические статы (ХП, Голод, Инвентарь) обновятся в Update -> RefreshDynamicInfo
            RefreshDynamicInfo();
        }

        // --- ОТОБРАЖЕНИЕ ГРУППЫ ---
        private void ShowGroupInfo(List<Unit> units)
        {
            if (singleUnitStatsParent) singleUnitStatsParent.SetActive(false); // Скрываем инвентарь и ХП
            if (groupIconObj) groupIconObj.SetActive(true);

            // Скрываем детальный портрет
            TogglePortraitParts(false);

            nameText.text = $"Отряд: {units.Count}";

            // Для группы показываем королевство первого юнита
            if (units[0] != null) UpdateKingdomInfo(units[0]);

            // Кнопка дома недоступна для толпы
            if (homeButton) homeButton.interactable = false;
            if (homeStatusText) homeStatusText.text = "---";
        }

        private void UpdateKingdomInfo(Unit unit)
        {
            if (unit.OwningKingdom != null)
            {
                kingdomText.text = unit.OwningKingdom.kingdomName.Value.ToString();
                kingdomText.color = unit.OwningKingdom.kingdomColor.Value;
            }
            else
            {
                kingdomText.text = "Нейтральный";
                kingdomText.color = Color.gray;
            }
        }

        private void RefreshDynamicInfo()
        {
            // ХП
            var health = _targetSingleUnit.GetComponent<Health>();
            if (health)
            {
                if (hpSlider)
                {
                    hpSlider.value = health.CurrentHealth;
                    hpSlider.maxValue = health.MaxHealth;
                }
                if (hpText) hpText.text = $"{health.CurrentHealth} / {health.MaxHealth}";
            }

            // Голод
            if (hungerSlider) hungerSlider.value = _targetSingleUnit.satiety;
            if (hungerText) hungerText.text = $"{(int)_targetSingleUnit.satiety}%";

            // Инвентарь
            ResourceType tool = _targetSingleUnit.Tool;
            ResourceType weapon = _targetSingleUnit.Weapon;
            ResourceType itemToShow = (weapon != ResourceType.Wood) ? weapon : tool;

            UpdateItemSlot(itemIcon, itemNameText, itemToShow);
            UpdateItemSlot(armorIcon, armorNameText, _targetSingleUnit.Armor);
        }

        private void UpdateItemSlot(Image icon, TextMeshProUGUI text, ResourceType item)
        {
            if (icon == null) return;

            if (item == ResourceType.Wood)
            {
                icon.enabled = false;
                if (text) text.text = "Пусто";
                return;
            }

            icon.enabled = true;
            if (WorldState.Instance && WorldState.Instance.AppearanceDB)
            {
                var visual = WorldState.Instance.AppearanceDB.GetEquipmentSprites(item);
                if (visual != null) icon.sprite = visual.idle;
            }
            if (text) text.text = item.ToString();
        }

        private void UpdatePortrait(Unit unit)
        {
            if (WorldState.Instance == null || WorldState.Instance.AppearanceDB == null) return;
            var db = WorldState.Instance.AppearanceDB;

            Sprite body = db.GetBodyByIndex(unit.bodyIndex.Value)?.idle;
            Sprite head = db.GetHeadByIndex(unit.headIndex.Value, unit.UnitGender)?.idle;
            Sprite cloth = db.GetClothesByIndex(unit.clothesIndex.Value, unit.Profession)?.idle;

            SetImage(portraitBody, body, Color.white);
            SetImage(portraitHead, head, Color.white);

            Color clothColor = Color.white;
            if (unit.OwningKingdom != null)
            {
                Color kColor = unit.OwningKingdom.kingdomColor.Value;
                float tint = unit.visualTint.Value;
                clothColor = new Color(kColor.r * tint, kColor.g * tint, kColor.b * tint, 1f);
            }
            SetImage(portraitClothes, cloth, clothColor);

            // Экипировка на портрете
            Sprite armorSprite = null;
            if (unit.Armor != ResourceType.Wood) armorSprite = db.GetEquipmentSprites(unit.Armor)?.idle;
            SetImage(portraitArmor, armorSprite, Color.white);

            ResourceType handItem = (unit.Weapon != ResourceType.Wood) ? unit.Weapon : unit.Tool;
            Sprite weaponSprite = null;
            if (handItem != ResourceType.Wood) weaponSprite = db.GetEquipmentSprites(handItem)?.idle;
            SetImage(portraitWeapon, weaponSprite, Color.white);

            // Щетка (Плюмаж)
            if (portraitPlume != null)
            {
                if (unit.Profession == ProfessionType.Soldier)
                {
                    Sprite plume = db.GetPlumeByIndex(unit.plumeIndex.Value)?.idle;
                    Color kColor = unit.OwningKingdom ? unit.OwningKingdom.kingdomColor.Value : Color.white;
                    SetImage(portraitPlume, plume, kColor);
                }
                else
                {
                    portraitPlume.enabled = false;
                }
            }
        }

        private void UpdateHomeInfo(Unit unit)
        {
            ulong houseID = unit.residenceNetID.Value;
            if (homeButton) homeButton.onClick.RemoveAllListeners();

            if (houseID == 0)
            {
                if (homeStatusText) homeStatusText.text = "Бездомный";
                if (homeButton) homeButton.interactable = false;
            }
            else
            {
                if (homeStatusText) homeStatusText.text = "Перейти к дому";
                if (homeButton)
                {
                    homeButton.interactable = true;
                    homeButton.onClick.AddListener(() => {
                        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(houseID, out var netObj))
                        {
                            if (Camera.main)
                            {
                                Vector3 pos = netObj.transform.position;
                                pos.z = -10;
                                Camera.main.transform.position = pos;
                            }
                        }
                    });
                }
            }
        }

        // --- УПРАВЛЕНИЕ СТОЙКАМИ ---
        private void SetStanceForSelection(UnitStance stance)
        {
            if (_selectedUnits == null) return;
            foreach (var unit in _selectedUnits)
            {
                if (unit != null) unit.SetStance(stance);
            }
            UpdateStanceButtonsVisual(stance);
        }

        private void UpdateStanceButtonsVisual(UnitStance currentStance)
        {
            if (aggressiveBtn) aggressiveBtn.image.color = (currentStance == UnitStance.Aggressive) ? selectedColor : normalColor;
            if (defensiveBtn) defensiveBtn.image.color = (currentStance == UnitStance.Defensive) ? selectedColor : normalColor;
            if (holdBtn) holdBtn.image.color = (currentStance == UnitStance.Hold) ? selectedColor : normalColor;
        }

        // --- ВСПОМОГАТЕЛЬНЫЕ ---
        private void SetImage(Image img, Sprite sprite, Color color)
        {
            if (img == null) return;
            if (sprite != null)
            {
                img.sprite = sprite;
                img.color = color;
                img.enabled = true;
            }
            else
            {
                img.sprite = null;
                img.enabled = false;
            }
        }

        private void TogglePortraitParts(bool active)
        {
            if (portraitBody) portraitBody.gameObject.SetActive(active);
            if (portraitHead) portraitHead.gameObject.SetActive(active);
            if (portraitClothes) portraitClothes.gameObject.SetActive(active);
            if (portraitArmor) portraitArmor.gameObject.SetActive(active);
            if (portraitWeapon) portraitWeapon.gameObject.SetActive(active);
            if (portraitPlume) portraitPlume.gameObject.SetActive(active);
        }
    }
}