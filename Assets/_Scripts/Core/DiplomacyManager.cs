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

        private void Awake() { Instance = this; }

        public void RequestRename(string newName)
        {
            if (Kingdom.PlayerKingdom != null) Kingdom.PlayerKingdom.SetName(newName);
        }

        public void RequestDeclareWar(int targetKingdomID)
        {
            if (Kingdom.PlayerKingdom == null) return;

            if (Kingdom.PlayerKingdom.GetResourceAmount(ResourceType.Gold) >= declareWarCost)
            {
                // ИСПРАВЛЕНО: .Value
                DeclareWarServerRpc(Kingdom.PlayerKingdom.kingdomID.Value, targetKingdomID);
            }
            else
            {
                NotificationUI.Instance.ShowNotification($"Не хватает золота! Нужно {declareWarCost}.", Color.red);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void DeclareWarServerRpc(int attackerID, int targetID)
        {
            Kingdom attacker = null;
            Kingdom target = null;

            foreach (var k in FindObjectsOfType<Kingdom>())
            {
                // ИСПРАВЛЕНО: .Value
                if (k.kingdomID.Value == attackerID) attacker = k;
                if (k.kingdomID.Value == targetID) target = k;
            }

            if (attacker == null || target == null) return;

            attacker.AddResource(ResourceType.Gold, -declareWarCost);

            if (!attacker.enemiesList.Contains(targetID)) attacker.enemiesList.Add(targetID);
            if (!target.enemiesList.Contains(attackerID)) target.enemiesList.Add(attackerID);

            NotifyWarClientRpc(attacker.kingdomName.Value.ToString(), target.kingdomName.Value.ToString());
        }

        [ClientRpc]
        private void NotifyWarClientRpc(string attackerName, string targetName)
        {
            string message = $"{attackerName} ОБЪЯВИЛ ВОЙНУ {targetName}!";
            NotificationUI.Instance.ShowNotification(message, new Color(1f, 0.3f, 0f));
        }
    }
}