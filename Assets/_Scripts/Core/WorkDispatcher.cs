using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
using WarOfCrowns.Units;
using WarOfCrowns.Buildings;

namespace WarOfCrowns.Core
{
    public class WorkDispatcher : NetworkBehaviour
    {
        public static WorkDispatcher Instance;

        private void Awake() { Instance = this; }

        // Вызывается из BuildManager, когда ставится фундамент
        public void AssignWorkersToSite(ConstructionSite site, int kingdomID)
        {
            if (!IsServer || PopulationManager.Instance == null) return;

            // 1. Ищем свободных юнитов (Idle)
            // Исключаем Солдат, так как они не работают
            var availableUnits = PopulationManager.Instance.AllUnits
                .Where(u => u.ownerKingdomID.Value == kingdomID
                       && u.Profession != ProfessionType.Soldier
                       && u.TryGetComponent<UnitAI>(out var ai)
                       && ai.CurrentState == UnitState.Idling)
                .OrderBy(u => Vector3.Distance(u.transform.position, site.transform.position)) // Ближайшие
                .Take(5) // Берем 5 штук
                .ToList();

            if (availableUnits.Count == 0) return;

            Debug.Log($"[WorkDispatcher] Найдено {availableUnits.Count} свободных рабочих для {site.name}");

            foreach (var unit in availableUnits)
            {
                if (unit.TryGetComponent<UnitAI>(out var ai))
                {
                    // Отправляем команду
                    ai.CommandBuild(site);
                }
            }
        }
    }
}