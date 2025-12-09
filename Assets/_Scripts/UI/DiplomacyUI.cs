using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WarOfCrowns.Core;
using System.Collections.Generic;

namespace WarOfCrowns.UI
{
    public class DiplomacyUI : MonoBehaviour
    {
        [Header("Мое Королевство")]
        [SerializeField] private TMP_InputField nameInputField;
        [SerializeField] private Button renameButton;

        [Header("Список")]
        [SerializeField] private Transform listContainer;
        [SerializeField] private GameObject kingdomSlotPrefab;
        [SerializeField] private Button closeButton;
        [SerializeField] private int warCost = 500;

        private void Start()
        {
            if (closeButton) closeButton.onClick.AddListener(() => gameObject.SetActive(false));
            if (renameButton) renameButton.onClick.AddListener(OnRenameClicked);
        }

        private void OnEnable()
        {
            RefreshList();
            if (Kingdom.PlayerKingdom != null)
                nameInputField.text = Kingdom.PlayerKingdom.kingdomName.Value.ToString();
        }

        private void OnRenameClicked()
        {
            if (!string.IsNullOrEmpty(nameInputField.text))
                DiplomacyManager.Instance.RequestRename(nameInputField.text);
        }

        public void RefreshList()
        {
            foreach (Transform child in listContainer) Destroy(child.gameObject);

            // ЗАЩИТА ОТ NULL
            if (Kingdom.PlayerKingdom == null) return;

            int myID = Kingdom.PlayerKingdom.kingdomID.Value;
            var allKingdoms = FindObjectsOfType<Kingdom>();

            foreach (var k in allKingdoms)
            {
                int kID = k.kingdomID.Value;
                if (kID == myID || kID == -1) continue;

                GameObject slot = Instantiate(kingdomSlotPrefab, listContainer);
                slot.transform.Find("NameText").GetComponent<TextMeshProUGUI>().text = k.kingdomName.Value.ToString();

                var statusText = slot.transform.Find("StatusText").GetComponent<TextMeshProUGUI>();
                var warBtn = slot.transform.Find("WarButton").GetComponent<Button>();
                var btnText = warBtn.GetComponentInChildren<TextMeshProUGUI>();

                bool isEnemy = Kingdom.PlayerKingdom.IsAtWarWith(kID);

                if (isEnemy)
                {
                    statusText.text = "<color=red>ВОЙНА</color>";
                    warBtn.interactable = false;
                    btnText.text = "Воюем";
                }
                else
                {
                    statusText.text = "<color=green>Мир</color>";
                    warBtn.interactable = true;
                    btnText.text = $"Война ({warCost})";
                    warBtn.onClick.AddListener(() => {
                        DiplomacyManager.Instance.RequestDeclareWar(kID);
                        Invoke(nameof(RefreshList), 0.2f);
                    });
                }
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(listContainer.GetComponent<RectTransform>());
        }
    }
}