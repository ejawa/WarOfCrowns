using UnityEngine;
using System.Collections;
using Unity.Netcode;
using WarOfCrowns.Core;
using WarOfCrowns.World;
using Unit = WarOfCrowns.Units.Unit;

namespace WarOfCrowns.Units
{
    [RequireComponent(typeof(UnitMotor), typeof(Unit))]
    public class UnitGatherer : NetworkBehaviour
    {
        private Unit _unit;
        private UnitMotor _motor;

        [Header("Настройки Добычи")]
        [SerializeField] private float gatherDistance = 1.2f;
        [SerializeField] private float gatherRate = 1f;

        private void Awake()
        {
            _unit = GetComponent<Unit>();
            _motor = GetComponent<UnitMotor>();
        }

        public void StartWorkingOn(ResourceNode resourceNode)
        {
            if (!IsOwner) return;
            RequestGatherServerRpc(resourceNode.GetComponent<NetworkObject>().NetworkObjectId);
        }

        [ServerRpc]
        private void RequestGatherServerRpc(ulong resourceNetId)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(resourceNetId, out var netObj))
            {
                var targetNode = netObj.GetComponent<ResourceNode>();
                if (targetNode != null)
                {
                    StopAllCoroutines();
                    StartCoroutine(ServerGatherRoutine(targetNode));
                }
            }
        }

        private IEnumerator ServerGatherRoutine(ResourceNode target)
        {
            if (target == null) yield break;

            _unit.GetComponent<UnitAI>().SetState(UnitState.Foraging);

            Collider2D targetCollider = target.GetComponent<Collider2D>();

            while (target != null)
            {
                Vector3 targetPoint = targetCollider ? targetCollider.ClosestPoint(transform.position) : target.transform.position;
                if (Vector3.Distance(transform.position, targetPoint) <= gatherDistance)
                {
                    _motor.StopMoving();
                    break;
                }
                _motor.MoveTo(targetPoint);
                yield return null;
            }

            while (target != null)
            {
                _unit.PlayAttackVisualsClientRpc(target.transform.position);

                float speedMult = _unit.GetToolSpeedMultiplier(target.resourceType.ToString());
                yield return new WaitForSeconds(gatherRate / speedMult);

                if (target == null) break;

                _unit.ReduceDurability(true, 1);
                int amount = target.TakeHit();
                if (amount > 0 && _unit.OwningKingdom != null)
                {
                    // 1. Добавляем ресурс в инвентарь на сервере
                    _unit.OwningKingdom.AddResource(target.resourceType, amount);

                    // 2. --- НОВЫЙ ДЕБАГ-ЛОГ ---
                    // Сразу после добавления, запрашиваем актуальное количество из того же инвентаря
                    int newTotal = _unit.OwningKingdom.GetResourceAmount(target.resourceType);
                    //Debug.LogWarning($"[SERVER GATHER] Kingdom ID {_unit.OwningKingdom.kingdomID.Value} получил {amount} {target.resourceType}. Новый итог на сервере: {newTotal}");
                    // --------------------------
                }
            }

            _unit.GetComponent<UnitAI>().SetState(UnitState.Idling);
        }

        public void StopGathering()
        {
            if (IsServer) StopAllCoroutines();
        }
    }
}