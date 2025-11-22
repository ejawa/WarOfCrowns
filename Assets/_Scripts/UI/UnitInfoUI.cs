using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WarOfCrowns.Units;
using WarOfCrowns.Core;
using System.Collections.Generic;

namespace WarOfCrowns.UI
{
    public class UnitInfoUI : MonoBehaviour
    {
        [Header("Общие Элементы")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private Button closeButton;

        [Header("Одиночный Юнит")]
        [SerializeField] private GameObject singleUnitParent; // <-- Ссылка на контейнер (SingleUnit_Container)
        [SerializeField] private TextMeshProUGUI genderText;
        [SerializeField] private Slider healthBar;
        [SerializeField] private Slider satietyBar;

        // Слои портрета
        [SerializeField] private Image portraitBody;
        [SerializeField] private Image portraitClothes;
        [SerializeField] private Image portraitHead;
        [SerializeField] private Image portraitArmor;
        [SerializeField] private Image portraitWeapon;

        [Header("Группа Юнитов")]
        [SerializeField] private GameObject groupIconObj; // <-- Ссылка на картинку Group_Icon
        // [SerializeField] private Image groupIconImage; // Если захочешь менять картинку динамически

        private Unit _targetSingleUnit;

        private void Start()
        {
            if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);
        }

        private void Update()
        {
            if (_targetSingleUnit != null && gameObject.activeSelf)
            {
                UpdateDynamicStats();
            }
        }

        public void SetTarget(List<Unit> units)
        {
            if (units == null || units.Count == 0)
            {
                ClosePanel();
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

        private void ShowSingleUnitInfo(Unit unit)
        {
            // 1. ВКЛЮЧАЕМ режим одиночки
            if (singleUnitParent != null) singleUnitParent.SetActive(true);
            if (groupIconObj != null) groupIconObj.SetActive(false);

            // Включаем статы (если они не внутри контейнера, а отдельно)
            if (healthBar != null) healthBar.gameObject.SetActive(true);
            if (satietyBar != null) satietyBar.gameObject.SetActive(true);
            if (genderText != null) genderText.gameObject.SetActive(true);

            // Заполняем текст
            if (nameText != null) nameText.text = unit.unitName;
            if (genderText != null) genderText.text = unit.gender.ToString();

            // --- СБОРКА ПОРТРЕТА ---
            var visuals = unit.GetComponent<UnitVisuals>();
            if (visuals != null)
            {
                SetPortraitLayer(portraitBody, visuals.BodySprite);
                SetPortraitLayer(portraitClothes, visuals.ClothesSprite);
                SetPortraitLayer(portraitHead, visuals.HeadSprite);
                SetPortraitLayer(portraitArmor, visuals.ArmorSprite);
                SetPortraitLayer(portraitWeapon, visuals.WeaponSprite);
            }

            UpdateDynamicStats();
        }

        private void ShowGroupInfo(List<Unit> units)
        {
            // 1. ВКЛЮЧАЕМ режим группы
            if (singleUnitParent != null) singleUnitParent.SetActive(false);
            if (groupIconObj != null) groupIconObj.SetActive(true);

            // Скрываем лишние статы
            if (healthBar != null) healthBar.gameObject.SetActive(false);
            if (satietyBar != null) satietyBar.gameObject.SetActive(false);
            if (genderText != null) genderText.gameObject.SetActive(false);

            // Пишем количество
            if (nameText != null) nameText.text = $"Отряд: {units.Count} чел.";
        }

        private void SetPortraitLayer(Image img, Sprite sprite)
        {
            if (img == null) return;
            if (sprite != null)
            {
                img.sprite = sprite;
                img.enabled = true;
                img.color = Color.white;
            }
            else
            {
                img.enabled = false;
            }
        }

        private void UpdateDynamicStats()
        {
            if (_targetSingleUnit == null) return;

            if (satietyBar != null) satietyBar.value = _targetSingleUnit.satiety;
            var health = _targetSingleUnit.GetComponent<Health>();
            if (health != null && healthBar != null) healthBar.value = health.CurrentHealth;
        }

        private void ClosePanel()
        {
            if (_targetSingleUnit != null) _targetSingleUnit.Deselect();
            gameObject.SetActive(false);
            _targetSingleUnit = null;
        }
    }
}