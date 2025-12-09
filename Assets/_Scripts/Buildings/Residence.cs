using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using WarOfCrowns.Units;

namespace WarOfCrowns.Buildings
{
    [RequireComponent(typeof(Building))]
    public class Residence : NetworkBehaviour
    {
        [Header("Жилье")]
        public int capacity = 5;

        // Список ID жильцов (только серверный)
        private List<ulong> _residents = new List<ulong>();

        public List<ulong> GetResidents() => _residents;

        // --- ЛОГИКА ---

        [ServerRpc(RequireOwnership = false)]
        public void CallResidentsServerRpc()
        {
            foreach (ulong id in _residents)
            {
                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(id, out var netObj))
                {
                    var unit = netObj.GetComponent<Unit>();
                    var ai = unit.GetComponent<UnitAI>();
                    // Командуем идти домой
                    ai.CancelAction();
                    ai.CommandMoveTo(transform.position); // Или спец команда "GoHome"
                    // Можно добавить, чтобы по приходу они прятались (EnterUnit)
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void EjectAllResidentsServerRpc()
        {
            // Выгоняем тех, кто внутри (Building._unitsInside)
            var building = GetComponent<Building>();
            // Нужно сделать копию списка, так как ExitUnit меняет его
            var inside = new List<Unit>(building.GetUnitsInside());
            foreach (var u in inside) building.ExitUnit(u);
        }

        [ServerRpc(RequireOwnership = false)]
        public void KickResidentServerRpc(ulong unitId)
        {
            if (_residents.Contains(unitId))
            {
                _residents.Remove(unitId);
                // Снимаем прописку у юнита
                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(unitId, out var netObj))
                {
                    netObj.GetComponent<Unit>().residenceNetID.Value = 0;
                }
            }
        }

        // Метод регистрации (вызывается при старте игры или найме)
        public void RegisterResident(Unit unit)
        {
            if (_residents.Count < capacity && !_residents.Contains(unit.NetworkObjectId))
            {
                _residents.Add(unit.NetworkObjectId);
                unit.residenceNetID.Value = GetComponent<NetworkObject>().NetworkObjectId;
            }
        }
    }
}