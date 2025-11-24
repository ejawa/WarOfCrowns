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
        [SerializeField] private GameObject recruitListPanel; // Панель, где лежит ScrollView
        [SerializeField] private Transform recruitListContent;
        [SerializeField] private GameObject recruitSlotPrefab;

        [Header("Панель Статистики (Инфо)")]
        [SerializeField] private GameObject statsPanel; // Панель справа или снизу
        [SerializeField] private TextMeshProUGUI statsNameText;
        [SerializeField] private TextMeshProUGUI statsHealthText;
        [SerializeField] private TextMeshProUGUI statsHungerText;
        // Можно добавить силу, ловкость и т.д.

        private Barracks _currentBarracks;
        private ResourceType _selectedWeaponType; // Какое оружие мы сейчас выдаем

        public void Initialize(Barracks barracks)
        {
            _currentBarracks = barracks;

            // Привязка кнопок
            if (closeButton != null) closeButton.onClick.AddListener(() => gameObject.SetActive(false));

            // Настраиваем типы оружия (можешь поменять на свои)
            swordsmanTabBtn.onClick.AddListener(() => OpenRecruitList(ResourceType.IronSword));
            archerTabBtn.onClick.AddListener(() => OpenRecruitList(ResourceType.WoodenBow)); // Или просто Bow
            spearmanTabBtn.onClick.AddListener(() => OpenRecruitList(ResourceType.IronSpear));

            // Скрываем список и статы в начале
            recruitListPanel.SetActive(false);
            statsPanel.SetActive(false);
        }

        private void OpenRecruitList(ResourceType weaponType)
        {
            _selectedWeaponType = weaponType;
            recruitListPanel.SetActive(true);

            // Очистка списка
            foreach (Transform child in recruitListContent) Destroy(child.gameObject);

            // Заполнение списка БЕЗРАБОТНЫМИ
            foreach (var unit in PopulationManager.Instance.AllUnits)
            {
                if (unit.profession == ProfessionType.Unemployed)
                {
                    GameObject slot = Instantiate(recruitSlotPrefab, recruitListContent);

                    // Настраиваем плашку и передаем методы для клика и ховера
                    slot.GetComponent<RecruitSlotUI>().Setup(
                        unit,
                        OnRecruitClicked,
                        OnUnitHoverEnter,
                        OnUnitHoverExit
                    );
                }
            }
        }

        // --- СОБЫТИЯ ---

        private void OnRecruitClicked(Unit unit)
        {
            // Отправляем приказ в казарму
            _currentBarracks.TrainSpecificUnit(unit, _selectedWeaponType);

            // Обновляем список (убираем нанятого)
            OpenRecruitList(_selectedWeaponType);
        }

        private void OnUnitHoverEnter(Unit unit)
        {
            statsPanel.SetActive(true);
            statsNameText.text = unit.unitName;

            // Получаем здоровье
            var health = unit.GetComponent<Health>();
            statsHealthText.text = $"HP: {health.CurrentHealth}";

            // Получаем сытость
            statsHungerText.text = $"Satiety: {(int)unit.satiety}%";
        }

        private void OnUnitHoverExit()
        {
            statsPanel.SetActive(false);
        }
    }
}