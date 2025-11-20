using System.Collections;
using UnityEngine;
using WarOfCrowns.Buildings;

namespace WarOfCrowns.Units
{
    [RequireComponent(typeof(UnitMotor))]
    public class UnitBuilder : MonoBehaviour
    {
        private UnitMotor _motor;
        private ConstructionSite _targetSite;
        private Coroutine _buildCoroutine;

        [SerializeField] private float buildDistance = 0.5f;

        private void Awake() { _motor = GetComponent<UnitMotor>(); }

        public void SetTarget(ConstructionSite site)
        {
            Cancel();
            _targetSite = site;
            _buildCoroutine = StartCoroutine(BuildRoutine());
        }

        public void Cancel()
        {
            if (_buildCoroutine != null) StopCoroutine(_buildCoroutine);
        }

        private IEnumerator BuildRoutine()
        {
            if (_targetSite == null) yield break;

            Collider2D siteCollider = _targetSite.GetComponent<Collider2D>();
            if (siteCollider == null)
            {
                Debug.LogError($"Foundation {_targetSite.name} has no Collider!");
                yield break;
            }

            // --- ИДЕМ К ФУНДАМЕНТУ ---
            while (true)
            {
                if (_targetSite == null) yield break;

                // Ищем ближайшую точку на краю фундамента
                Vector3 targetPoint = siteCollider.ClosestPoint(transform.position);

                if (Vector3.Distance(transform.position, targetPoint) <= buildDistance)
                {
                    break; // Пришли
                }

                _motor.MoveTo(targetPoint);
                yield return null;
            }

            _motor.MoveTo(transform.position); // Стоп

            // --- СТРОИМ ---
            while (_targetSite != null)
            {
                yield return new WaitForSeconds(1f);
                if (_targetSite.AddBuildProgress(1f))
                {
                    yield break;
                }
            }
        }
    }
}