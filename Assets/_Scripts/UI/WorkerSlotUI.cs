using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WarOfCrowns.Units;
using System;

namespace WarOfCrowns.UI
{
    public class WorkerSlotUI : MonoBehaviour
    {
        [Header("Текст")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI roleText;
        [SerializeField] private TextMeshProUGUI buttonText;
        [SerializeField] private Button actionButton;

        [Header("Портрет (Слои)")]
        [SerializeField] private Image bodyImg;
        [SerializeField] private Image clothesImg;
        [SerializeField] private Image headImg;
        [SerializeField] private Image armorImg;
        [SerializeField] private Image weaponImg; // Опционально

        private Unit _unit;
        private Action<Unit> _callback;

        public void Setup(Unit unit, string actionLabel, Action<Unit> onClickAction)
        {
            _unit = unit;
            _callback = onClickAction;

            // 1. Тексты
            if (nameText != null) nameText.text = unit.unitName;
            if (roleText != null) roleText.text = unit.profession.ToString();
            if (buttonText != null) buttonText.text = actionLabel;

            // 2. Кнопка
            if (actionButton != null)
            {
                actionButton.onClick.RemoveAllListeners();
                actionButton.onClick.AddListener(() => _callback(_unit));
            }

            // 3. Сборка Портрета
            // Получаем визуал с самого юнита
            var visuals = unit.GetComponent<UnitVisuals>();
            if (visuals != null)
            {
                SetLayer(bodyImg, visuals.BodySprite);
                SetLayer(clothesImg, visuals.ClothesSprite);
                SetLayer(headImg, visuals.HeadSprite);
                SetLayer(armorImg, visuals.ArmorSprite);
                SetLayer(weaponImg, visuals.WeaponSprite);
            }
            else
            {
                // Если визуалов нет (ошибка?), выключаем картинки
                DisableAllLayers();
            }
        }

        private void SetLayer(Image img, Sprite sprite)
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

        private void DisableAllLayers()
        {
            if (bodyImg != null) bodyImg.enabled = false;
            if (clothesImg != null) clothesImg.enabled = false;
            if (headImg != null) headImg.enabled = false;
            if (armorImg != null) armorImg.enabled = false;
            if (weaponImg != null) weaponImg.enabled = false;
        }
    }
}