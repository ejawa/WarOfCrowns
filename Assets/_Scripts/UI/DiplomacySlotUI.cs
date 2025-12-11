using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WarOfCrowns.Core;

namespace WarOfCrowns.UI
{
    public class DiplomacySlotUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Button actionButton;
        [SerializeField] private TextMeshProUGUI buttonText;

        private Kingdom _targetKingdom; // Ссылка на королевство в этом слоте

        public void Setup(Kingdom targetKingdom)
        {
            _targetKingdom = targetKingdom;

            // --- ИСПРАВЛЕНИЕ: Подписка на изменение имени ---
            // Устанавливаем текущее имя
            UpdateName(default, targetKingdom.kingdomName.Value);

            // Подписываемся, чтобы обновлялось само
            targetKingdom.kingdomName.OnValueChanged += UpdateName;
            // -----------------------------------------------

            UpdateState();
        }

        private void OnDestroy()
        {
            // Отписываемся, чтобы не было ошибок
            if (_targetKingdom != null)
                _targetKingdom.kingdomName.OnValueChanged -= UpdateName;
        }

        // Этот метод вызовется сам, когда переменная nameNet изменится
        private void UpdateName(Unity.Collections.FixedString64Bytes oldName, Unity.Collections.FixedString64Bytes newName)
        {
            if (nameText) nameText.text = newName.ToString();
        }

        private void Update()
        {
            if (gameObject.activeSelf) UpdateState();
        }

        private void UpdateState()
        {
            if (Kingdom.PlayerKingdom == null || _targetKingdom == null) return;

            int targetID = _targetKingdom.kingdomID.Value;
            Kingdom myK = Kingdom.PlayerKingdom;

            bool isEnemy = myK.enemiesList.Contains(targetID);
            bool theyOfferedPeace = myK.incomingPeaceOffers.Contains(targetID);
            bool weOfferedPeace = _targetKingdom.incomingPeaceOffers.Contains(myK.kingdomID.Value);

            actionButton.onClick.RemoveAllListeners();

            if (!isEnemy)
            {
                statusText.text = "<color=green>Мир</color>";
                buttonText.text = "Объявить войну (500)";
                actionButton.interactable = true;
                actionButton.image.color = Color.red; // Красная кнопка войны
                actionButton.onClick.AddListener(() => DiplomacyManager.Instance.RequestDeclareWar(targetID));
            }
            else
            {
                statusText.text = "<color=red>ВОЙНА</color>";

                if (theyOfferedPeace)
                {
                    buttonText.text = "Принять мир!";
                    actionButton.interactable = true;
                    actionButton.image.color = Color.green;
                    actionButton.onClick.AddListener(() => DiplomacyManager.Instance.RequestAcceptPeace(targetID));
                }
                else if (weOfferedPeace)
                {
                    buttonText.text = "Предложение отправлено...";
                    actionButton.interactable = false;
                    actionButton.image.color = Color.yellow;
                }
                else
                {
                    buttonText.text = "Предложить мир";
                    actionButton.interactable = true;
                    actionButton.image.color = Color.white;
                    actionButton.onClick.AddListener(() => DiplomacyManager.Instance.RequestOfferPeace(targetID));
                }
            }
        }
    }
}