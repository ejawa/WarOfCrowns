using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WarOfCrowns.Units;
using System;

namespace WarOfCrowns.UI
{
    public class WorkerSlotUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI roleText; // Профессия или статус
        [SerializeField] private Image portraitImage;
        [SerializeField] private Button actionButton;
        [SerializeField] private TextMeshProUGUI buttonText; // "+" или "-"

        private Unit _unit;
        private Action<Unit> _callback;

        public void Setup(Unit unit, string actionLabel, Action<Unit> onClickAction)
        {
            _unit = unit;
            _callback = onClickAction;

            nameText.text = unit.unitName;
            roleText.text = unit.profession.ToString();

            if (unit.unitPortrait != null) portraitImage.sprite = unit.unitPortrait;

            buttonText.text = actionLabel;

            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(() => _callback(_unit));
        }
    }
}