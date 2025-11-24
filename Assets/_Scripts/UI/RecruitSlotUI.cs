using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Нужен для наведения мыши (показ статов)
using TMPro;
using WarOfCrowns.Units;
using System;

namespace WarOfCrowns.UI
{
    public class RecruitSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Текст и Кнопка")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private Button recruitButton;

        [Header("Портрет (Слои)")]
        [SerializeField] private Image bodyImg;
        [SerializeField] private Image clothesImg;
        [SerializeField] private Image headImg;
        [SerializeField] private Image armorImg;
        [SerializeField] private Image weaponImg; // Можно оставить, вдруг у рекрута уже есть инструмент

        private Unit _unit;
        private Action<Unit> _onRecruitClick;
        private Action<Unit> _onHoverEnter;
        private Action _onHoverExit;

        public void Setup(Unit unit, Action<Unit> onClick, Action<Unit> onEnter, Action onExit)
        {
            _unit = unit;
            _onRecruitClick = onClick;
            _onHoverEnter = onEnter;
            _onHoverExit = onExit;

            // Настройка текста
            if (nameText != null) nameText.text = unit.unitName;

            // Настройка кнопки
            if (recruitButton != null)
            {
                recruitButton.onClick.RemoveAllListeners();
                recruitButton.onClick.AddListener(() => _onRecruitClick(_unit));
            }

            // --- СБОРКА ПОРТРЕТА (ИЗ СЛОЕВ) ---
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
                DisableAllLayers();
            }
        }

        // Вспомогательный метод для включения/выключения слоя
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

        // --- СОБЫТИЯ МЫШИ (Для показа статов справа) ---
        public void OnPointerEnter(PointerEventData eventData)
        {
            _onHoverEnter?.Invoke(_unit);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _onHoverExit?.Invoke();
        }
    }
}