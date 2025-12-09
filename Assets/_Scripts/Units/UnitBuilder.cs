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

        [SerializeField] private float buildDistance = 1.6f; // Чуть больше, чтобы не толкались

        private void Awake()
        {
            _unit = GetComponent<Unit>();
            _motor = GetComponent<UnitMotor>();
            _visuals = GetComponent<UnitVisuals>();
        }

        public void SetTarget(ConstructionSite site)
        {
            // Не вызываем Cancel() здесь, так как UnitAI уже должен был это сделать
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
            // 1. Подход к цели
            Collider2D siteCollider = _targetSite ? _targetSite.GetComponent<Collider2D>() : null;

            while (_targetSite != null)
            {
                Vector3 targetPos = siteCollider ? siteCollider.ClosestPoint(transform.position) : _targetSite.transform.position;
                float dist = Vector3.Distance(transform.position, targetPos);

                if (dist <= buildDistance)
                {
                    _motor.StopMoving();
                    break;
                }

                _motor.MoveTo(targetPos);
                yield return null;
            }

            // 2. Процесс стройки
            while (_targetSite != null)
            {
                // Поворачиваемся к стройке
                if (_visuals) _visuals.FaceTarget(_targetSite.transform.position);

                // Анимация удара
                if (_visuals) _visuals.TriggerAttackAnimation();

                // Расчет скорости
                float speed = _unit.GetToolSpeedMultiplier("Construction");
                // Ждем время "замаха"
                yield return new WaitForSeconds(1f / speed);

                // Проверка: сайт все еще существует?
                if (_targetSite == null) yield break;

                // Проверка дистанции (если оттолкнули)
                Vector3 targetPos = siteCollider ? siteCollider.ClosestPoint(transform.position) : _targetSite.transform.position;
                if (Vector3.Distance(transform.position, targetPos) > buildDistance + 0.5f)
                {
                    // Если далеко - идем обратно (рекурсия или просто break чтобы UnitAI перезапустил?)
                    // Проще всего: снова включаем мотор
                    _motor.MoveTo(targetPos);
                    yield return null;
                    continue; // Пропускаем удар, пока идем
                }
                else
                {
                    _motor.StopMoving();
                }

                // Внесение прогресса
                if (_unit.IsServer)
                {
                    _unit.ReduceDurability(true, 1);
                    bool finished = _targetSite.AddBuildProgress(1f); // 1.0 прогресса

                    if (finished)
                    {
                        _targetSite = null;
                        if (_unit.TryGetComponent<UnitAI>(out var ai)) ai.SetState(UnitState.Idling);
                        yield break;
                    }
                }
            }
        }
    }
}