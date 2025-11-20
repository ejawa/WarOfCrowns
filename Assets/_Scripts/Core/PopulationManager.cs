using System;
using System.Collections.Generic;
using UnityEngine;
using WarOfCrowns.Units; // Чтобы видеть класс Unit

namespace WarOfCrowns.Core
{
    public class PopulationManager : MonoBehaviour
    {
        public static PopulationManager Instance { get; private set; }

        // ТЕПЕРЬ ЭТО СПИСОК, А НЕ ПРОСТО ЧИСЛО
        public List<Unit> AllUnits { get; private set; } = new List<Unit>();

        // Свойства для удобства (чтобы не ломать старый код)
        public int CurrentPopulation => AllUnits.Count;
        public int PopulationCap { get; private set; }

        public static event Action OnPopulationChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            // При старте список пуст, юниты сами добавятся в Start()
        }

        public void SetInitialPopulation(int current, int cap)
        {
            // current нам больше не нужен, мы считаем по головам
            PopulationCap = cap;
            OnPopulationChanged?.Invoke();
        }

        // Теперь мы добавляем самого Юнита в список
        public void AddUnit(Unit unit)
        {
            if (!AllUnits.Contains(unit))
            {
                AllUnits.Add(unit);
                OnPopulationChanged?.Invoke();
            }
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

        // Метод для "Геноцида" (нужен при загрузке сохранения, чтобы удалить старых юнитов)
        public void ClearAllUnits()
        {
            // Идем с конца, чтобы безопасно удалять
            for (int i = AllUnits.Count - 1; i >= 0; i--)
            {
                if (AllUnits[i] != null)
                {
                    Destroy(AllUnits[i].gameObject);
                }
            }
            AllUnits.Clear();
            OnPopulationChanged?.Invoke();
        }
    }
}