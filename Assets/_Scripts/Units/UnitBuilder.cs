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

        private void Awake() { _motor = GetComponent<UnitMotor>(); }

        public void SetTarget(ConstructionSite site)
        {
            _targetSite = site;
            StartCoroutine(BuildRoutine());
        }

        private IEnumerator BuildRoutine()
        {
            _motor.MoveTo(_targetSite.transform.position);

            while (Vector3.Distance(transform.position, _targetSite.transform.position) > 2f)
            {
                yield return null;
            }

            _motor.MoveTo(transform.position); // Stop moving

            while (_targetSite != null)
            {
                yield return new WaitForSeconds(1f); // Work every second
                bool isFinished = _targetSite.AddBuildProgress(1f);
                if (isFinished)
                {
                    yield break; // Exit coroutine
                }
            }
        }
    }
}