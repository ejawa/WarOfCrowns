using UnityEngine;
using Unity.Netcode;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System;

// Структура для хранения информации о найденном сервере
public struct ServerInfo
{
    public string ServerName;
    public IPEndPoint EndPoint;
    public float LastSeenTime;
}

public class NetworkDiscovery : MonoBehaviour
{
    public static NetworkDiscovery Instance;

    // Событие, на которое подпишется UI, чтобы обновлять список серверов
    public event Action<Dictionary<string, ServerInfo>> OnFoundServers;

    private UdpClient udpClient;
    private const int DiscoveryPort = 8888; // Порт для "криков", не для игры
    private const string MagicKey = "WarOfCrowns_Discovery"; // Чтобы отличать наши пакеты от чужих

    private Dictionary<string, ServerInfo> foundServers = new Dictionary<string, ServerInfo>();

    private void Awake()
    {
        Instance = this;
    }

    // --- ЛОГИКА СЕРВЕРА (ХОСТА) ---
    public void StartBroadcasting()
    {
        if (udpClient != null) udpClient.Close();

        udpClient = new UdpClient();
        udpClient.EnableBroadcast = true;

        InvokeRepeating(nameof(BroadcastServerInfo), 0f, 1f);
        Debug.Log("Network Discovery: Started broadcasting server info.");
    }

    private void BroadcastServerInfo()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        string serverName = System.Environment.MachineName; // Имя твоего компа
        string message = $"{MagicKey}:{serverName}";
        byte[] data = Encoding.UTF8.GetBytes(message);

        // Отправляем пакет на всю локальную сеть
        udpClient.Send(data, data.Length, new IPEndPoint(IPAddress.Broadcast, DiscoveryPort));
    }

    // --- ЛОГИКА КЛИЕНТА ---
    public void StartListening()
    {
        // 1. Закрываем старый, если был
        if (udpClient != null)
        {
            udpClient.Close();
            udpClient = null;
        }

        // 2. Создаем новый клиент БЕЗ привязки к порту сразу
        udpClient = new UdpClient();

        // 3. ВАЖНО: Разрешаем использовать порт, даже если он занят другим приложением (например, второй копией игры)
        udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

        // 4. Теперь привязываем к порту 8888
        try
        {
            udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, DiscoveryPort));

            // Начинаем слушать
            udpClient.BeginReceive(OnUdpData, null);
            Debug.Log("Network Discovery: Started listening for servers.");
        }
        catch (SocketException e)
        {
            Debug.LogError($"NetworkDiscovery: Не удалось занять порт {DiscoveryPort}. Ошибка: {e.Message}");
        }
    }

    private void OnUdpData(IAsyncResult result)
    {
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
        byte[] receivedBytes = udpClient.EndReceive(result, ref remoteEndPoint);
        string message = Encoding.UTF8.GetString(receivedBytes);

        if (message.StartsWith(MagicKey))
        {
            string[] parts = message.Split(':');
            string serverName = parts[1];

            // Если сервер уже в списке, обновляем время. Если нет - добавляем.
            if (!foundServers.ContainsKey(remoteEndPoint.Address.ToString()))
            {
                Debug.Log($"Found new server: {serverName} at {remoteEndPoint.Address}");
            }
            foundServers[remoteEndPoint.Address.ToString()] = new ServerInfo
            {
                ServerName = serverName,
                EndPoint = remoteEndPoint,
                LastSeenTime = Time.time
            };
        }

        // Продолжаем слушать
        udpClient.BeginReceive(OnUdpData, null);
    }

    private void Update()
    {
        // Проверяем, не "протухли" ли серверы
        if (foundServers.Count > 0)
        {
            bool listChanged = false;
            List<string> serversToRemove = new List<string>();

            foreach (var server in foundServers.Values)
            {
                if (Time.time - server.LastSeenTime > 3f) // Если не было вестей 3 секунды
                {
                    serversToRemove.Add(server.EndPoint.Address.ToString());
                    listChanged = true;
                }
            }

            foreach (var key in serversToRemove)
            {
                foundServers.Remove(key);
                Debug.Log($"Server {key} timed out.");
            }

            // Если список изменился, сообщаем UI
            if (listChanged || Time.frameCount % 30 == 0) // Обновляем UI раз в полсекунды
                OnFoundServers?.Invoke(foundServers);
        }
    }

    public void StopDiscovery()
    {
        CancelInvoke();
        if (udpClient != null)
        {
            udpClient.Close();
            udpClient = null;
        }
        foundServers.Clear();
        OnFoundServers?.Invoke(foundServers); // Очистить UI
        Debug.Log("Network Discovery: Stopped.");
    }

    private void OnDestroy()
    {
        StopDiscovery();
    }
}