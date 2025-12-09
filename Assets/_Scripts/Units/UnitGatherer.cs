using System.Collections;
using UnityEngine;
using WarOfCrowns.Core;
using WarOfCrowns.World;
using WarOfCrowns.Buildings;
using Unity.Netcode;
using Unit = WarOfCrowns.Units.Unit;
namespace WarOfCrowns.Units
{
    [RequireComponent(typeof(UnitMotor), typeof(Unit))]
    public class UnitGatherer : NetworkBehaviour
    {
        private Unit _unit;
        private UnitMotor _motor;
        private UnitVisuals _visuals;
        private ResourceNode _currentTarget;
        private Coroutine _gatherCoroutine;

        [SerializeField] private float gatherDistance = 1.2f;
        [SerializeField] private float gatherRate = 1f;

        private void Awake()
        {
            _unit = GetComponent<Unit>();
            _motor = GetComponent<UnitMotor>();
            _visuals = GetComponent<UnitVisuals>();
        }

        public void SetTarget(ResourceNode resourceNode)
        {
            if (!IsServer)
            {
                SetTargetServerRpc(resourceNode.transform.position);
                return;
            }
            StartGatheringLogic(resourceNode);
        }

        [ServerRpc]
        private void SetTargetServerRpc(Vector3 resourcePos)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(resourcePos, 2.0f);
            ResourceNode targetNode = null;
            float minDst = float.MaxValue;

            foreach (var hit in hits)
            {
                ResourceNode node = hit.GetComponent<ResourceNode>();
                if (node == null) node = hit.GetComponentInParent<ResourceNode>();

                if (node != null)
                {
                    float d = Vector3.Distance(resourcePos, node.transform.position);
                    if (d < minDst) { minDst = d; targetNode = node; }
                }
            }

            if (targetNode != null) StartGatheringLogic(targetNode);
        }

        private void StartGatheringLogic(ResourceNode node)
        {
            StopGathering();
            _currentTarget = node;
            _gatherCoroutine = StartCoroutine(GatherRoutine());
        }

        public void StopGathering()
        {
            if (_gatherCoroutine != null) StopCoroutine(_gatherCoroutine);
            _gatherCoroutine = null;
            _currentTarget = null;
        }

        private IEnumerator GatherRoutine()
        {
            if (_currentTarget == null) yield break;

            Collider2D targetCollider = _currentTarget.GetComponent<Collider2D>();

            // 1. Äâèæåíèå
            while (true)
            {
                if (_currentTarget == null) yield break;

                Vector3 targetPoint = targetCollider
                    ? targetCollider.ClosestPoint(transform.position)
                    : _currentTarget.transform.position;

                if (Vector3.Distance(transform.position, targetPoint) <= gatherDistance)
                {
                    _motor.StopMoving();
                    break;
                }
                _motor.MoveTo(targetPoint);
                yield return null;
            }

            // 2. Äîáû÷à
            while (_currentTarget != null)
            {
                // --- ÏÐÎÂÅÐÊÀ ÄÈÑÒÀÍÖÈÈ ---
                Vector3 targetPoint = targetCollider
                    ? targetCollider.ClosestPoint(transform.position)
                    : _currentTarget.transform.position;

                if (Vector3.Distance(transform.position, targetPoint) > gatherDistance + 0.5f)
                {
                    _currentTarget = null;
                    yield break;
                }
                // ---------------------------

                _unit.PlayAttackVisualsClientRpc(_currentTarget.transform.position);

                float speedMult = _unit.GetToolSpeedMultiplier(_currentTarget.resourceType.ToString());
                yield return new WaitForSeconds(gatherRate / speedMult);

                if (_currentTarget == null) yield break;

                if (NetworkManager.Singleton.IsServer)
                {
                    _unit.ReduceDurability(true, 1);
                    int amount = _currentTarget.TakeHit();

                    if (amount > 0)
                    {
                        if (_unit.OwningKingdom == null || _unit.OwningKingdom.kingdomID.Value != _unit.ownerKingdomID.Value)
                        {
                            _unit.OwningKingdom = Kingdom.GetKingdomByID(_unit.ownerKingdomID.Value);
                        }

                        if (_unit.OwningKingdom != null)
                        {
                            _unit.OwningKingdom.AddResource(_currentTarget.resourceType, amount);
                        }
                    }
                }
            }
        }
    }
}