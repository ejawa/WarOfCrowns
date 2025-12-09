using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace WarOfCrowns.Core
{
    public class ConnectionManager : NetworkBehaviour
    {
        public static ConnectionManager Instance { get; private set; }

        public NetworkList<PlayerData> ConnectedPlayers;

        [Header("Íàñòðîéêè")]
        public int MaxPlayers = 4;

        private void Awake()
        {
            Instance = this;
            ConnectedPlayers = new NetworkList<PlayerData>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
            }
            // ÓÁÐÀÍ ÐÓ×ÍÎÉ DISPOSE
        }

        private void OnClientConnected(ulong clientId)
        {
            if (ConnectedPlayers.Count >= MaxPlayers)
            {
                NetworkManager.Singleton.DisconnectClient(clientId);
                return;
            }
            foreach (var p in ConnectedPlayers) if (p.ClientId == clientId) return;

            int newKingdomId = ConnectedPlayers.Count;
            PlayerData newPlayer = new PlayerData { ClientId = clientId, KingdomId = newKingdomId, Status = PlayerStatus.Lobby };
            ConnectedPlayers.Add(newPlayer);
        }

        private void OnClientDisconnect(ulong clientId)
        {
            for (int i = 0; i < ConnectedPlayers.Count; i++)
            {
                if (ConnectedPlayers[i].ClientId == clientId)
                {
                    ConnectedPlayers.RemoveAt(i);
                    break;
                }
            }
        }

        public PlayerData? GetLocalPlayer()
        {
            if (NetworkManager.Singleton == null) return null;
            ulong myId = NetworkManager.Singleton.LocalClientId;
            foreach (var p in ConnectedPlayers) if (p.ClientId == myId) return p;
            return null;
        }
    }
}