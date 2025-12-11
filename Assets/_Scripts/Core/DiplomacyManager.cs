using UnityEngine;
using Unity.Netcode;
using WarOfCrowns.UI;

namespace WarOfCrowns.Core
{
    public class DiplomacyManager : NetworkBehaviour
    {
        public static DiplomacyManager Instance { get; private set; }

        [Header("Настройки")]
        [SerializeField] private int declareWarCost = 500;

        // Настройки Легитимности
        private const float LEGITIMACY_WAR_FORMAL_GAIN = 20f; // Бонус атакующему (офиц.)
        private const float LEGITIMACY_WAR_FORMAL_PENALTY = -20f; // Штраф жертве (офиц.)

        private const float LEGITIMACY_WAR_SNEAK_PENALTY = -20f; // Штраф агрессору (вкрысу)
        private const float LEGITIMACY_WAR_SNEAK_GAIN = 15f; // Бонус жертве (вкрысу)

        private void Awake() { Instance = this; }

        public void RequestRename(string newName)
        {
            if (Kingdom.PlayerKingdom != null) Kingdom.PlayerKingdom.SetName(newName);
        }

        // --- 1. ОФИЦИАЛЬНАЯ ВОЙНА (Кнопка) ---
        public void RequestDeclareWar(int targetKingdomID)
        {
            if (Kingdom.PlayerKingdom == null) return;

            // Проверка золота
            if (Kingdom.PlayerKingdom.GetResourceAmount(ResourceType.Gold) >= declareWarCost)
            {
                DeclareWarServerRpc(targetKingdomID);
            }
            else
            {
                if (NotificationUI.Instance)
                    NotificationUI.Instance.ShowNotification($"Нужно {declareWarCost} золота для войны!", Color.yellow);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void DeclareWarServerRpc(int targetID, ServerRpcParams rpcParams = default)
        {
            int attackerID = (int)rpcParams.Receive.SenderClientId;
            if (attackerID == targetID) return;

            Kingdom attacker = Kingdom.GetKingdomByID(attackerID);
            Kingdom target = Kingdom.GetKingdomByID(targetID);

            if (attacker == null || target == null) return;
            if (attacker.enemiesList.Contains(targetID)) return; // Уже воюют

            // 1. Списываем золото
            attacker.AddResource(ResourceType.Gold, -declareWarCost);

            // 2. Легитимность (Официальная война)
            attacker.ModifyLegitimacy(LEGITIMACY_WAR_FORMAL_GAIN); // Атакующему бонус
            target.ModifyLegitimacy(LEGITIMACY_WAR_FORMAL_PENALTY); // Жертве штраф

            // 3. Начинаем войну
            StartWarState(attacker, target);

            NotifyWarClientRpc(attacker.kingdomName.Value.ToString(), target.kingdomName.Value.ToString(), true);
        }

        // --- 2. ВНЕЗАПНАЯ ВОЙНА (Вызывается из UnitAI) ---
        // aggressorID - тот, чей юнит первым заметил/ударил
        public void TriggerSurpriseWar(int aggressorID, int victimID)
        {
            if (!IsServer) return;

            Kingdom attacker = Kingdom.GetKingdomByID(aggressorID);
            Kingdom target = Kingdom.GetKingdomByID(victimID);

            if (attacker == null || target == null) return;
            if (attacker.IsAtWarWith(victimID)) return; // Уже воюют

            // 1. Золото НЕ списываем

            // 2. Легитимность (Вкрысу)
            attacker.ModifyLegitimacy(LEGITIMACY_WAR_SNEAK_PENALTY); // Агрессору штраф
            target.ModifyLegitimacy(LEGITIMACY_WAR_SNEAK_GAIN);      // Жертве бонус (сплочение)

            Debug.Log($"[Diplomacy] {attacker.kingdomName.Value} напал вкрысу на {target.kingdomName.Value}!");

            // 3. Начинаем войну
            StartWarState(attacker, target);

            NotifyWarClientRpc(attacker.kingdomName.Value.ToString(), target.kingdomName.Value.ToString(), false);
        }

        // Общая логика начала войны
        private void StartWarState(Kingdom k1, Kingdom k2)
        {
            if (!k1.enemiesList.Contains(k2.kingdomID.Value)) k1.enemiesList.Add(k2.kingdomID.Value);
            if (!k2.enemiesList.Contains(k1.kingdomID.Value)) k2.enemiesList.Add(k1.kingdomID.Value);

            // Сбрасываем предложения мира
            int id1 = k1.kingdomID.Value;
            int id2 = k2.kingdomID.Value;
            if (k1.incomingPeaceOffers.Contains(id2)) k1.incomingPeaceOffers.Remove(id2);
            if (k2.incomingPeaceOffers.Contains(id1)) k2.incomingPeaceOffers.Remove(id1);
        }

        // --- 3. МИРНЫЕ ДОГОВОРЫ ---
        public void RequestOfferPeace(int targetKingdomID) { OfferPeaceServerRpc(targetKingdomID); }

        [ServerRpc(RequireOwnership = false)]
        private void OfferPeaceServerRpc(int targetID, ServerRpcParams rpcParams = default)
        {
            int senderID = (int)rpcParams.Receive.SenderClientId;
            Kingdom target = Kingdom.GetKingdomByID(targetID);

            if (target != null && !target.incomingPeaceOffers.Contains(senderID))
            {
                target.incomingPeaceOffers.Add(senderID);
                NotifyPeaceOfferClientRpc(senderID, targetID);
            }
        }

        public void RequestAcceptPeace(int targetKingdomID) { AcceptPeaceServerRpc(targetKingdomID); }

        [ServerRpc(RequireOwnership = false)]
        private void AcceptPeaceServerRpc(int targetID, ServerRpcParams rpcParams = default)
        {
            int accepterID = (int)rpcParams.Receive.SenderClientId;
            Kingdom accepter = Kingdom.GetKingdomByID(accepterID);
            Kingdom offerer = Kingdom.GetKingdomByID(targetID);

            if (accepter == null || offerer == null) return;

            if (accepter.incomingPeaceOffers.Contains(targetID))
            {
                accepter.incomingPeaceOffers.Remove(targetID);
                if (accepter.enemiesList.Contains(targetID)) accepter.enemiesList.Remove(targetID);
                if (offerer.enemiesList.Contains(accepterID)) offerer.enemiesList.Remove(accepterID);

                NotifyPeaceSignedClientRpc(accepter.kingdomName.Value.ToString(), offerer.kingdomName.Value.ToString());
            }
        }

        // --- УВЕДОМЛЕНИЯ ---
        [ClientRpc]
        private void NotifyWarClientRpc(string name1, string name2, bool isFormal)
        {
            if (NotificationUI.Instance)
            {
                string msg = isFormal
                    ? $"ОФИЦИАЛЬНАЯ ВОЙНА! {name1} объявил войну {name2}!"
                    : $"ВНЕЗАПНАЯ АТАКА! {name1} напал на {name2}!";

                NotificationUI.Instance.ShowNotification(msg, Color.red);
            }
        }

        [ClientRpc]
        private void NotifyPeaceOfferClientRpc(int senderID, int targetID)
        {
            if (Kingdom.PlayerKingdom != null && Kingdom.PlayerKingdom.kingdomID.Value == targetID)
            {
                Kingdom sender = Kingdom.GetKingdomByID(senderID);
                string sName = sender ? sender.kingdomName.Value.ToString() : "Враг";
                if (NotificationUI.Instance) NotificationUI.Instance.ShowNotification($"{sName} предлагает мир!", Color.green);
            }
        }

        [ClientRpc]
        private void NotifyPeaceSignedClientRpc(string name1, string name2)
        {
            if (NotificationUI.Instance) NotificationUI.Instance.ShowNotification($"МИР ЗАКЛЮЧЕН между {name1} и {name2}", Color.green);
        }
    }
}