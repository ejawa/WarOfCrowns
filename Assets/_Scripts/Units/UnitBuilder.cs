using System.Collections;
using UnityEngine;
using WarOfCrowns.Buildings;
using WarOfCrowns.Core;

namespace WarOfCrowns.Units
{
    [RequireComponent(typeof(UnitMotor), typeof(Unit))]
    public class UnitBuilder : MonoBehaviour
    {
        private Unit _unit;
        private UnitMotor _motor;
        private UnitVisuals _visuals;
        private ConstructionSite _targetSite;
        private Coroutine _buildCoroutine;

        [SerializeField] private float buildDistance = 1.6f;

        private void Awake()
        {
            _unit = GetComponent<Unit>();
            _motor = GetComponent<UnitMotor>();
            _visuals = GetComponent<UnitVisuals>();
        }

        public void StartWorkingOn(ConstructionSite site)
        {
            if (_buildCoroutine != null) StopCoroutine(_buildCoroutine);
            _targetSite = site;
            _buildCoroutine = StartCoroutine(BuildRoutine());
        }

        public void Cancel()
        {
            if (_buildCoroutine != null) StopCoroutine(_buildCoroutine);
            _buildCoroutine = null;
            _targetSite = null;
            if (_motor) _motor.StopMoving();
        }

        private IEnumerator BuildRoutine()
        {
            if (_targetSite == null) yield break;

            Collider2D siteCollider = _targetSite.GetComponent<Collider2D>();

            while (_targetSite != null)
            {
                Vector3 targetPos = siteCollider ? siteCollider.ClosestPoint(transform.position) : _targetSite.transform.position;
                if (Vector3.Distance(transform.position, targetPos) <= buildDistance)
                {
                    _motor.StopMoving();
                    break;
                }
                _motor.MoveTo(targetPos);
                yield return null;
            }

            while (_targetSite != null)
            {
                if (_visuals) _visuals.FaceTarget(_targetSite.transform.position);
                _unit.PlayAttackVisualsClientRpc(_targetSite.transform.position);

                float speed = _unit.GetToolSpeedMultiplier("Construction");
                yield return new WaitForSeconds(1f / speed);

                if (_targetSite == null) yield break;

                bool finished = _targetSite.AddBuildProgress(1f);
                _unit.ReduceDurability(true, 1);

                if (finished)
                {
                    _targetSite = null;
                    if (_unit.TryGetComponent<UnitAI>(out var ai))
                        ai.SetState(UnitState.Idling);
                    yield break;
                }
            }
        }
    }
}