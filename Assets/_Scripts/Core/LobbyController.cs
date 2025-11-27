using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using TMPro;
using WarOfCrowns.World;

namespace WarOfCrowns.Core
{
    public class LobbyController : NetworkBehaviour
    {
        [Header("UI Панели")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject lobbyPanel;
        [SerializeField] private GameObject gameUIPanel;

        [Header("Элементы Меню")]
        [SerializeField] private TMP_InputField ipInputField; // Поле для IP
        [SerializeField] private Button hostButton;
        [SerializeField] private Button joinButton;

        [Header("Элементы Лобби")]
        [SerializeField] private TextMeshProUGUI lobbyStatusText;
        [SerializeField] private TextMeshProUGUI playerCountText;
        [SerializeField] private Button startGameButton;

        private NetworkVariable<int> playersInLobby = new NetworkVariable<int>(0);

        private void Awake()
        {
            // --- HOST (СОЗДАТЬ) ---
            hostButton.onClick.AddListener(() =>
            {
                // Принудительно ставим 0.0.0.0, чтобы сервер был виден в Radmin VPN
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData("0.0.0.0", 7777);

                NetworkManager.Singleton.StartHost();
                EnterLobby();
            });

            // --- CLIENT (ПОДКЛЮЧИТЬСЯ) ---
            joinButton.onClick.AddListener(() =>
            {
                string ip = "127.0.0.1"; // Дефолт

                if (ipInputField != null && !string.IsNullOrEmpty(ipInputField.text))
                {
                    ip = ipInputField.text;
                }

                // Указываем IP из поля ввода
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(ip, 7777);

                NetworkManager.Singleton.StartClient();
                EnterLobby();
            });

            // --- START GAME ---
            startGameButton.onClick.AddListener(() =>
            {
                if (!IsHost) return;
                // Генерируем сид
                string matchSeed = Random.Range(0, 999999).ToString();
                // Передаем сид и количество игроков для генерации карты
                StartGameClientRpc(matchSeed, playersInLobby.Value);
            });
        }

        private void Start()
        {
            if (gameUIPanel) gameUIPanel.SetActive(false);
            if (lobbyPanel) lobbyPanel.SetActive(false);
            if (mainMenuPanel) mainMenuPanel.SetActive(true);

            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            }
        }

        private void EnterLobby()
        {
            mainMenuPanel.SetActive(false);
            lobbyPanel.SetActive(true);
            lobbyStatusText.text = "Ожидание игроков...";

            // Кнопка старта только у хоста
            if (startGameButton) startGameButton.gameObject.SetActive(IsHost);
        }

        private void OnClientConnected(ulong clientId)
        {
            if (IsServer)
            {
                playersInLobby.Value++;
                UpdateLobbyUIClientRpc(playersInLobby.Value);
            }
        }

        private void OnClientDisconnected(ulong clientId)
        {
            // Защита от ошибки при выключении сервера
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && IsServer)
            {
                playersInLobby.Value--;
                UpdateLobbyUIClientRpc(playersInLobby.Value);
            }
        }

        [ClientRpc]
        private void UpdateLobbyUIClientRpc(int count)
        {
            if (playerCountText) playerCountText.text = $"Игроков: {count}";
        }

        [ClientRpc]
        private void StartGameClientRpc(string seed, int totalPlayers)
        {
            Debug.Log($"[Game] Starting! Seed: {seed}, Players: {totalPlayers}");

            lobbyPanel.SetActive(false);
            gameUIPanel.SetActive(true);

            // 1. Генерация карты (под кол-во игроков)
            if (WorldGenerator.Instance)
                WorldGenerator.Instance.GenerateWorld(seed, totalPlayers);

            // 2. Ресурсы
            if (ResourceSpawner.Instance)
                ResourceSpawner.Instance.SpawnAllResources(seed);

            // 3. Инициализация Игрока (получаем свой ID для спавна на нужном острове)
            int myID = (int)NetworkManager.Singleton.LocalClientId;

            if (GameManager.Instance)
                GameManager.Instance.InitializeGame(myID);
            if (GameManager.Instance != null)
            {
                // Инициализируем GameManager и передаем кол-во игроков
                GameManager.Instance.InitializeGame(totalPlayers);
            }
        }
    }
}