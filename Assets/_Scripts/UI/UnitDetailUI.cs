using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WarOfCrowns.Units;
using WarOfCrowns.Core;
using WarOfCrowns.Buildings;
using Unity.Netcode;

namespace WarOfCrowns.UI
{
    public class UnitDetailUI : MonoBehaviour
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

        [Header("Характеристики")]
        [SerializeField] private Slider hpSlider;
        [SerializeField] private TextMeshProUGUI hpText;
        [SerializeField] private Slider hungerSlider;
        [SerializeField] private TextMeshProUGUI hungerText;

        [Header("Инвентарь")]
        [SerializeField] private Image itemIcon;
        [SerializeField] private TextMeshProUGUI itemNameText;
        [SerializeField] private Image armorIcon;
        [SerializeField] private TextMeshProUGUI armorNameText;

        [Header("Дом")]
        [SerializeField] private Button homeButton;
        [SerializeField] private TextMeshProUGUI homeStatusText;

        private Unit _currentUnit;

        private void Start()
        {
            if (closeButton) closeButton.onClick.AddListener(Close);
        }

        public void Open(Unit unit)
        {
            _currentUnit = unit;
            if (MainUIController.Instance)
                MainUIController.Instance.CloseEverythingForUnitView();

            gameObject.SetActive(true);
            RefreshStaticInfo();
        }

        public void Close()
        {
            _currentUnit = null;
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!gameObject.activeSelf) return;
            if (_currentUnit == null) { Close(); return; }

            UpdateKingdomDisplay();
            RefreshDynamicInfo();
        }

        private void UpdateKingdomDisplay()
        {
            string kName = "Нейтральный";
            Color kColor = Color.gray;

            // 1. Пробуем взять прямо из ссылки
            if (_currentUnit.OwningKingdom != null)
            {
                kName = _currentUnit.OwningKingdom.kingdomName.Value.ToString();
                kColor = _currentUnit.OwningKingdom.kingdomColor.Value;
            }
            // 2. Если ссылки нет, но ID валидный - ищем в реестре
            else if (_currentUnit.ownerKingdomID.Value != -1)
            {
                var k = Kingdom.GetKingdomByID(_currentUnit.ownerKingdomID.Value);
                if (k != null)
                {
                    _currentUnit.OwningKingdom = k; // Кэшируем для юнита
                    kName = k.kingdomName.Value.ToString();
                    kColor = k.kingdomColor.Value;
                }
                else
                {
                    kName = "Загрузка...";
                }
            }

            kingdomText.text = kName;

            // --- ИСПРАВЛЕНИЕ: Форсируем непрозрачность ---
            kColor.a = 1f;
            kingdomText.color = kColor;
            // ---------------------------------------------
        }

        private void RefreshStaticInfo()
        {
            nameText.text = _currentUnit.UnitName;
            UpdateKingdomDisplay(); // Вызываем сразу при открытии
            UpdatePortrait();
            UpdateHomeInfo();
        }

        private void RefreshDynamicInfo()
        {
            var health = _currentUnit.GetComponent<Health>();
            if (health)
            {
                hpSlider.value = health.CurrentHealth;
                hpSlider.maxValue = health.MaxHealth;
                hpText.text = $"{health.CurrentHealth} / {health.MaxHealth}";
            }

            hungerSlider.value = _currentUnit.satiety;
            hungerText.text = $"{(int)_currentUnit.satiety}%";

            ResourceType tool = _currentUnit.Tool;
            ResourceType weapon = _currentUnit.Weapon;
            ResourceType itemToShow = (weapon != ResourceType.Wood) ? weapon : tool;

            UpdateItemSlot(itemIcon, itemNameText, itemToShow);
            UpdateItemSlot(armorIcon, armorNameText, _currentUnit.Armor);
        }

        private void UpdateItemSlot(Image icon, TextMeshProUGUI text, ResourceType item)
        {
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

        private void UpdatePortrait()
        {
            if (WorldState.Instance == null || WorldState.Instance.AppearanceDB == null) return;
            var db = WorldState.Instance.AppearanceDB;

            Sprite body = db.GetBodyByIndex(_currentUnit.bodyIndex.Value)?.idle;
            Sprite head = db.GetHeadByIndex(_currentUnit.headIndex.Value, _currentUnit.UnitGender)?.idle;
            Sprite cloth = db.GetClothesByIndex(_currentUnit.clothesIndex.Value, _currentUnit.Profession)?.idle;

            SetImage(portraitBody, body, Color.white);
            SetImage(portraitHead, head, Color.white);

            Color clothColor = Color.white;
            if (_currentUnit.OwningKingdom != null)
            {
                Color kColor = _currentUnit.OwningKingdom.kingdomColor.Value;
                float tint = _currentUnit.visualTint.Value;
                // Форсируем альфу
                clothColor = new Color(kColor.r * tint, kColor.g * tint, kColor.b * tint, 1f);
            }
            SetImage(portraitClothes, cloth, clothColor);

            Sprite armorSprite = null;
            if (_currentUnit.Armor != ResourceType.Wood) armorSprite = db.GetEquipmentSprites(_currentUnit.Armor)?.idle;
            SetImage(portraitArmor, armorSprite, Color.white);

            ResourceType handItem = (_currentUnit.Weapon != ResourceType.Wood) ? _currentUnit.Weapon : _currentUnit.Tool;
            Sprite weaponSprite = null;
            if (handItem != ResourceType.Wood) weaponSprite = db.GetEquipmentSprites(handItem)?.idle;
            SetImage(portraitWeapon, weaponSprite, Color.white);

            if (portraitPlume)
            {
                if (_currentUnit.Profession == ProfessionType.Soldier)
                {
                    Sprite plume = db.GetPlumeByIndex(_currentUnit.plumeIndex.Value)?.idle;
                    Color kColor = _currentUnit.OwningKingdom ? _currentUnit.OwningKingdom.kingdomColor.Value : Color.white;
                    kColor.a = 1f; // Форсируем
                    SetImage(portraitPlume, plume, kColor);
                }
                else
                {
                    portraitPlume.enabled = false;
                }
            }
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

        private void UpdateHomeInfo()
        {
            ulong houseID = _currentUnit.residenceNetID.Value;
            homeButton.onClick.RemoveAllListeners();

            if (houseID == 0)
            {
                homeStatusText.text = "Бездомный";
                homeButton.interactable = false;
            }
            else
            {
                homeStatusText.text = "Перейти к дому";
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
}