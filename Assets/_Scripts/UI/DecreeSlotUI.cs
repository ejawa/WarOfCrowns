using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WarOfCrowns.Data;
using WarOfCrowns.Core;

namespace WarOfCrowns.UI
{
    public class DecreeSlotUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private Image iconImage;
        [SerializeField] private Button activateButton;
        [SerializeField] private GameObject cooldownOverlay; // Полупрозрачная панель поверх
        [SerializeField] private TextMeshProUGUI cooldownText;

        private DecreeData _data;
        private int _myKingdomID;

        public void Setup(DecreeData data, int kingdomID)
        {
            _data = data;
            _myKingdomID = kingdomID;

            if (titleText) titleText.text = data.title;
            if (iconImage) iconImage.sprite = data.icon;

            // Формируем текст цены
            string costStr = "";
            foreach (var cost in data.costs)
            {
                costStr += $"{cost.amount} {cost.resourceType}\n";
            }
            if (costText) costText.text = costStr;

            activateButton.onClick.RemoveAllListeners();
            activateButton.onClick.AddListener(() => {
                DecreeManager.Instance.RequestEnactDecree(data.id);
            });
        }

        private void Update()
        {
            if (DecreeManager.Instance == null || _data == null) return;

            float remainingTime = DecreeManager.Instance.GetRemainingCooldown(_myKingdomID, _data.id);

            if (remainingTime > 0)
            {
                activateButton.interactable = false;
                if (cooldownOverlay) cooldownOverlay.SetActive(true);
                if (cooldownText) cooldownText.text = $"{Mathf.CeilToInt(remainingTime)}с";
            }
            else
            {
                activateButton.interactable = true;
                if (cooldownOverlay) cooldownOverlay.SetActive(false);
                if (cooldownText) cooldownText.text = "";
            }
        }
    }
}