using System.Collections;
using UnityEngine;
using WarOfCrowns.Buildings;

namespace WarOfCrowns.Units
{
    [RequireComponent(typeof(UnitMotor))]
    public class UnitWorker : MonoBehaviour
    {
        private UnitMotor _motor;
        private JobBuilding _targetJob;
        private Coroutine _workCoroutine;

        [SerializeField] private float workDistance = 0.5f;

        // --- НОВОЕ СВОЙСТВО ---
        // Позволяет узнать текущее место работы (нужно для AI)
        public JobBuilding CurrentJob => _targetJob;
        // ----------------------

        private void Awake() { _motor = GetComponent<UnitMotor>(); }

        public void SetTarget(JobBuilding job)
        {
            StopWorking();
            _targetJob = job;
            _workCoroutine = StartCoroutine(WorkRoutine());
        }

        public void StopWorking()
        {
            if (_workCoroutine != null)
            {
                StopCoroutine(_workCoroutine);
                _workCoroutine = null;
            }
            _targetJob = null;
        }

        private IEnumerator WorkRoutine()
        {
            if (_targetJob == null) yield break;

            Collider2D buildingCollider = _targetJob.GetComponent<Collider2D>();
            if (buildingCollider == null) yield break;

            while (true)
            {
                if (_targetJob == null) yield break;
                Vector3 targetPoint = buildingCollider.ClosestPoint(transform.position);
                if (Vector3.Distance(transform.position, targetPoint) <= workDistance) break;
                _motor.MoveTo(targetPoint);
                yield return null;
            }

            _motor.MoveTo(transform.position);

            while (_targetJob != null)
            {
                yield return new WaitForSeconds(1f);
                if (_targetJob != null) _targetJob.AddWorkProgress(1f);
            }
        }
    }
}