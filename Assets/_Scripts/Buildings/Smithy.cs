using UnityEngine;
using System.Collections.Generic;
using WarOfCrowns.Core;

namespace WarOfCrowns.Buildings
{
    [RequireComponent(typeof(Building))]
    public class Smithy : MonoBehaviour
    {
        [Header("Список Рецептов")]
        public List<CraftingRecipe> recipes;

        private Building _building;
        private Queue<CraftingRecipe> _craftingQueue = new Queue<CraftingRecipe>();
        private CraftingRecipe _currentRecipe;
        private float _timer;

        private void Awake()
        {
            _building = GetComponent<Building>();
        }

        private void Update()
        {
            if (_building.OwningKingdom == null) return;

            // Если ничего не крафтим, берем следующий заказ из очереди
            if (_currentRecipe == null && _craftingQueue.Count > 0)
            {
                _currentRecipe = _craftingQueue.Dequeue();
                _timer = _currentRecipe.craftTime;
                Debug.Log($"{name}: Started crafting {_currentRecipe.recipeName}");
            }

            // Процесс крафта
            if (_currentRecipe != null)
            {
                _timer -= Time.deltaTime;
                if (_timer <= 0)
                {
                    FinishCraft();
                }
            }
        }

        // Метод для UI: Добавить заказ
        public void EnqueueCraft(int recipeIndex)
        {
            if (recipeIndex < 0 || recipeIndex >= recipes.Count) return;

            CraftingRecipe recipe = recipes[recipeIndex];
            Kingdom kingdom = _building.OwningKingdom;

            // СРАЗУ проверяем и забираем ресурсы (оплата вперед)
            if (kingdom.TrySpendResources(recipe.inputs))
            {
                _craftingQueue.Enqueue(recipe);
                Debug.Log($"{name}: Added {recipe.recipeName} to queue. Queue size: {_craftingQueue.Count}");
            }
            else
            {
                Debug.Log($"{name}: Not enough resources for {recipe.recipeName}!");
            }
        }

        private void FinishCraft()
        {
            // Добавляем готовый предмет в королевство
            _building.OwningKingdom.AddResource(_currentRecipe.outputItem, _currentRecipe.outputAmount);
            Debug.Log($"{name}: Finished {_currentRecipe.recipeName}!");

            _currentRecipe = null; // Готово, освобождаем слот
        }

        // Для UI: Получить инфо о текущем статусе
        public string GetStatusText()
        {
            if (_currentRecipe != null)
                return $"Crafting: {_currentRecipe.recipeName} ({_timer:F1}s)";
            else if (_craftingQueue.Count > 0)
                return $"Queued: {_craftingQueue.Count}";
            else
                return "Idle";
        }
        // --- СОХРАНЕНИЕ ---
        public int GetCurrentRecipeIndex()
        {
            if (_currentRecipe != null)
            {
                return recipes.IndexOf(_currentRecipe);
            }
            return -1;
        }

        public float GetCurrentTimer()
        {
            return _timer;
        }

        // --- ЗАГРУЗКА ---
        public void LoadState(int recipeIndex, float timeRemaining)
        {
            if (recipeIndex >= 0 && recipeIndex < recipes.Count)
            {
                _currentRecipe = recipes[recipeIndex];
                _timer = timeRemaining;
                // Очередь мы пока не сохраняем, но текущий предмет восстановится
            }
        }
    }
}