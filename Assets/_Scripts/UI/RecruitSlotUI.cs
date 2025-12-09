using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using WarOfCrowns.Units;
using WarOfCrowns.Core;
using System;

namespace WarOfCrowns.UI
{
    public class RecruitSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private Button recruitButton;

        [Header("Визуал")]
        [SerializeField] private Image portraitHead; // <-- Добавь Image в префаб!

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

            if (nameText) nameText.text = unit.UnitName;

            if (recruitButton)
            {
                recruitButton.onClick.RemoveAllListeners();
                recruitButton.onClick.AddListener(() => _onRecruitClick(_unit));
            }

            // Отрисовка головы
            if (portraitHead != null && WorldState.Instance && WorldState.Instance.AppearanceDB)
            {
                var db = WorldState.Instance.AppearanceDB;
                // Используем поиск по индексу
                Sprite headSprite = db.GetHeadByIndex(unit.headIndex.Value, unit.UnitGender)?.idle;

                if (headSprite != null)
                {
                    portraitHead.sprite = headSprite;
                    portraitHead.enabled = true;
                }
                else
                {
                    portraitHead.enabled = false;
                }
            }
        }

        public void OnPointerEnter(PointerEventData eventData) => _onHoverEnter?.Invoke(_unit);
        public void OnPointerExit(PointerEventData eventData) => _onHoverExit?.Invoke();
    }
}