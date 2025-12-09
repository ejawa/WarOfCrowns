using UnityEngine;
using Unity.Netcode;
using WarOfCrowns.Units;
using WarOfCrowns.Core;
using System.Collections.Generic;

namespace WarOfCrowns.Buildings
{
    public class Barracks : NetworkBehaviour
    {
        [Header("Точки")]
        [SerializeField] private Transform entrancePoint;
        [SerializeField] private Transform spawnPoint;

        private Building _building;

        private void Awake()
        {
            _building = GetComponent<Building>();
        }

        public void TrainSpecificUnit(Unit unit, ResourceType weaponType)
        {
            if (unit.TryGetComponent<UnitAI>(out var ai))
            {
                ai.CommandGoTrain(this, weaponType);
            }
        }

        public void FinalizeTraining(Unit unit, ResourceType weaponType)
        {
            TrainUnitServerRpc(unit.GetComponent<NetworkObject>().NetworkObjectId, weaponType);
        }

        [ServerRpc(RequireOwnership = false)]
        public void TrainUnitServerRpc(ulong unitId, ResourceType weaponType)
        {
            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(unitId, out var netObj)) return;
            Unit unit = netObj.GetComponent<Unit>();
            if (unit == null) return;

            if (_building.OwningKingdom != null && _building.OwningKingdom.GetResourceAmount(weaponType) >= 1)
            {
                _building.OwningKingdom.AddResource(weaponType, -1);
                unit.EquipItemServer(weaponType);
                unit.SetProfession(ProfessionType.Soldier);

                if (spawnPoint != null) unit.transform.position = spawnPoint.position;
                if (unit.TryGetComponent<UnitAI>(out var ai)) ai.SetState(UnitState.Idling);

                // ИСПРАВЛЕНО: UnitName
                Debug.Log($"Barracks: Юнит {unit.UnitName} вооружен {weaponType}!");
            }
        }
    }
}