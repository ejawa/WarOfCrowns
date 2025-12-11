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
        [SerializeField] private GameObject kingdomSlotPrefab; // Сюда префаб DiplomacySlot
        [SerializeField] private Button closeButton;

        private void Start()
        {
            if (closeButton) closeButton.onClick.AddListener(() => gameObject.SetActive(false));
            if (renameButton) renameButton.onClick.AddListener(OnRenameClicked);
        }

        private void OnEnable()
        {
            if (Kingdom.PlayerKingdom != null)
                nameInputField.text = Kingdom.PlayerKingdom.kingdomName.Value.ToString();

            RefreshList();
        }

        private void OnRenameClicked()
        {
            if (!string.IsNullOrEmpty(nameInputField.text))
                DiplomacyManager.Instance.RequestRename(nameInputField.text);
        }

        public void RefreshList()
        {
            if (listContainer == null || Kingdom.PlayerKingdom == null) return;

            foreach (Transform child in listContainer) Destroy(child.gameObject);

            int myID = Kingdom.PlayerKingdom.kingdomID.Value;

            foreach (var kvp in Kingdom.ActiveKingdoms)
            {
                Kingdom k = kvp.Value;
                if (k.kingdomID.Value == myID) continue; // Себя не показываем

                GameObject slot = Instantiate(kingdomSlotPrefab, listContainer);
                var ui = slot.GetComponent<DiplomacySlotUI>();
                if (ui) ui.Setup(k);
            }
        }
    }
}