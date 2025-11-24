using UnityEngine;
using WarOfCrowns.Core;
using WarOfCrowns.Units;

namespace WarOfCrowns.Buildings
{
    [RequireComponent(typeof(Building))]
    public class Barracks : MonoBehaviour
    {
        [Header("Точки")]
        [SerializeField] private Transform entrancePoint;
        [SerializeField] private Transform spawnPoint;

        private Building _building;

        private void Awake()
        {
            _building = GetComponent<Building>();
        }

        // --- НОВЫЙ МЕТОД: Тренировка конкретного парня ---
        public void TrainSpecificUnit(Unit unit, ResourceType weaponType)
        {
            if (_building.OwningKingdom == null) return;

            // 1. Проверка ресурсов
            if (_building.OwningKingdom.GetResourceAmount(weaponType) >= 1)
            {
                // 2. Списываем оружие
                _building.OwningKingdom.AddResource(weaponType, -1);

                // 3. Запускаем процесс (юнит бежит в казарму)
                // Используем тот же метод, что и раньше, но теперь мы знаем, кто это
                StartTrainingProcess(unit, weaponType);
            }
            else
            {
                Debug.Log($"Barracks: Not enough {weaponType}!");
            }
        }

        private void StartTrainingProcess(Unit unit, ResourceType weapon)
        {
            // Этот код у нас уже был, оставляем логику "Иди в казарму"
            if (unit.TryGetComponent<UnitAI>(out var ai))
            {
                ai.CommandGoTrain(this, weapon);
            }
        }

        // Этот метод вызывается юнитом, когда он дошел (из UnitAI)
        public void FinalizeTraining(Unit unit, ResourceType weapon)
        {
            unit.SetProfession(ProfessionType.Soldier);
            unit.EquipItem(weapon);

            // Телепорт на выход
            if (spawnPoint != null) unit.transform.position = spawnPoint.position;

            if (unit.TryGetComponent<UnitAI>(out var ai)) ai.SetState(UnitState.Idling);

            Debug.Log($"{unit.unitName} is now a Soldier!");
        }
    }
}