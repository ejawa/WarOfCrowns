using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WarOfCrowns.Core;
using WarOfCrowns.Buildings;
using Unity.Netcode;
using System.Collections.Generic;
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
        [SerializeField] private TextMeshProUGUI kingdomText;
        [SerializeField] private Button closeButton;

        [Header("Стойки (Кнопки)")]
        [SerializeField] private Button aggressiveBtn;
        [SerializeField] private Button defensiveBtn;
        [SerializeField] private Button holdBtn;
        [SerializeField] private Color selectedColor = Color.green;
        [SerializeField] private Color normalColor = Color.white;

        [Header("Характеристики")]
        [SerializeField] private GameObject singleUnitStatsParent;
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
        [SerializeField] private GameObject groupIconObj;

        private Unit _targetSingleUnit;
        private List<Unit> _selectedUnits = new List<Unit>();

        private void Start()
        {
            if (closeButton) closeButton.onClick.AddListener(Close);

            if (aggressiveBtn) aggressiveBtn.onClick.AddListener(() => SetStanceForSelection(UnitStance.Aggressive));
            if (defensiveBtn) defensiveBtn.onClick.AddListener(() => SetStanceForSelection(UnitStance.Defensive));
            if (holdBtn) holdBtn.onClick.AddListener(() => SetStanceForSelection(UnitStance.Hold));
        }

        public void SetTarget(List<Unit> units)
        {
            _selectedUnits = units;
            if (units == null || units.Count == 0)
            {
                Close();
                return;
            }

            gameObject.SetActive(true);

            if (units.Count == 1)
            {
                if (units[0] != null)
                {
                    _targetSingleUnit = units[0];
                    ShowSingleUnitInfo(_targetSingleUnit);
                }
            }
            else
            {
                _targetSingleUnit = null;
                ShowGroupInfo(units);
            }
        }

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
            if (_selectedUnits == null || _selectedUnits.Count == 0 || (_targetSingleUnit == null && _selectedUnits.Count == 1))
            {
                Close();
                return;
            }

            Unit unitToShow = _targetSingleUnit != null ? _targetSingleUnit : _selectedUnits[0];
            if (unitToShow != null)
            {
                UpdateKingdomDisplayDirectly(unitToShow);
            }

            if (_targetSingleUnit != null)
            {
                RefreshDynamicInfo();
            }

            if (_selectedUnits.Count > 0 && _selectedUnits[0] != null)
            {
                UpdateStanceButtonsVisual(_selectedUnits[0].Stance);
            }
        }

        // --- ИСПРАВЛЕНИЕ ПРОЗРАЧНОСТИ ---
        private void UpdateKingdomDisplayDirectly(Unit unit)
        {
            if (kingdomText == null) return;

            int ownerId = unit.ownerKingdomID.Value;
            Kingdom k = Kingdom.GetKingdomByID(ownerId);

            if (k != null)
            {
                kingdomText.text = k.kingdomName.Value.ToString();

                // Форсируем непрозрачность (Alpha = 1)
                Color c = k.kingdomColor.Value;
                c.a = 1f;
                kingdomText.color = c;
            }
            else
            {
                if (ownerId == -1)
                {
                    kingdomText.text = "Нейтральный";
                    kingdomText.color = Color.gray;
                }
                else
                {
                    kingdomText.text = $"ID: {ownerId} (Загрузка...)";
                    kingdomText.color = Color.yellow;
                }
            }
        }
        // --------------------------------

        private void ShowSingleUnitInfo(Unit unit)
        {
            if (singleUnitStatsParent) singleUnitStatsParent.SetActive(true);
            if (groupIconObj) groupIconObj.SetActive(false);

            TogglePortraitParts(true);
            nameText.text = unit.UnitName;
            if (genderText) genderText.text = unit.UnitGender.ToString();

            UpdatePortrait(unit);
            UpdateHomeInfo(unit);
        }

        private void ShowGroupInfo(List<Unit> units)
        {
            if (singleUnitStatsParent) singleUnitStatsParent.SetActive(false);
            if (groupIconObj) groupIconObj.SetActive(true);

            TogglePortraitParts(false);
            nameText.text = $"Отряд: {units.Count}";

            if (homeButton) homeButton.interactable = false;
            if (homeStatusText) homeStatusText.text = "---";
        }

        private void RefreshDynamicInfo()
        {
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

            if (hungerSlider) hungerSlider.value = _targetSingleUnit.satiety;
            if (hungerText) hungerText.text = $"{(int)_targetSingleUnit.satiety}%";

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
            if (text) text.text = item.ToString();

            if (MainUIController.Instance != null)
            {
                Sprite beautifulIcon = MainUIController.Instance.GetIconForResourceType(item);
                if (beautifulIcon != null)
                {
                    icon.sprite = beautifulIcon;
                }
                else
                {
                    var visual = WorldState.Instance.AppearanceDB.GetEquipmentSprites(item);
                    if (visual != null) icon.sprite = visual.idle;
                }
            }
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
            // Здесь тоже берем цвет напрямую из реестра
            Kingdom k = Kingdom.GetKingdomByID(unit.ownerKingdomID.Value);
            if (k != null)
            {
                Color kColor = k.kingdomColor.Value;
                float tint = unit.visualTint.Value;
                // Форсируем Alpha = 1
                clothColor = new Color(kColor.r * tint, kColor.g * tint, kColor.b * tint, 1f);
            }
            SetImage(portraitClothes, cloth, clothColor);

            Sprite armorSprite = null;
            if (unit.Armor != ResourceType.Wood) armorSprite = db.GetEquipmentSprites(unit.Armor)?.idle;
            SetImage(portraitArmor, armorSprite, Color.white);

            ResourceType handItem = (unit.Weapon != ResourceType.Wood) ? unit.Weapon : unit.Tool;
            Sprite weaponSprite = null;
            if (handItem != ResourceType.Wood) weaponSprite = db.GetEquipmentSprites(handItem)?.idle;
            SetImage(portraitWeapon, weaponSprite, Color.white);

            if (portraitPlume != null)
            {
                if (unit.Profession == ProfessionType.Soldier)
                {
                    Sprite plume = db.GetPlumeByIndex(unit.plumeIndex.Value)?.idle;
                    Color kColor = (k != null) ? k.kingdomColor.Value : Color.white;
                    kColor.a = 1f; // Форсируем
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