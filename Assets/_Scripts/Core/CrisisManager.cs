using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using WarOfCrowns.UI;
using WarOfCrowns.Buildings;
using WarOfCrowns.Units;

namespace WarOfCrowns.Core
{
    public enum CrisisType { None, Criticism, Disobedience, Riots }

    public class CrisisManager : NetworkBehaviour
    {
        public static CrisisManager Instance { get; private set; }

        private Dictionary<int, float> _deathTimers = new Dictionary<int, float>();
        private const float MAX_DEATH_TIME = 300f; // 5 минут

        private Dictionary<int, CrisisType> _activeCrises = new Dictionary<int, CrisisType>();
        private Dictionary<int, float> _crisisCooldowns = new Dictionary<int, float>();

        // Список активных бунтовщиков для каждого королевства
        private Dictionary<int, List<Unit>> _activeRioters = new Dictionary<int, List<Unit>>();

        // ID фракции мятежников
        public const int REBEL_ID = -2;

        private void Awake() { Instance = this; }

        private void Update()
        {
            if (!IsServer) return;
            if (WorldState.Instance == null || WorldState.Instance.CurrentPhase.Value != WorldPhase.Game) return;

            foreach (var kvp in Kingdom.ActiveKingdoms)
            {
                int kID = kvp.Key;
                Kingdom kingdom = kvp.Value;
                float leg = kingdom.legitimacy.Value;

                // 1. Таймер поражения
                if (leg <= 0)
                {
                    if (!_deathTimers.ContainsKey(kID)) _deathTimers[kID] = 0;
                    _deathTimers[kID] += Time.deltaTime;

                    if (_deathTimers[kID] >= MAX_DEATH_TIME)
                    {
                        Debug.LogWarning($"[CrisisManager] KINGDOM {kID} GAME OVER (0 Legitimacy)!");
                        kingdom.ModifyLegitimacy(50); // Временный сброс, чтобы игра не крашнулась
                        _deathTimers[kID] = 0;
                        // Тут можно вызвать GameFlowManager.EndGame(loserID)
                    }
                }
                else
                {
                    if (_deathTimers.ContainsKey(kID)) _deathTimers[kID] = 0;
                }

                // 2. Проверка на кризис
                CheckForCrisis(kID, leg);
            }
        }

        private void CheckForCrisis(int kID, float legitimacy)
        {
            if (_activeCrises.ContainsKey(kID) && _activeCrises[kID] != CrisisType.None) return;
            if (_crisisCooldowns.ContainsKey(kID) && Time.time < _crisisCooldowns[kID]) return;

            CrisisType newCrisis = CrisisType.None;

            if (legitimacy < 30) newCrisis = CrisisType.Riots;
            else if (legitimacy < 45) newCrisis = CrisisType.Disobedience;
            else if (legitimacy < 65) newCrisis = CrisisType.Criticism;

            if (newCrisis != CrisisType.None)
            {
                TriggerCrisis(kID, newCrisis);
            }
        }

        private void TriggerCrisis(int kID, CrisisType type)
        {
            _activeCrises[kID] = type;
            Debug.Log($"[CrisisManager] Kingdom {kID} started crisis: {type}");

            // Физический бунт
            if (type == CrisisType.Riots)
            {
                StartCoroutine(StartRiotRoutine(kID, 10));
            }

            // Уведомление клиента
            ClientRpcParams clientParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { (ulong)kID } }
            };
            ShowCrisisAlertClientRpc(type, clientParams);
        }

        private IEnumerator StartRiotRoutine(int kID, int count)
        {
            if (PopulationManager.Instance == null) yield break;

            // Ищем центр бунта (здание)
            var allBuildings = FindObjectsOfType<Building>();
            var myBuildings = allBuildings.Where(b => b.ownerKingdomID.Value == kID).ToList();
            if (myBuildings.Count == 0) yield break;

            Vector3 center = myBuildings[Random.Range(0, myBuildings.Count)].transform.position + Vector3.right * 3f;

            // Ищем юнитов
            var myUnits = PopulationManager.Instance.AllUnits
                .Where(u => u.ownerKingdomID.Value == kID)
                .OrderBy(u => Vector3.Distance(u.transform.position, center))
                .Take(count)
                .ToList();

            if (!_activeRioters.ContainsKey(kID)) _activeRioters[kID] = new List<Unit>();
            _activeRioters[kID] = myUnits;

            float radius = 3f;
            float angleStep = 360f / Mathf.Max(1, myUnits.Count);

            // 1. Отправляем в точку сбора
            for (int i = 0; i < myUnits.Count; i++)
            {
                var unit = myUnits[i];
                if (unit == null) continue;

                unit.isControlLocked = true; // Блокируем управление игроком

                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 targetPos = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;

                if (unit.TryGetComponent<UnitMotor>(out var motor))
                {
                    unit.GetComponent<UnitAI>().CancelAction();
                    motor.MoveTo(targetPos);
                }
            }

            // 2. Ждем пока дойдут (макс 8 сек)
            float waitTimer = 0f;
            while (waitTimer < 8f)
            {
                waitTimer += Time.deltaTime;
                yield return null;
            }

            // 3. Превращаем в мятежников
            foreach (var unit in myUnits)
            {
                if (unit == null) continue;
                unit.isControlLocked = false;
                unit.ownerKingdomID.Value = REBEL_ID; // Смена ID -> выход из PopulationManager
                unit.ForceUpdateKingdomReferenceServer();
                unit.SetStance(UnitStance.Aggressive);

                // Сбрасываем AI чтобы он начал искать врагов
                if (unit.TryGetComponent<UnitAI>(out var ai)) ai.SetState(UnitState.Idling);
            }

            Debug.Log($"[CrisisManager] БУНТ АКТИВИРОВАН! Юниты стали врагами.");
        }

        [ClientRpc]
        private void ShowCrisisAlertClientRpc(CrisisType type, ClientRpcParams clientParams = default)
        {
            if (LegitimacyUI.Instance) LegitimacyUI.Instance.OnCrisisStarted(type);
        }

        [ServerRpc(RequireOwnership = false)]
        public void ResolveCrisisServerRpc(int optionIndex)
        {
            int senderID = (int)OwnerClientId;
            if (!_activeCrises.ContainsKey(senderID)) return;

            CrisisType type = _activeCrises[senderID];
            Kingdom k = Kingdom.GetKingdomByID(senderID);
            bool isResolved = true;

            switch (type)
            {
                case CrisisType.Riots:
                    if (optionIndex == 0) // Подкуп
                    {
                        if (k.GetResourceAmount(ResourceType.Gold) >= 300)
                        {
                            k.AddResource(ResourceType.Gold, -300);
                            RedeemRioters(senderID);
                        }
                        else { k.ModifyLegitimacy(-5); return; } // Не решено
                    }
                    else if (optionIndex == 1) // Арест
                    {
                        bool anyArrested = ArrestRioters(senderID);
                        if (!anyArrested) { NotifyClientRpc("Нет места в тюрьмах!", senderID); return; }
                    }
                    // Kill опции тут нет в UI, но логика на всякий случай
                    break;

                case CrisisType.Criticism:
                    if (optionIndex == 0) // Подкуп
                    {
                        if (k.GetResourceAmount(ResourceType.Gold) >= 50) k.AddResource(ResourceType.Gold, -50);
                        else { k.ModifyLegitimacy(-5); isResolved = false; }
                    }
                    else if (optionIndex == 2) // Убить
                    {
                        KillRandomUnit(senderID);
                        k.ModifyLegitimacy(-10);
                    }
                    break;

                case CrisisType.Disobedience:
                    if (optionIndex == 0) // Подкуп
                    {
                        if (k.GetResourceAmount(ResourceType.Gold) >= 200) k.AddResource(ResourceType.Gold, -200);
                        else { k.ModifyLegitimacy(-10); isResolved = false; }
                    }
                    else if (optionIndex == 2) // Силовой разгон (Убить)
                    {
                        KillRandomUnit(senderID);
                        k.ModifyLegitimacy(-20);
                    }
                    break;
            }

            if (isResolved)
            {
                _activeCrises[senderID] = CrisisType.None;
                _crisisCooldowns[senderID] = Time.time + 120f;
                CloseCrisisAlertClientRpc(new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { (ulong)senderID } } });
            }
        }

        // --- ЛОГИКА ДЕЙСТВИЙ ---

        private Unit GetRandomUnit(int kingdomID)
        {
            if (PopulationManager.Instance == null) return null;

            // Фильтруем всех юнитов на карте
            var myUnits = new List<Unit>();
            foreach (var u in PopulationManager.Instance.AllUnits)
            {
                if (u != null && u.ownerKingdomID.Value == kingdomID)
                    myUnits.Add(u);
            }

            if (myUnits.Count == 0) return null;
            return myUnits[Random.Range(0, myUnits.Count)];
        }

        private void KillRandomUnit(int kID)
        {
            Unit victim = GetRandomUnit(kID);
            if (victim != null)
            {
                Debug.Log($"[CrisisManager] Казнь юнита {victim.UnitName}");
                victim.GetComponent<NetworkObject>().Despawn();
            }
            else
            {
                Debug.LogWarning("[CrisisManager] Некого казнить!");
            }
        }

        private void RedeemRioters(int kID)
        {
            if (!_activeRioters.ContainsKey(kID)) return;
            foreach (var unit in _activeRioters[kID])
            {
                if (unit != null && unit.ownerKingdomID.Value == REBEL_ID)
                {
                    unit.ownerKingdomID.Value = kID;
                    unit.ForceUpdateKingdomReferenceServer();
                    unit.SetStance(UnitStance.Defensive);
                }
            }
            _activeRioters[kID].Clear();
        }

        private bool ArrestRioters(int kID)
        {
            if (!_activeRioters.ContainsKey(kID)) return false;
            var myPrisons = FindObjectsOfType<Prison>().Where(p => p.GetComponent<Building>().ownerKingdomID.Value == kID && p.HasSpace()).ToList();
            if (myPrisons.Count == 0) return false;

            int arrested = 0;
            foreach (var unit in _activeRioters[kID].ToList())
            {
                if (unit == null) continue;
                var prison = myPrisons.FirstOrDefault(p => p.HasSpace());
                if (prison != null)
                {
                    prison.ImprisonUnit(unit);
                    _activeRioters[kID].Remove(unit);
                    arrested++;
                }
            }
            return arrested > 0;
        }

        [ClientRpc] private void CloseCrisisAlertClientRpc(ClientRpcParams p) { if (LegitimacyUI.Instance) LegitimacyUI.Instance.OnCrisisResolved(); }
        [ClientRpc]
        private void NotifyClientRpc(string msg, int targetID)
        {
            if (Kingdom.PlayerKingdom != null && Kingdom.PlayerKingdom.kingdomID.Value == targetID)
                if (NotificationUI.Instance) NotificationUI.Instance.ShowNotification(msg, Color.red);
        }
    }
}