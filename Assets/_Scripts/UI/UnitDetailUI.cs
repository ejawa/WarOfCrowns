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
        [SerializeField] private TextMeshProUGUI homeStatusText; // "Живет в Доме" или "Бездомный"

        private Unit _currentUnit;

        private void Start()
        {
            if (closeButton) closeButton.onClick.AddListener(Close);
        }

        public void Open(Unit unit)
        {
            _currentUnit = unit;

            // ВАЖНО: Закрываем другие окна
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

            // Если юнит умер или исчез
            if (_currentUnit == null)
            {
                Close();
                return;
            }

            // Динамическое обновление (ХП, Голод, Экипировка - вдруг поменялась)
            RefreshDynamicInfo();
        }

        private void RefreshStaticInfo()
        {
            // Имя
            nameText.text = _currentUnit.UnitName;

            // Королевство
            if (_currentUnit.OwningKingdom != null)
            {
                kingdomText.text = _currentUnit.OwningKingdom.kingdomName.Value.ToString();
                kingdomText.color = _currentUnit.OwningKingdom.kingdomColor.Value;
            }
            else
            {
                kingdomText.text = "Нейтральный";
                kingdomText.color = Color.gray;
            }

            // Портрет
            UpdatePortrait();

            // Дом (кнопка)
            UpdateHomeInfo();
        }

        private void RefreshDynamicInfo()
        {
            // ХП
            var health = _currentUnit.GetComponent<Health>();
            if (health)
            {
                hpSlider.value = health.CurrentHealth;
                hpSlider.maxValue = health.MaxHealth;
                hpText.text = $"{health.CurrentHealth} / {health.MaxHealth}";
            }

            // Голод
            hungerSlider.value = _currentUnit.satiety;
            hungerText.text = $"{(int)_currentUnit.satiety}%";

            // Инвентарь (Оружие/Инструмент)
            ResourceType tool = _currentUnit.Tool;
            ResourceType weapon = _currentUnit.Weapon;
            ResourceType itemToShow = (weapon != ResourceType.Wood) ? weapon : tool;

            UpdateItemSlot(itemIcon, itemNameText, itemToShow);

            // Броня
            UpdateItemSlot(armorIcon, armorNameText, _currentUnit.Armor);
        }

        private void UpdateItemSlot(Image icon, TextMeshProUGUI text, ResourceType item)
        {
            if (item == ResourceType.Wood) // Wood считаем как "Пусто" для слота
            {
                icon.enabled = false;
                text.text = "Пусто";
                return;
            }

            icon.enabled = true;
            if (WorldState.Instance && WorldState.Instance.AppearanceDB)
            {
                var visual = WorldState.Instance.AppearanceDB.GetEquipmentSprites(item);
                if (visual != null) icon.sprite = visual.idle;
            }
            text.text = item.ToString();
        }

        private void UpdatePortrait()
        {
            if (WorldState.Instance == null || WorldState.Instance.AppearanceDB == null) return;
            var db = WorldState.Instance.AppearanceDB;

            // База
            Sprite body = db.GetBodyByIndex(_currentUnit.bodyIndex.Value)?.idle;
            Sprite head = db.GetHeadByIndex(_currentUnit.headIndex.Value, _currentUnit.UnitGender)?.idle;
            Sprite cloth = db.GetClothesByIndex(_currentUnit.clothesIndex.Value, _currentUnit.Profession)?.idle;

            SetImage(portraitBody, body, Color.white);
            SetImage(portraitHead, head, Color.white);

            // Одежда (цветная)
            Color clothColor = Color.white;
            if (_currentUnit.OwningKingdom != null)
            {
                Color kColor = _currentUnit.OwningKingdom.kingdomColor.Value;
                float tint = _currentUnit.visualTint.Value;
                clothColor = new Color(kColor.r * tint, kColor.g * tint, kColor.b * tint, 1f);
            }
            SetImage(portraitClothes, cloth, clothColor);

            // Экипировка
            Sprite armorSprite = null;
            if (_currentUnit.Armor != ResourceType.Wood) armorSprite = db.GetEquipmentSprites(_currentUnit.Armor)?.idle;
            SetImage(portraitArmor, armorSprite, Color.white);

            ResourceType handItem = (_currentUnit.Weapon != ResourceType.Wood) ? _currentUnit.Weapon : _currentUnit.Tool;
            Sprite weaponSprite = null;
            if (handItem != ResourceType.Wood) weaponSprite = db.GetEquipmentSprites(handItem)?.idle;
            SetImage(portraitWeapon, weaponSprite, Color.white);

            // Щетка
            if (portraitPlume)
            {
                if (_currentUnit.Profession == ProfessionType.Soldier)
                {
                    Sprite plume = db.GetPlumeByIndex(_currentUnit.plumeIndex.Value)?.idle;
                    Color kColor = _currentUnit.OwningKingdom ? _currentUnit.OwningKingdom.kingdomColor.Value : Color.white;
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

            // Очищаем старые листенеры
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
                    // Ищем объект дома
                    if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(houseID, out var netObj))
                    {
                        // Двигаем камеру к дому
                        if (Camera.main)
                        {
                            Vector3 pos = netObj.transform.position;
                            pos.z = -10;
                            Camera.main.transform.position = pos;
                        }
                        // Можно закрыть окно или выделить дом
                    }
                });
            }
        }
    }
}