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
        [Header("Элементы UI")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI genderText;
        [SerializeField] private Slider healthBar;
        [SerializeField] private Slider satietyBar;
        [SerializeField] private Image portraitImage;
        [SerializeField] private Button closeButton;

        [Header("Группа")]
        [SerializeField] private Sprite groupIcon;

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
            // Проверка на null и пустой список
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
            // Безопасное включение элементов
            if (healthBar != null) healthBar.gameObject.SetActive(true);
            if (satietyBar != null) satietyBar.gameObject.SetActive(true);
            if (portraitImage != null) portraitImage.gameObject.SetActive(true);
            if (genderText != null) genderText.gameObject.SetActive(true);

            // Заполнение данных
            if (nameText != null) nameText.text = unit.unitName;
            if (genderText != null) genderText.text = unit.gender.ToString();

            if (portraitImage != null)
            {
                if (unit.unitPortrait != null)
                {
                    portraitImage.sprite = unit.unitPortrait;
                    portraitImage.enabled = true;
                }
                else
                {
                    portraitImage.enabled = false;
                }
            }

            UpdateDynamicStats();
        }

        private void ShowGroupInfo(List<Unit> units)
        {
            if (healthBar != null) healthBar.gameObject.SetActive(false);
            if (satietyBar != null) satietyBar.gameObject.SetActive(false);
            if (genderText != null) genderText.gameObject.SetActive(false);

            if (nameText != null) nameText.text = $"Выбрано: {units.Count}";

            if (portraitImage != null)
            {
                if (groupIcon != null)
                {
                    portraitImage.sprite = groupIcon;
                    portraitImage.enabled = true;
                }
                else
                {
                    portraitImage.enabled = false;
                }
            }
        }

        private void UpdateDynamicStats()
        {
            if (_targetSingleUnit == null) return;

            if (satietyBar != null)
                satietyBar.value = _targetSingleUnit.satiety;

            var health = _targetSingleUnit.GetComponent<Health>();
            if (health != null && healthBar != null)
                healthBar.value = health.CurrentHealth;
        }

        private void ClosePanel()
        {
            if (_targetSingleUnit != null) _targetSingleUnit.Deselect();
            gameObject.SetActive(false);
            _targetSingleUnit = null;
        }
    }
}