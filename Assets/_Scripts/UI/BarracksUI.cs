using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WarOfCrowns.Buildings;
using WarOfCrowns.Core;
using WarOfCrowns.Units;
using System.Collections.Generic;

namespace WarOfCrowns.UI
{
    public class BarracksUI : MonoBehaviour
    {
        [Header("Меню Категорий")]
        [SerializeField] private Button swordsmanTabBtn;
        [SerializeField] private Button archerTabBtn;
        [SerializeField] private Button spearmanTabBtn;
        [SerializeField] private Button closeButton;

        [Header("Список Рекрутов")]
        [SerializeField] private GameObject recruitListPanel;
        [SerializeField] private Transform recruitListContent;
        [SerializeField] private GameObject recruitSlotPrefab;

        [Header("Панель Статистики")]
        [SerializeField] private GameObject statsPanel;
        [SerializeField] private TextMeshProUGUI statsNameText;
        [SerializeField] private TextMeshProUGUI statsHealthText;
        [SerializeField] private TextMeshProUGUI statsHungerText;

        private Barracks _currentBarracks;
        private ResourceType _selectedWeaponType;

        public void Initialize(Barracks barracks)
        {
            _currentBarracks = barracks;

            if (closeButton != null) closeButton.onClick.AddListener(() => gameObject.SetActive(false));

            // Настройка кнопок (пример)
            swordsmanTabBtn.onClick.AddListener(() => OpenRecruitList(ResourceType.IronSword));
            archerTabBtn.onClick.AddListener(() => OpenRecruitList(ResourceType.WoodenBow));
            spearmanTabBtn.onClick.AddListener(() => OpenRecruitList(ResourceType.IronSpear));

            recruitListPanel.SetActive(false);
            statsPanel.SetActive(false);
        }

        private void OpenRecruitList(ResourceType weaponType)
        {
            _selectedWeaponType = weaponType;
            recruitListPanel.SetActive(true);

            foreach (Transform child in recruitListContent) Destroy(child.gameObject);

            // Заполняем список безработными
            if (PopulationManager.Instance != null)
            {
                foreach (var unit in PopulationManager.Instance.AllUnits)
                {
                    // --- ИСПРАВЛЕНО: Profession с большой буквы ---
                    if (unit.Profession == ProfessionType.Unemployed)
                    {
                        GameObject slot = Instantiate(recruitSlotPrefab, recruitListContent);
                        slot.GetComponent<RecruitSlotUI>().Setup(
                            unit,
                            OnRecruitClicked,
                            OnUnitHoverEnter,
                            OnUnitHoverExit
                        );
                    }
                }
            }
        }

        private void OnRecruitClicked(Unit unit)
        {
            _currentBarracks.TrainSpecificUnit(unit, _selectedWeaponType);
            OpenRecruitList(_selectedWeaponType); // Обновить список
        }

        private void OnUnitHoverEnter(Unit unit)
        {
            statsPanel.SetActive(true);

            // --- ИСПРАВЛЕНО: UnitName с большой буквы ---
            statsNameText.text = unit.UnitName;

            var health = unit.GetComponent<Health>();
            statsHealthText.text = $"HP: {health.CurrentHealth}";
            statsHungerText.text = $"Satiety: {(int)unit.satiety}%";
        }

        private void OnUnitHoverExit()
        {
            statsPanel.SetActive(false);
        }
    }
}