using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using WarOfCrowns.Buildings;
using WarOfCrowns.Units;
using WarOfCrowns.World;
using System.Collections.Generic;

namespace WarOfCrowns.Core
{
    public enum GameState { PreGame, Setup, Playing }

    public class GameManager : NetworkBehaviour
    {
        public static GameManager Instance { get; private set; }

        // Синхронизация состояния игры
        public NetworkVariable<GameState> CurrentState = new NetworkVariable<GameState>(GameState.PreGame);

        // Количество игроков в матче (чтобы знать, скольких ждать)
        public NetworkVariable<int> TotalPlayersInMatch = new NetworkVariable<int>(2);

        // Серверные переменные для отслеживания готовности
        private int _playersReadyCount = 0;
        private Dictionary<ulong, Vector3> _townHallPositions = new Dictionary<ulong, Vector3>();

        [Header("Ссылки")]
        [SerializeField] private UnitSelectionController selectionController;
        [SerializeField] private BuildManager buildManager;

        [Header("Базы Данных")]
        [SerializeField] private NameDatabase nameDatabase;
        [SerializeField] private AppearanceDatabase appearanceDatabase;
        public AppearanceDatabase AppearanceDB => appearanceDatabase;

        [Header("Префабы")]
        [SerializeField] private GameObject townHallGhostPrefab; // Призрак
        public GameObject townHallPrefab; // Публичный, чтобы BuildManager мог его взять
        public GameObject peasantPrefab;  // Публичный, для спавна

        [Header("Настройки")]
        [SerializeField] private float setupTime = 60f;
        [SerializeField] private int startingPeasants = 10;

        // Локальные переменные
        private GameObject _currentGhost;
        private SpriteRenderer _ghostRenderer;
        private float _timer;
        private Camera _mainCamera;
        private Vector3 _mySpawnPos;

        public override void OnNetworkSpawn()
        {
            Instance = this;
            _mainCamera = Camera.main;
            if (buildManager == null) buildManager = FindObjectOfType<BuildManager>();
        }

        // --- ОБНОВЛЕННЫЙ МЕТОД ИНИЦИАЛИЗАЦИИ ---
        public void InitializeGame(int totalPlayers)
        {
            // 1. Инициализируем локальное королевство
            if (Kingdom.PlayerKingdom != null)
            {
                // Используем свой ClientId как ID королевства (0, 1, 2...)
                int myID = (int)NetworkManager.Singleton.LocalClientId;
                Kingdom.PlayerKingdom.InitializeKingdomLogic(myID);
                Kingdom.PlayerKingdom.ResetPopulationLogic(); // Сбрасываем, чтобы было 0/0 до старта
            }

            // 2. Если мы сервер — запоминаем, сколько игроков ждать
            if (IsServer)
            {
                TotalPlayersInMatch.Value = totalPlayers;
                CurrentState.Value = GameState.Setup;
                _playersReadyCount = 0;
                _townHallPositions.Clear();
            }

            // 3. Запускаем выбор места
            StartSetupPhase();
        }
        // ----------------------------------------

        private void Update()
        {
            // Логика призрака работает локально, пока идет фаза Setup
            if (CurrentState.Value == GameState.Setup)
            {
                UpdateSetupPhase_Local();
            }
        }

        private void StartSetupPhase()
        {
            // --- ЗАЩИТА ОТ ДВОЙНОГО ЗАПУСКА ---
            if (_currentGhost != null)
            {
                return; // У нас уже есть призрак, не надо создавать второго!
            }
            // ----------------------------------

            CurrentState.Value = GameState.Playing; // <--- И ТУТ
            if (selectionController) selectionController.enabled = false;
            _timer = setupTime;

            int myID = (Kingdom.PlayerKingdom != null) ? Kingdom.PlayerKingdom.kingdomID : 0;
            _mySpawnPos = Vector3.zero;

            if (WorldGenerator.Instance != null)
                _mySpawnPos = WorldGenerator.Instance.GetSpawnPosition(myID);

            if (_mainCamera != null)
                _mainCamera.transform.position = new Vector3(_mySpawnPos.x, _mySpawnPos.y, -10);

            if (townHallGhostPrefab != null)
            {
                _currentGhost = Instantiate(townHallGhostPrefab, _mySpawnPos, Quaternion.identity);
                if (_currentGhost.TryGetComponent(out SpriteRenderer sr)) _ghostRenderer = sr;
            }
        }

        private void UpdateSetupPhase_Local()
        {
            if (_currentGhost == null) return;

            Vector3 mPos = _mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            mPos.z = 0;
            _currentGhost.transform.position = mPos;

            bool isValid = buildManager && buildManager.IsValidPlacement(mPos, townHallPrefab);

            // Проверка радиуса (чтобы не строить у соседа)
            if (Vector3.Distance(mPos, _mySpawnPos) > 120f) isValid = false;

            if (_ghostRenderer) _ghostRenderer.color = isValid ? new Color(0, 1, 0, 0.6f) : new Color(1, 0, 0, 0.6f);

            _timer -= Time.deltaTime;

            if ((_timer <= 0 || (Mouse.current.leftButton.wasPressedThisFrame && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())) && isValid)
            {
                // Отправляем запрос на сервер через BuildManager
                if (buildManager != null)
                {
                    buildManager.RequestPlaceInitialTownHall(mPos, townHallPrefab);
                }

                // Удаляем призрака и ждем старта игры
                Destroy(_currentGhost);
                _currentGhost = null;
                Debug.Log("Место выбрано. Ожидание остальных игроков...");
            }
        }

        // --- ЛОГИКА СЕРВЕРА (Синхронный старт) ---

        // Вызывается из BuildManager, когда игрок поставил Мэрию
        public void RegisterPlayerReady(ulong clientId, Vector3 townHallPos)
        {
            if (!IsServer) return;

            if (!_townHallPositions.ContainsKey(clientId))
            {
                _townHallPositions.Add(clientId, townHallPos);
                _playersReadyCount++;
                Debug.Log($"[Server] Игрок {clientId} готов! ({_playersReadyCount}/{TotalPlayersInMatch.Value})");
            }

            // Если ВСЕ игроки поставили Мэрии -> ЗАПУСКАЕМ ИГРУ
            if (_playersReadyCount >= TotalPlayersInMatch.Value)
            {
                Debug.Log("[Server] Все готовы! Спавн юнитов и старт игры.");

                SpawnAllStartingUnits();

                // Меняем состояние на Playing (это увидит каждый клиент в Update)
                CurrentState.Value = GameState.Playing;

                // Разблокируем управление всем клиентам
                UnlockControlsClientRpc();
            }
        }

        private void SpawnAllStartingUnits()
        {
            if (!IsServer) return;

            foreach (var kvp in _townHallPositions)
            {
                ulong playerId = kvp.Key;
                Vector3 pos = kvp.Value;
                int kingdomID = (int)playerId; // 0, 1, 2...

                for (int i = 0; i < startingPeasants; i++)
                {
                    float angle = i * (360f / startingPeasants);
                    Vector3 off = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * 5f;

                    GameObject p = Instantiate(peasantPrefab, pos + off, Quaternion.identity);
                    var netObj = p.GetComponent<NetworkObject>();

                    if (netObj != null)
                    {
                        // Отдаем владение игроку
                        netObj.SpawnWithOwnership(playerId);
                    }

                    var unit = p.GetComponent<Unit>();
                    if (unit != null)
                    {
                        // Назначаем ID королевства
                        unit.ownerKingdomID.Value = kingdomID;
                    }
                }
            }
        }

        [ClientRpc]
        private void UnlockControlsClientRpc()
        {
            Debug.Log("ВСЕ ГОТОВЫ! Игра началась.");

            if (selectionController != null) selectionController.enabled = true;

            // Устанавливаем визуальный лимит населения (реальный - в зданиях)
            if (PopulationManager.Instance != null)
                PopulationManager.Instance.SetInitialPopulation(0, 10);
        }

        // Вспомогательные методы
        public string GetRandomFullName(Gender g) => nameDatabase ? nameDatabase.GetRandomName(g) : "Unnamed";
        public Sprite GetRandomPortrait(Gender g) => nameDatabase ? nameDatabase.GetRandomPortrait(g) : null;
    }
}