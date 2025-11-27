using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using TMPro;

// Ïîäêëþ÷àåì ïðîñòðàíñòâî èìåí äëÿ òâîåãî òðàíñïîðòà
using Unity.Netcode.Transports.UTP;

public class NetworkConnectionTest : MonoBehaviour
{
    [SerializeField] private Button hostBtn;
    [SerializeField] private Button clientBtn;
    [SerializeField] private TMP_InputField ipInput;
    [SerializeField] private TextMeshProUGUI statusText;

    private void Start()
    {
        Debug.Log("--- ÇÀÏÓÙÅÍÀ ÂÅÐÑÈß v3 (ÑÎÂÌÅÑÒÈÌÀß) ---");
        statusText.text = "Ready.";

        hostBtn.onClick.AddListener(() => {
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

            // --- ÏÐÀÂÈËÜÍÛÉ ÑÏÎÑÎÁ ÄËß ÒÂÎÅÉ ÂÅÐÑÈÈ ---
            // Ìû íàïðÿìóþ ìåíÿåì ïîëÿ â ConnectionData. Ýòî äîëæíî ðàáîòàòü.
            transport.SetConnectionData("0.0.0.0", 7777);
            // ------------------------------------------

            statusText.text = "Starting Host on 0.0.0.0...";
            Debug.Log("Attempting to start Host on 0.0.0.0 using SetConnectionData(ip, port)...");
            NetworkManager.Singleton.StartHost();
        });

        clientBtn.onClick.AddListener(() => {
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            string ip = "127.0.0.1";

            if (ipInput != null && !string.IsNullOrEmpty(ipInput.text))
            {
                ip = ipInput.text;
            }

            // --- ÏÐÀÂÈËÜÍÛÉ ÑÏÎÑÎÁ ÄËß ÊËÈÅÍÒÀ ---
            transport.SetConnectionData(ip, 7777);
            // ---------------------------------------

            statusText.text = $"Attempting to connect to {ip}...";
            Debug.Log($"Attempting to connect to {ip}...");
            NetworkManager.Singleton.StartClient();
        });

        // ... (Êîëëáýêè OnClientConnected/Disconnected îñòàþòñÿ áåç èçìåíåíèé) ...
        NetworkManager.Singleton.OnClientConnectedCallback += (clientId) => {
            statusText.text = $"SUCCESS! Client connected. My ID: {clientId}";
            Debug.Log($"SUCCESS! Client connected. ID: {clientId}");
        };

        NetworkManager.Singleton.OnClientDisconnectCallback += (clientId) => {
            if (!NetworkManager.Singleton.IsServer && NetworkManager.Singleton.LocalClientId == clientId)
            {
                statusText.text = "FAILED to connect to host.";
                Debug.LogError("FAILED to connect to host. Check IP, Port, Firewall.");
            }
        };
    }
}