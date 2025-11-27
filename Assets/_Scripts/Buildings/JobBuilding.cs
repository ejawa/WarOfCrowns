using UnityEngine;
using WarOfCrowns.Core;
using WarOfCrowns.Units; // Для списков рабочих
using System.Collections.Generic;

namespace WarOfCrowns.Buildings
{
    [RequireComponent(typeof(Building))]
    public class JobBuilding : MonoBehaviour
    {
        [Header("Настройки Работы")]
        public ProfessionType requiredProfession;
        public int maxWorkers = 2;

        [Header("Производство")]
        public ResourceType producedResource;
        public int producedAmount = 1;
        public float productionTime = 5f;

        [Header("Потребление")]
        public ResourceType requiredResource;
        public int requiredAmount = 1; // Если 0 - значит здание добывающее (Ферма)

        private List<Unit> _workers = new List<Unit>();
        private Building _building;
        private float _currentProgress;
        public float GetProgress() => _currentProgress;
        public void SetProgress(float value) { _currentProgress = value; }

        private void Awake()
        {
            _building = GetComponent<Building>();
        }

        // --- Управление Персоналом ---
        public bool CanAddWorker() => _workers.Count < maxWorkers;

        public void AddWorker(Unit unit)
        {
            if (!CanAddWorker() || _workers.Contains(unit)) return;

            // --- ИСПРАВЛЕНО: Принимаем на работу только безработных ---
            if (unit.profession != ProfessionType.Unemployed)
            {
                Debug.LogWarning($"{unit.unitName} уже работает ({unit.profession}), не может быть назначен на {requiredProfession}.");
                return;
            }
            // -----------------------------------------------------------

            _workers.Add(unit);
            unit.SetProfession(requiredProfession);

            if (unit.TryGetComponent<UnitWorker>(out var workerAI))
            {
                workerAI.SetTarget(this);
            }
        }

        public void RemoveWorker(Unit unit)
        {
            if (_workers.Contains(unit))
            {
                _workers.Remove(unit);
                unit.SetProfession(ProfessionType.Unemployed);

                if (unit.TryGetComponent<UnitWorker>(out var workerAI)) workerAI.StopWorking();
                if (unit.TryGetComponent<UnitAI>(out var ai)) ai.SetState(UnitState.Idling);
            }
        }

        public List<Unit> GetWorkers() => _workers;

        // --- Производство ---
        public void AddWorkProgress(float amount)
        {
            if (_building.OwningKingdom == null) return;

            _currentProgress += amount;
            if (_currentProgress >= productionTime)
            {
                TryProduce();
                _currentProgress = 0;
            }
        }

        private void TryProduce()
        {
            Kingdom kingdom = _building.OwningKingdom;

            // ВАРИАНТ 1: Конвертер (Пекарня, Мельница)
            if (requiredAmount > 0)
            {
                if (kingdom.GetResourceAmount(requiredResource) >= requiredAmount)
                {
                    // 1. Забираем сырье
                    kingdom.AddResource(requiredResource, -requiredAmount);
                    Debug.Log($"{gameObject.name}: Consumed {requiredAmount} {requiredResource}");

                    // 2. Производим продукт
                    kingdom.AddResource(producedResource, producedAmount);
                    Debug.Log($"{gameObject.name}: PRODUCED {producedAmount} {producedResource}!");

                    // 3. Конвертируем в сытость (Если это Хлеб)
                    // FoodConverter сам увидит это через событие Kingdom.OnResourceChanged? 
                    // НЕТ! Мы договорились вызывать его вручную для надежности.
                    
                }
                else
                {
                    // Debug.Log($"{gameObject.name}: Not enough {requiredResource}!");
                }
            }
            // ВАРИАНТ 2: Генератор (Ферма)
            else
            {
                kingdom.AddResource(producedResource, producedAmount);
                Debug.Log($"{gameObject.name}: Produced {producedAmount} {producedResource}");

                
            }
        }
    }
}