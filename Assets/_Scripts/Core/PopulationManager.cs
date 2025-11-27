using System;
using System.Collections.Generic;
using UnityEngine;
using WarOfCrowns.Units;

namespace WarOfCrowns.Core
{
    public class PopulationManager : MonoBehaviour
    {
        public static PopulationManager Instance { get; private set; }

        public List<Unit> AllUnits { get; private set; } = new List<Unit>();

        public int CurrentPopulation => AllUnits.Count;
        public int PopulationCap { get; private set; }

        public static event Action OnPopulationChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // --- ИСПРАВЛЕНО: Больше не очищаем список юнитов здесь ---
        public void SetInitialPopulation(int current, int cap)
        {
            PopulationCap = cap;
            // AllUnits.Clear(); <--- УДАЛИЛИ ЭТО, ЧТОБЫ НЕ СТИРАТЬ СПАВНЯЩИХСЯ ЮНИТОВ
            OnPopulationChanged?.Invoke();
        }
        // ---------------------------------------------------------

        public void AddUnit(Unit unit)
        {
            if (!AllUnits.Contains(unit))
            {
                AllUnits.Add(unit);
                OnPopulationChanged?.Invoke();
            }
        }
        public void ResetRegistry()
        {
            AllUnits.Clear();
            // PopulationCap можно не сбрасывать, он пересчитается зданиями
            OnPopulationChanged?.Invoke();
        }
        public void RemoveUnit(Unit unit)
        {
            if (AllUnits.Contains(unit))
            {
                AllUnits.Remove(unit);
                OnPopulationChanged?.Invoke();
            }
        }

        public void AddPopulationCap(int amount)
        {
            PopulationCap += amount;
            OnPopulationChanged?.Invoke();
        }

        public bool IsCapReached()
        {
            return CurrentPopulation >= PopulationCap;
        }

        public void ClearAllUnits()
        {
            AllUnits.Clear();
            OnPopulationChanged?.Invoke();
        }
    }
}