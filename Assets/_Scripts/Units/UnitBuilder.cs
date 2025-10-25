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
        private Coroutine _buildCoroutine; // <-- Äîáàâèëè ññûëêó íà êîðóòèíó

        private void Awake() { _motor = GetComponent<UnitMotor>(); }

        public void SetTarget(ConstructionSite site)
        {
            // Stop previous building process if any
            Cancel();

            _targetSite = site;
            _buildCoroutine = StartCoroutine(BuildRoutine());
        }

        // --- ÂÎÒ ÍÎÂÀß ÔÓÍÊÖÈß ---
        public void Cancel()
        {
            if (_buildCoroutine != null)
            {
                StopCoroutine(_buildCoroutine);
                _buildCoroutine = null;
            }
            // We don't need to stop the motor here, 
            // as the selection controller will give a new move command right after.
        }
        // --- ÊÎÍÅÖ ÍÎÂÎÉ ÔÓÍÊÖÈÈ ---

        private IEnumerator BuildRoutine()
        {
            _motor.MoveTo(_targetSite.transform.position);

            while (Vector3.Distance(transform.position, _targetSite.transform.position) > 2f)
            {
                if (_targetSite == null) yield break; // Target might be destroyed
                yield return null;
            }

            _motor.MoveTo(transform.position); // Stop moving

            while (_targetSite != null)
            {
                yield return new WaitForSeconds(1f);
                bool isFinished = _targetSite.AddBuildProgress(1f);
                if (isFinished)
                {
                    _buildCoroutine = null;
                    yield break;
                }
            }
        }
    }
}