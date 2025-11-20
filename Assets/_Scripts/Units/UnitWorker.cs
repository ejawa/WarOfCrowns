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

        // На каком расстоянии от стены здания останавливаться
        [SerializeField] private float workDistance = 0.5f;

        private void Awake() { _motor = GetComponent<UnitMotor>(); }

        public void SetTarget(JobBuilding job)
        {
            StopWorking();
            _targetJob = job;
            _workCoroutine = StartCoroutine(WorkRoutine());
        }

        public void StopWorking()
        {
            if (_workCoroutine != null) StopCoroutine(_workCoroutine);
            _workCoroutine = null;
        }

        private IEnumerator WorkRoutine()
        {
            if (_targetJob == null) yield break;

            // Получаем коллайдер здания (стены), чтобы знать, где остановиться
            Collider2D buildingCollider = _targetJob.GetComponent<Collider2D>();

            if (buildingCollider == null)
            {
                Debug.LogError($"Building {_targetJob.name} has no Collider! Unit cannot find where to stop.");
                yield break;
            }

            // --- ДВИЖЕНИЕ К ЦЕЛИ ---
            while (true)
            {
                if (_targetJob == null) yield break;

                // Находим ближайшую точку на КРАЮ (периметре) здания
                Vector3 targetPoint = buildingCollider.ClosestPoint(transform.position);

                // Проверяем расстояние до этой точки (до стены)
                float distanceToWall = Vector3.Distance(transform.position, targetPoint);

                if (distanceToWall <= workDistance)
                {
                    // Мы пришли!
                    break;
                }

                // Идем к этой точке на стене
                _motor.MoveTo(targetPoint);
                yield return null;
            }

            // Останавливаемся
            _motor.MoveTo(transform.position);

            // --- РАБОТА ---
            while (_targetJob != null)
            {
                yield return new WaitForSeconds(1f);
                _targetJob.AddWorkProgress(1f);
            }
        }
    }
}