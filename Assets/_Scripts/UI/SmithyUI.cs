using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WarOfCrowns.Buildings;
using System.Collections.Generic;

namespace WarOfCrowns.UI
{
    // Вспомогательный класс для настройки в Инспекторе
    [System.Serializable]
    public struct CategoryMapping
    {
        public string name;             // Просто для удобства (напиши "Мечи")
        public RecipeCategory category; // Какую категорию ловим
        public Transform container;     // В какую колонку кидаем
    }

    public class SmithyUI : MonoBehaviour
    {
        [Header("Тексты")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI recipeInfoText;

        [Header("Кнопки")]
        [SerializeField] private Button closeButton;
        [SerializeField] private GameObject recipeButtonPrefab;

        // --- ГЛАВНОЕ ИЗМЕНЕНИЕ ---
        [Header("Настройка Колонок")]
        [Tooltip("Здесь ты связываешь Категорию рецепта с Колонкой в UI")]
        public List<CategoryMapping> columnMappings;
        // -------------------------

        private Smithy _currentSmithy;

        public void Initialize(Smithy smithy)
        {
            _currentSmithy = smithy;
            titleText.text = smithy.GetComponent<Building>().buildingName;

            if (closeButton != null)
                closeButton.onClick.RemoveAllListeners(); // Важно очищать старые
            closeButton.onClick.AddListener(() => gameObject.SetActive(false));

            if (recipeInfoText != null) recipeInfoText.text = "Выберите рецепт...";

            GenerateButtons();
        }

        private void Update()
        {
            if (gameObject.activeSelf && _currentSmithy != null)
            {
                statusText.text = _currentSmithy.GetStatusText();
            }
        }

        private void GenerateButtons()
        {
            // 1. Очищаем все контейнеры, которые указаны в настройках
            foreach (var mapping in columnMappings)
            {
                if (mapping.container != null) ClearChildren(mapping.container);
            }

            // 2. Создаем кнопки
            for (int i = 0; i < _currentSmithy.recipes.Count; i++)
            {
                int index = i;
                CraftingRecipe recipe = _currentSmithy.recipes[i];

                // Ищем подходящую колонку для этой категории
                Transform targetContainer = GetContainerForCategory(recipe.category);

                if (targetContainer == null)
                {
                    Debug.LogWarning($"Не найдена колонка для категории: {recipe.category}");
                    continue;
                }

                // Создаем кнопку
                GameObject btnObj = Instantiate(recipeButtonPrefab, targetContainer);

                // Иконка
                Image iconImg = btnObj.transform.Find("Icon")?.GetComponent<Image>();
                if (iconImg != null) iconImg.sprite = recipe.icon;

                // Клик
                Button btn = btnObj.GetComponent<Button>();
                btn.onClick.AddListener(() =>
                {
                    _currentSmithy.EnqueueCraft(index);
                    ShowRecipeInfo(recipe);
                });
            }
        }

        // Ищем, куда положить кнопку
        private Transform GetContainerForCategory(RecipeCategory cat)
        {
            foreach (var map in columnMappings)
            {
                if (map.category == cat) return map.container;
            }
            return null;
        }

        private void ShowRecipeInfo(CraftingRecipe recipe)
        {
            if (recipeInfoText == null) return;

            string costStr = "";
            foreach (var cost in recipe.inputs)
                costStr += $"{cost.resourceType} x{cost.amount}\n";

            recipeInfoText.text = $"<size=120%>{recipe.recipeName}</size>\n\nЦена:\n{costStr}";
        }

        private void ClearChildren(Transform parent)
        {
            if (parent == null) return;
            foreach (Transform child in parent) Destroy(child.gameObject);
        }
    }
}