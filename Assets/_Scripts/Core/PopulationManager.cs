using System;
using UnityEngine;

namespace WarOfCrowns.Core
{
    public class PopulationManager : MonoBehaviour
    {
        public static PopulationManager Instance { get; private set; }

        public int CurrentPopulation { get; private set; }
        public int PopulationCap { get; private set; }

        public static event Action OnPopulationChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) Destroy(gameObject);
            else Instance = this;
        }

        public void SetInitialPopulation(int current, int cap)
        {
            CurrentPopulation = current;
            PopulationCap = cap;
            OnPopulationChanged?.Invoke();
        }

        public void AddUnit()
        {
            CurrentPopulation++;
            OnPopulationChanged?.Invoke();
        }

        public void RemoveUnit()
        {
            CurrentPopulation--;
            OnPopulationChanged?.Invoke();
        }

        public void AddPopulationCap(int amount)
        {
            PopulationCap += amount;
            OnPopulationChanged?.Invoke(); // <-- Убедись, что эта строка здесь есть!
        }

        public bool IsCapReached()
        {
            return CurrentPopulation >= PopulationCap;
        }
    }
}