using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WarOfCrowns.Buildings;

namespace WarOfCrowns.UI
{
    public class DefenseTowerUI : MonoBehaviour
    {
        [Header("Элементы")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI garrisonText; // Текст "Гарнизон: 1/3"
        [SerializeField] private Button ejectButton;           // Кнопка "Выгнать всех"
        [SerializeField] private Button closeButton;

        private DefenseTower _currentTower;

        public void Initialize(DefenseTower tower)
        {
            _currentTower = tower;

            // Настраиваем заголовок
            if (titleText != null)
                titleText.text = tower.GetComponent<Building>().buildingName;

            // Привязываем кнопки
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(() => gameObject.SetActive(false));
            }

            if (ejectButton != null)
            {
                ejectButton.onClick.RemoveAllListeners();
                ejectButton.onClick.AddListener(OnEjectClicked);
            }

            UpdateUI();
        }

        private void Update()
        {
            // Обновляем текст в реальном времени (вдруг кто-то зашел или вышел)
            if (gameObject.activeSelf && _currentTower != null)
            {
                UpdateUI();
            }
        }

        private void UpdateUI()
        {
            if (garrisonText != null)
            {
                // Для доступа к приватным полям DefenseTower нам, возможно, придется сделать их публичными.
                // Но лучше добавить публичные методы в DefenseTower.
                // Пока предположим, что мы добавим метод GetGarrisonCount() в DefenseTower.

                int current = _currentTower.GetGarrisonCount();
                int max = _currentTower.maxGarrison;
                garrisonText.text = $"Гарнизон: {current} / {max}";
            }
        }

        private void OnEjectClicked()
        {
            if (_currentTower != null)
            {
                _currentTower.EjectAll();
            }
        }
    }
}