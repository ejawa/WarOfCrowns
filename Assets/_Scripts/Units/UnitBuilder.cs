using System.Collections;
using UnityEngine;
using WarOfCrowns.Buildings;

namespace WarOfCrowns.Units
{
    [RequireComponent(typeof(UnitMotor))]
    public class UnitBuilder : MonoBehaviour
    {
        private UnitMotor _motor;
        private UnitVisuals _visuals; // <-- Ссылка на визуал
        private ConstructionSite _targetSite;
        private Coroutine _buildCoroutine;

        [SerializeField] private float buildDistance = 0.5f;

        private void Awake()
        {
            _motor = GetComponent<UnitMotor>();
            _visuals = GetComponent<UnitVisuals>(); // <-- Получаем компонент
        }

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

            // --- 1. ИДЕМ К ФУНДАМЕНТУ ---
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

            // --- 2. СТРОИМ ---
            while (_targetSite != null)
            {
                // --- ДОБАВЛЕНА АНИМАЦИЯ ---
                if (_visuals != null)
                {
                    // Поворачиваемся к центру фундамента
                    _visuals.FaceTarget(_targetSite.transform.position);
                    // Запускаем наклон
                    _visuals.TriggerAttackAnimation();
                }
                // ---------------------------

                yield return new WaitForSeconds(1f);

                // Проверка на случай, если здание достроилось или уничтожилось во время ожидания
                if (_targetSite == null) yield break;

                if (_targetSite.AddBuildProgress(1f))
                {
                    yield break; // Стройка завершена
                }
            }
        }
    }
}