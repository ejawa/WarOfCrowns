using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WarOfCrowns.Core;
using WarOfCrowns.Buildings; // Нужно для Prison и Building
using System.Collections.Generic;

namespace WarOfCrowns.UI
{
    public class LegitimacyUI : MonoBehaviour
    {
        public static LegitimacyUI Instance { get; private set; }

        [Header("--- ПАНЕЛЬ УКАЗОВ ---")]
        [SerializeField] private GameObject decreePanel;
        [SerializeField] private Transform decreeContainer;
        [SerializeField] private GameObject decreeSlotPrefab;
        [SerializeField] private Button openDecreeButton;
        [SerializeField] private Button closeDecreeButton;

        [Header("--- КРИЗИСЫ ---")]
        [Tooltip("Кнопка '!', которая появляется при проблемах")]
        [SerializeField] private Button alertButton;
        [SerializeField] private GameObject crisisPopup;
        [SerializeField] private TextMeshProUGUI crisisTitle;
        [SerializeField] private TextMeshProUGUI crisisDescription;
        [SerializeField] private Button option1Btn;
        [SerializeField] private Button option2Btn;
        [SerializeField] private Button option3Btn;
        [SerializeField] private TextMeshProUGUI opt1Text;
        [SerializeField] private TextMeshProUGUI opt2Text;
        [SerializeField] private TextMeshProUGUI opt3Text;

        private CrisisType _currentCrisis = CrisisType.None;

        private void Awake()
        {
            Instance = this;
            if (crisisPopup) crisisPopup.SetActive(false);
            if (decreePanel) decreePanel.SetActive(false);
            if (alertButton)
            {
                alertButton.gameObject.SetActive(false);
                alertButton.onClick.AddListener(OpenCrisisPopup);
            }

            if (openDecreeButton) openDecreeButton.onClick.AddListener(OpenDecreeMenu);
            if (closeDecreeButton) closeDecreeButton.onClick.AddListener(() => decreePanel.SetActive(false));
        }

        // --- ЛОГИКА УКАЗОВ ---
        public void OpenDecreeMenu()
        {
            decreePanel.SetActive(true);
            RefreshDecreeUI();
        }

        public void RefreshDecreeUI()
        {
            if (!decreePanel.activeSelf || DecreeManager.Instance == null) return;
            foreach (Transform child in decreeContainer) Destroy(child.gameObject);

            int myID = -1;
            if (Kingdom.PlayerKingdom != null) myID = Kingdom.PlayerKingdom.kingdomID.Value;

            if (DecreeManager.Instance.availableDecrees != null)
            {
                foreach (var decree in DecreeManager.Instance.availableDecrees)
                {
                    var slotObj = Instantiate(decreeSlotPrefab, decreeContainer);
                    var slotUI = slotObj.GetComponent<DecreeSlotUI>();
                    if (slotUI != null) slotUI.Setup(decree, myID);
                }
            }
        }

        // --- ЛОГИКА КРИЗИСОВ ---

        public void OnCrisisStarted(CrisisType type)
        {
            _currentCrisis = type;
            if (alertButton) alertButton.gameObject.SetActive(true);
        }

        public void OnCrisisResolved()
        {
            _currentCrisis = CrisisType.None;
            if (alertButton) alertButton.gameObject.SetActive(false);
            if (crisisPopup) crisisPopup.SetActive(false);
        }

        public void OpenCrisisPopup()
        {
            if (_currentCrisis == CrisisType.None) return;

            crisisPopup.SetActive(true);
            option1Btn.onClick.RemoveAllListeners();
            option2Btn.onClick.RemoveAllListeners();
            option3Btn.onClick.RemoveAllListeners();

            // Сбрасываем активность кнопок (включаем все)
            option1Btn.gameObject.SetActive(true);
            option1Btn.interactable = true;

            option2Btn.gameObject.SetActive(true);
            option2Btn.interactable = true;

            option3Btn.gameObject.SetActive(true);
            option3Btn.interactable = true;

            // Проверяем наличие тюрьмы для блокировки кнопок ареста
            bool canArrest = HasFreePrison();
            string arrestLabel = canArrest ? "Арестовать" : "Нет мест/тюрьмы";

            switch (_currentCrisis)
            {
                case CrisisType.Criticism:
                    crisisTitle.text = "КРИТИКА ВЛАСТИ";
                    crisisDescription.text = "Народ шепчется о вашей некомпетентности.";

                    SetButton(option1Btn, opt1Text, "Подкупить (50 зол)", 0);

                    // Кнопка ареста
                    SetButton(option2Btn, opt2Text, canArrest ? "Арестовать (Риск)" : arrestLabel, 1);
                    option2Btn.interactable = canArrest; // Блокируем, если нет тюрьмы

                    SetButton(option3Btn, opt3Text, "Убить (-10 Лег)", 2);
                    break;

                case CrisisType.Disobedience:
                    crisisTitle.text = "НЕПОВИНОВЕНИЕ";
                    crisisDescription.text = "Оппозиция отказывается подчиняться.";

                    SetButton(option1Btn, opt1Text, "Подкупить (200 зол, 50 дер)", 0);

                    // Кнопка ареста
                    SetButton(option2Btn, opt2Text, canArrest ? "Арестовать лидеров" : arrestLabel, 1);
                    option2Btn.interactable = canArrest;

                    SetButton(option3Btn, opt3Text, "Силовой разгон (-20 Лег)", 2);
                    break;

                case CrisisType.Riots:
                    crisisTitle.text = "ВООРУЖЕННЫЙ БУНТ!";
                    crisisDescription.text = "Толпа взялась за оружие! Они атакуют наши здания!";

                    SetButton(option1Btn, opt1Text, "Подкупить (-300 зол)", 0);

                    // Кнопка ареста
                    SetButton(option2Btn, opt2Text, canArrest ? "АРЕСТОВАТЬ ВСЕХ" : arrestLabel, 1);
                    option2Btn.interactable = canArrest;

                    option3Btn.gameObject.SetActive(false);
                    break;
            }
        }

        private void SetButton(Button btn, TextMeshProUGUI txt, string label, int optionIndex)
        {
            txt.text = label;
            // Не добавляем листенер, если кнопка уже выключена (interactable = false), 
            // хотя Unity сама блокирует клики, но для чистоты кода это делает SetButton универсальным
            btn.onClick.AddListener(() => {
                if (CrisisManager.Instance)
                    CrisisManager.Instance.ResolveCrisisServerRpc(optionIndex);

                crisisPopup.SetActive(false);
            });
        }

        // --- ПРОВЕРКА ТЮРЬМЫ НА КЛИЕНТЕ ---
        private bool HasFreePrison()
        {
            if (Kingdom.PlayerKingdom == null) return false;
            int myID = Kingdom.PlayerKingdom.kingdomID.Value;

            // Находим все тюрьмы в сцене
            var prisons = FindObjectsOfType<Prison>();
            foreach (var p in prisons)
            {
                var building = p.GetComponent<Building>();
                // Проверяем: 1. Это наша тюрьма? 2. Есть ли там место?
                if (building != null && building.ownerKingdomID.Value == myID && p.HasSpace())
                {
                    return true;
                }
            }
            return false;
        }
    }
}