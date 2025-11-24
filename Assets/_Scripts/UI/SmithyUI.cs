using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WarOfCrowns.Buildings;
using System.Collections.Generic;

namespace WarOfCrowns.UI
{
    public class SmithyUI : MonoBehaviour
    {
        [Header("Элементы")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Button closeButton;
        [SerializeField] private Transform buttonsParent;
        [SerializeField] private GameObject recipeButtonPrefab; // Кнопка с текстом

        private Smithy _currentSmithy;

        public void Initialize(Smithy smithy)
        {
            _currentSmithy = smithy;
            titleText.text = smithy.GetComponent<Building>().buildingName;

            if (closeButton != null)
                closeButton.onClick.AddListener(() => gameObject.SetActive(false));

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
            foreach (Transform child in buttonsParent) Destroy(child.gameObject);

            for (int i = 0; i < _currentSmithy.recipes.Count; i++)
            {
                int index = i; // Важно для замыкания в лямбде
                CraftingRecipe recipe = _currentSmithy.recipes[i];

                GameObject btnObj = Instantiate(recipeButtonPrefab, buttonsParent);

                // Настройка текста кнопки (Название + Стоимость)
                TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                string costStr = "";
                foreach (var cost in recipe.inputs) costStr += $"{cost.resourceType} x{cost.amount} ";
                btnText.text = $"{recipe.recipeName}\n<size=60%>{costStr}</size>";

                // Настройка клика
                btnObj.GetComponent<Button>().onClick.AddListener(() => _currentSmithy.EnqueueCraft(index));
            }
        }
    }
}