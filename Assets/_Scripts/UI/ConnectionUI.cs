using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using TMPro;
using WarOfCrowns.Core;

namespace WarOfCrowns.UI
{
    public class ConnectionUI : MonoBehaviour
    {
        [Header("Панели")]
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private GameObject lobbyPanel;
        [SerializeField] private GameObject gameUIPanel;

        [Header("Элементы Меню")]
        [SerializeField] private TMP_InputField ipInput;
        [SerializeField] private Button hostBtn;
        [SerializeField] private Button clientBtn;

        [Header("Элементы Лобби")]
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Button startGameBtn;
        [SerializeField] private TextMeshProUGUI worldStatusText; // НОВОЕ ПОЛЕ: Статус мира

        private void Start()
        {
            menuPanel.SetActive(true);
            lobbyPanel.SetActive(false);
            if (gameUIPanel) gameUIPanel.SetActive(false);

            hostBtn.onClick.AddListener(StartHost);
            clientBtn.onClick.AddListener(StartClient);
            startGameBtn.onClick.AddListener(OnStartGameClicked);
        }

        private void Update()
        {
            if (NetworkManager.Singleton == null || (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer))
                return;

            // 1. Обновление Лобби
            if (lobbyPanel.activeSelf && ConnectionManager.Instance != null)
            {
                int count = ConnectionManager.Instance.ConnectedPlayers.Count;
                int max = ConnectionManager.Instance.MaxPlayers;
                statusText.text = $"Игроков: {count} / {max}";

                startGameBtn.gameObject.SetActive(NetworkManager.Singleton.IsServer);

                // Если мы в фазе Loading или Lobby
                if (WorldState.Instance != null)
                {
                    if (WorldState.Instance.CurrentPhase.Value == WorldPhase.Loading)
                    {
                        startGameBtn.interactable = false;
                        if (worldStatusText) worldStatusText.text = "Генерация мира... Подождите.";
                    }
                    else
                    {
                        startGameBtn.interactable = (count >= 1);
                        if (worldStatusText) worldStatusText.text = "Ожидание запуска...";
                    }
                }
            }

            // 2. Переход в игру (ТОЛЬКО КОГДА ФАЗА SETUP ИЛИ GAME)
            if (WorldState.Instance != null)
            {
                WorldPhase phase = WorldState.Instance.CurrentPhase.Value;

                if (phase == WorldPhase.Setup || phase == WorldPhase.Game)
                {
                    // Скрываем лобби, открываем игру
                    if (menuPanel.activeSelf) menuPanel.SetActive(false);
                    if (lobbyPanel.activeSelf) lobbyPanel.SetActive(false);
                    if (gameUIPanel && !gameUIPanel.activeSelf) gameUIPanel.SetActive(true);
                }
            }
        }

        private void StartHost()
        {
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData("0.0.0.0", 7777);
            if (NetworkManager.Singleton.StartHost()) EnterLobbyView();
        }

        private void StartClient()
        {
            string ip = "127.0.0.1";
            if (ipInput != null && !string.IsNullOrEmpty(ipInput.text)) ip = ipInput.text;
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(ip, 7777);
            if (NetworkManager.Singleton.StartClient()) EnterLobbyView();
        }

        private void EnterLobbyView()
        {
            menuPanel.SetActive(false);
            lobbyPanel.SetActive(true);
            statusText.text = "Подключение...";
        }

        private void OnStartGameClicked()
        {
            if (WorldState.Instance != null && ConnectionManager.Instance != null)
            {
                // Передаем кол-во игроков из ConnectionManager
                int total = ConnectionManager.Instance.ConnectedPlayers.Count;
                WorldState.Instance.StartGameSequence(total);
            }
        }
    }
}