using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using WarOfCrowns.Data;

namespace WarOfCrowns.Core
{
    public class DecreeManager : NetworkBehaviour
    {
        public static DecreeManager Instance { get; private set; }

        [Header("База Указов")]
        public List<DecreeData> availableDecrees;

        // Храним кулдауны: KingdomID -> (DecreeID -> TimeWhenReady)
        private Dictionary<int, Dictionary<string, float>> _cooldowns = new Dictionary<int, Dictionary<string, float>>();

        private void Awake()
        {
            Instance = this;
        }

        public float GetRemainingCooldown(int kingdomID, string decreeID)
        {
            if (_cooldowns.ContainsKey(kingdomID) && _cooldowns[kingdomID].TryGetValue(decreeID, out float readyTime))
            {
                return Mathf.Max(0, readyTime - Time.time);
            }
            return 0f;
        }

        // Вызывается из UI кнопкой
        public void RequestEnactDecree(string decreeID)
        {
            EnactDecreeServerRpc(decreeID);
        }

        [ServerRpc(RequireOwnership = false)]
        private void EnactDecreeServerRpc(string decreeID, ServerRpcParams rpcParams = default)
        {
            int senderID = (int)rpcParams.Receive.SenderClientId;
            Kingdom kingdom = Kingdom.GetKingdomByID(senderID);

            if (kingdom == null) return;

            // 1. Ищем указ
            DecreeData decree = availableDecrees.Find(d => d.id == decreeID);
            if (decree == null) return;

            // 2. Проверяем Кулдаун
            if (GetRemainingCooldown(senderID, decreeID) > 0) return;

            // 3. Проверяем Ресурсы
            // Сначала проверка (Transaction Check)
            foreach (var cost in decree.costs)
            {
                if (cost.resourceType == ResourceType.Food)
                {
                    if (kingdom.GetTotalFoodAmount() < cost.amount) return; // Не хватает еды
                }
                else
                {
                    if (kingdom.GetResourceAmount(cost.resourceType) < cost.amount) return; // Не хватает реса
                }
            }

            // 4. Списываем Ресурсы
            foreach (var cost in decree.costs)
            {
                if (cost.resourceType == ResourceType.Food)
                {
                    kingdom.TrySpendFood(cost.amount);
                }
                else
                {
                    kingdom.AddResource(cost.resourceType, -cost.amount);
                }
            }

            // 5. Применяем Эффект
            kingdom.ModifyLegitimacy(decree.legitimacyBoost);

            // 6. Ставим Кулдаун
            if (!_cooldowns.ContainsKey(senderID)) _cooldowns[senderID] = new Dictionary<string, float>();
            _cooldowns[senderID][decreeID] = Time.time + decree.cooldown;

            Debug.Log($"[DecreeManager] Kingdom {senderID} приняло указ {decree.title}. +{decree.legitimacyBoost} Легитимности.");

            // 7. Сообщаем клиенту об успехе (для обновления UI кулдаунов)
            ConfirmDecreeClientRpc(senderID, decreeID, Time.time + decree.cooldown);
        }

        [ClientRpc]
        private void ConfirmDecreeClientRpc(int kingdomID, string decreeID, float readyTime)
        {
            // Обновляем локальный словарь кулдаунов (для UI)
            if (!_cooldowns.ContainsKey(kingdomID)) _cooldowns[kingdomID] = new Dictionary<string, float>();
            _cooldowns[kingdomID][decreeID] = readyTime;

            // Если это наш указ - обновляем UI
            if (Kingdom.PlayerKingdom != null && Kingdom.PlayerKingdom.kingdomID.Value == kingdomID)
            {
                if (WarOfCrowns.UI.LegitimacyUI.Instance)
                    WarOfCrowns.UI.LegitimacyUI.Instance.RefreshDecreeUI();
            }
        }
    }
}