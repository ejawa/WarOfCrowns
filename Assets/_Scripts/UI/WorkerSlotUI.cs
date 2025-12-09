using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WarOfCrowns.Units;
using WarOfCrowns.Core; // Для WorldState
using System;

namespace WarOfCrowns.UI
{
    public class WorkerSlotUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI roleText;
        [SerializeField] private TextMeshProUGUI buttonText;
        [SerializeField] private Button actionButton;

        [Header("Визуал")]
        [SerializeField] private Image portraitImage; // Назначь сюда картинку головы

        private Unit _unit;
        private Action<Unit> _callback;

        public void Setup(Unit unit, string actionLabel, Action<Unit> onClickAction)
        {
            _unit = unit;
            _callback = onClickAction;

            if (nameText) nameText.text = unit.UnitName;
            if (roleText) roleText.text = unit.Profession.ToString();
            if (buttonText) buttonText.text = actionLabel;

            if (actionButton)
            {
                actionButton.onClick.RemoveAllListeners();
                actionButton.onClick.AddListener(() => _callback(_unit));
            }

            // Отрисовка портрета (только голова для простоты)
            if (portraitImage != null && WorldState.Instance && WorldState.Instance.AppearanceDB)
            {
                var db = WorldState.Instance.AppearanceDB;
                Sprite head = db.GetSpriteSetByName(unit.HeadName)?.idle;

                if (head != null)
                {
                    portraitImage.sprite = head;
                    portraitImage.enabled = true;
                }
                else
                {
                    portraitImage.enabled = false;
                }
            }
        }
    }
}