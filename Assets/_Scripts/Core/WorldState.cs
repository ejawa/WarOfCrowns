using UnityEngine;
using Unity.Netcode;
using WarOfCrowns.Data;

namespace WarOfCrowns.Core
{
    public enum WorldPhase
    {
        Lobby,
        Loading,
        Setup,
        Game
    }

    public class WorldState : NetworkBehaviour
    {
        public static WorldState Instance { get; private set; }

        public NetworkVariable<WorldPhase> CurrentPhase = new NetworkVariable<WorldPhase>(WorldPhase.Lobby);
        public NetworkVariable<int> MapSeed = new NetworkVariable<int>(0);

        // Сколько всего игроков должно быть в матче (чтобы знать, когда все готовы)
        public NetworkVariable<int> TotalPlayers = new NetworkVariable<int>(0);

        [Header("Базы Данных")]
        public NameDatabase nameDatabase;
        public AppearanceDatabase appearanceDatabase;
        public ToolDatabase toolDatabase;
        public WeaponDatabase weaponDatabase;

        public AppearanceDatabase AppearanceDB => appearanceDatabase;
        public ToolDatabase ToolDB => toolDatabase;
        public WeaponDatabase WeaponDB => weaponDatabase;

        private void Awake()
        {
            Instance = this;
        }

        public void StartGameSequence(int playerCount)
        {
            if (!IsServer) return;

            TotalPlayers.Value = playerCount;
            MapSeed.Value = Random.Range(1000, 9999999);
            CurrentPhase.Value = WorldPhase.Loading;

            Debug.Log($"[WorldState] Starting Sequence. Players: {playerCount}, Seed: {MapSeed.Value}");
        }

        public void MoveToSetup()
        {
            if (IsServer) CurrentPhase.Value = WorldPhase.Setup;
        }

        public void MoveToGame()
        {
            if (IsServer) CurrentPhase.Value = WorldPhase.Game;
        }

        public string GetRandomFullName(Gender g) => nameDatabase ? nameDatabase.GetRandomName(g) : "Unit";
    }
}