using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using WarOfCrowns.Units;
using WarOfCrowns.Core;

namespace WarOfCrowns.Buildings
{
    // Данные о заключенном
    [System.Serializable]
    public class PrisonerData
    {
        public Unit unit;
        public float releaseTime; // Время сервера, когда выпускать
    }

    public class Prison : NetworkBehaviour
    {
        [Header("Настройки Тюрьмы")]
        public int maxCapacity = 5;
        public float jailTime = 60f; // Сколько секунд сидят (1 минута)

        // Количество заключенных (для UI)
        public NetworkVariable<int> currentPrisonersCount = new NetworkVariable<int>(0);

        // Список заключенных (Только сервер)
        private List<PrisonerData> _prisoners = new List<PrisonerData>();

        public bool HasSpace() => currentPrisonersCount.Value < maxCapacity;

        private void Update()
        {
            if (!IsServer) return;

            // Проверяем сроки заключения
            for (int i = _prisoners.Count - 1; i >= 0; i--)
            {
                if (Time.time >= _prisoners[i].releaseTime)
                {
                    ReleasePrisoner(_prisoners[i]);
                    _prisoners.RemoveAt(i);
                }
            }

            // Синхронизируем количество
            if (currentPrisonersCount.Value != _prisoners.Count)
                currentPrisonersCount.Value = _prisoners.Count;
        }

        public bool ImprisonUnit(Unit unit)
        {
            if (!IsServer) return false;
            if (_prisoners.Count >= maxCapacity) return false;
            if (unit.TryGetComponent<Fighter>(out var fighter))
            {
                fighter.Cancel(); // Сброс цели
                fighter.enabled = false; // Отключить компонент
            }
            // 1. Телепортируем и прячем
            unit.transform.position = transform.position;
            unit.SetVisibility(false);

            // 2. Отключаем мозги
            if (unit.TryGetComponent<UnitAI>(out var ai))
            {
                ai.CancelAction();
                ai.enabled = false;
            }
            if (unit.TryGetComponent<UnitMotor>(out var motor)) motor.StopMoving();

            // 3. Записываем срок
            PrisonerData data = new PrisonerData
            {
                unit = unit,
                releaseTime = Time.time + jailTime
            };
            _prisoners.Add(data);

            Debug.Log($"[Prison] {unit.UnitName} посажен на {jailTime} сек.");
            return true;
        }

        private void ReleasePrisoner(PrisonerData data)
        {
            Unit unit = data.unit;
            if (unit != null)
            {
                // 1. Возвращаем лояльность! (Присваиваем ID владельца тюрьмы)
                // Важно: берем ID из компонента Building этой тюрьмы
                int myOwnerID = GetComponent<Building>().ownerKingdomID.Value;
                unit.ownerKingdomID.Value = myOwnerID;
                unit.ForceUpdateKingdomReferenceServer();

                // 2. Включаем
                unit.SetVisibility(true);
                if (unit.TryGetComponent<UnitAI>(out var ai))
                {
                    ai.enabled = true;
                    ai.SetState(UnitState.Idling);
                }

                // 3. Выкидываем наружу
                unit.transform.position = transform.position + Vector3.down * 2f;
                Debug.Log($"[Prison] {unit.UnitName} вышел на свободу и снова служит нам!");
                if (unit.TryGetComponent<Fighter>(out var fighter))
                {
                    fighter.enabled = true; // Включить обратно
                }
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                // Если тюрьму сломали - все выходят (но остаются бунтовщиками или лояльными? 
                // Пусть выходят лояльными, амнистия по случаю разрушения)
                foreach (var p in _prisoners) ReleasePrisoner(p);
                _prisoners.Clear();
            }
            base.OnNetworkDespawn();
        }
    }
}