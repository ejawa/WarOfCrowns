using UnityEngine;
using WarOfCrowns.Core;
using WarOfCrowns.Units;
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
        public int requiredAmount = 1;

        private List<Unit> _workers = new List<Unit>();
        private Building _building;
        private float _currentProgress;

        public float GetProgress() => _currentProgress;
        public void SetProgress(float value) { _currentProgress = value; }

        private void Awake()
        {
            _building = GetComponent<Building>();
        }

        public bool CanAddWorker() => _workers.Count < maxWorkers;

        public void AddWorker(Unit unit)
        {
            if (!CanAddWorker() || _workers.Contains(unit)) return;

            // ИСПРАВЛЕНО: Profession и UnitName
            if (unit.Profession != ProfessionType.Unemployed)
            {
                Debug.LogWarning($"{unit.UnitName} уже работает ({unit.Profession}).");
                return;
            }

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
            if (requiredAmount > 0)
            {
                if (kingdom.GetResourceAmount(requiredResource) >= requiredAmount)
                {
                    kingdom.AddResource(requiredResource, -requiredAmount);
                    kingdom.AddResource(producedResource, producedAmount);
                }
            }
            else
            {
                kingdom.AddResource(producedResource, producedAmount);
            }
        }
    }
}