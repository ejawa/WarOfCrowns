using System.Collections;
using UnityEngine;
using WarOfCrowns.Buildings; // Мы подключили библиотеку зданий

namespace WarOfCrowns.Units
{
    [RequireComponent(typeof(UnitMotor))]
    public class UnitBuilder : MonoBehaviour
    {
        private UnitMotor _motor;
        private ConstructionSite _targetSite; // Теперь он знает, что это такое
        private Coroutine _buildCoroutine;

        private void Awake() { _motor = GetComponent<UnitMotor>(); }

        public void SetTarget(ConstructionSite site) // И здесь тоже знает
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
            if (_targetSite == null) yield break; // Добавим проверку на всякий случай

            _motor.MoveTo(_targetSite.transform.position);
            while (Vector3.Distance(transform.position, _targetSite.transform.position) > 2f)
            {
                if (_targetSite == null) yield break;
                yield return null;
            }
            _motor.MoveTo(transform.position);

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