using UnityEngine;
using Unity.Netcode;
using System.Collections;
using WarOfCrowns.World;
using WarOfCrowns.Units;
using WarOfCrowns.Buildings;
using System.Collections.Generic;

namespace WarOfCrowns.Core
{
    public class GameFlowManager : MonoBehaviour
    {
        public static GameFlowManager Instance { get; private set; }

        [Header("Настройки старта")]
        public GameObject peasantPrefab;
        public int startingPeasants = 10;
        public GameObject townHallGhostPrefab;
        public GameObject townHallPrefab;

        // --- НОВОЕ: Радиус спавна юнитов вокруг мэрии ---
        [Header("Спавн")]
        [Tooltip("На каком расстоянии от центра Мэрии появляются крестьяне")]
        public float unitSpawnRadius = 8f;

        private bool _isWorldGenerated = false;
        private Camera _mainCamera;
        private Dictionary<ulong, Vector3> _pendingTownHalls = new Dictionary<ulong, Vector3>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _mainCamera = Camera.main;
        }

        private void Start()
        {
            StartCoroutine(WaitForWorldState());
        }

        private IEnumerator WaitForWorldState()
        {
            while (WorldState.Instance == null) yield return null;
            WorldState.Instance.CurrentPhase.OnValueChanged += OnPhaseChanged;
            if (WorldState.Instance.CurrentPhase.Value != WorldPhase.Lobby && !_isWorldGenerated)
            {
                StartCoroutine(GenerateWorldRoutine());
            }
        }

        private void OnDestroy()
        {
            if (WorldState.Instance != null)
                WorldState.Instance.CurrentPhase.OnValueChanged -= OnPhaseChanged;
        }

        private void OnPhaseChanged(WorldPhase oldPhase, WorldPhase newPhase)
        {
            if (newPhase == WorldPhase.Loading && !_isWorldGenerated)
            {
                StartCoroutine(GenerateWorldRoutine());
            }
            else if (newPhase == WorldPhase.Setup)
            {
                StartCoroutine(SetupPhaseRoutine());
            }
        }

        private IEnumerator GenerateWorldRoutine()
        {
            while (WorldState.Instance.MapSeed.Value == 0) yield return null;
            int seed = WorldState.Instance.MapSeed.Value;

            if (WorldGenerator.Instance != null)
            {
                int playersCount = ConnectionManager.Instance.ConnectedPlayers.Count;
                if (playersCount == 0) playersCount = 2;

                WorldGenerator.Instance.GenerateWorld(seed.ToString(), playersCount);
                while (!WorldGenerator.Instance.IsWorldGenerated) yield return null;
            }

            _isWorldGenerated = true;

            if (NetworkManager.Singleton.IsServer)
            {
                yield return new WaitForSeconds(0.5f);
                if (ResourceSpawner.Instance != null)
                {
                    ResourceSpawner.Instance.SpawnAllResources(seed.ToString());

                    // --- НОВОЕ: ОЖИДАНИЕ ЗАВЕРШЕНИЯ СПАВНА ---
                    while (!ResourceSpawner.Instance.IsSpawningComplete)
                    {
                        yield return null; // Ждем, пока спавнер не закончит
                    }
                    // ------------------------------------------
                }

                WorldState.Instance.MoveToSetup();
            }
        }

        private IEnumerator SetupPhaseRoutine()
        {
            while (!_isWorldGenerated) yield return null;

            var localPlayer = ConnectionManager.Instance.GetLocalPlayer();
            while (localPlayer == null)
            {
                localPlayer = ConnectionManager.Instance.GetLocalPlayer();
                yield return null;
            }

            while (Kingdom.PlayerKingdom == null || Kingdom.PlayerKingdom.kingdomID.Value == -1) yield return null;

            int myID = localPlayer.Value.KingdomId;
            Vector3 spawnPos = WorldGenerator.Instance.GetSpawnPosition(myID);

            if (_mainCamera != null)
                _mainCamera.transform.position = new Vector3(spawnPos.x, spawnPos.y, -10);

            var bm = FindObjectOfType<BuildManager>();
            if (bm != null)
            {
                GameObject ghostToUse = townHallGhostPrefab != null ? townHallGhostPrefab : townHallPrefab;
                bm.EnterBuildMode(ghostToUse, true);
            }
        }

        public void OnTownHallPlaced(ulong clientId, Vector3 pos)
        {
            if (!NetworkManager.Singleton.IsServer) return;

            if (!_pendingTownHalls.ContainsKey(clientId))
                _pendingTownHalls.Add(clientId, pos);

            if (_pendingTownHalls.Count >= WorldState.Instance.TotalPlayers.Value)
            {
                GlobalMatchStart();
            }
        }

        private void GlobalMatchStart()
        {
            foreach (var kvp in _pendingTownHalls)
            {
                ulong clientId = kvp.Key;
                Vector3 pos = kvp.Value;

                Kingdom k = null;
                foreach (var kingdom in FindObjectsOfType<Kingdom>())
                {
                    if (kingdom.OwnerClientId == clientId) { k = kingdom; break; }
                }

                if (k != null)
                {
                    k.GrantStartingResources();
                    SpawnUnits(clientId, k.kingdomID.Value, pos);
                }
            }
            WorldState.Instance.MoveToGame();
        }

        private void SpawnUnits(ulong clientId, int kingdomID, Vector3 townHallPos)
        {
            float searchRadius = 35f; // Чуть увеличим радиус
            Collider2D[] hits = Physics2D.OverlapCircleAll(townHallPos, searchRadius);

            List<ResourceNode> priorityNodes = new List<ResourceNode>();

            foreach (var hit in hits)
            {
                var node = hit.GetComponent<ResourceNode>();
                if (node != null)
                {
                    string rName = node.resourceType.ToString();
                    // Ищем дерево и еду
                    if (rName.Contains("Wood") || rName.Contains("Berr") || rName.Contains("Food"))
                    {
                        priorityNodes.Add(node);
                    }
                }
            }

            // Сортируем от ближних к дальним
            priorityNodes.Sort((a, b) => Vector3.Distance(townHallPos, a.transform.position)
                .CompareTo(Vector3.Distance(townHallPos, b.transform.position)));

            // --- НОВОЕ: Словарь для виртуального учета отправленных рабочих ---
            // Key: Ресурс, Value: Сколько мы только что отправили
            Dictionary<ResourceNode, int> pendingAssignments = new Dictionary<ResourceNode, int>();
            // ----------------------------------------------------------------

            for (int i = 0; i < startingPeasants; i++)
            {
                float angle = i * (360f / startingPeasants);
                Vector3 offset = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * unitSpawnRadius;
                Vector3 spawnPos = townHallPos + offset;

                GameObject p = Instantiate(peasantPrefab, spawnPos, Quaternion.identity);
                var netObj = p.GetComponent<NetworkObject>();

                if (netObj != null)
                {
                    netObj.SpawnWithOwnership(clientId);
                    var unit = p.GetComponent<Unit>();
                    if (unit != null)
                    {
                        unit.ownerKingdomID.Value = kingdomID;
                        unit.ForceUpdateKingdomReferenceServer();

                        if (NetworkManager.Singleton.IsServer)
                        {
                            ResourceNode targetNode = null;

                            // Перебираем ресурсы и ищем свободный С УЧЕТОМ тех, кого мы только что отправили
                            foreach (var node in priorityNodes)
                            {
                                if (node == null) continue;

                                int alreadyThere = node.CurrentWorkers; // Реально занято
                                int weJustSent = pendingAssignments.ContainsKey(node) ? pendingAssignments[node] : 0; // Мысленно занято

                                if (alreadyThere + weJustSent < node.maxWorkers)
                                {
                                    targetNode = node;

                                    // Записываем в виртуальный учет
                                    if (!pendingAssignments.ContainsKey(node)) pendingAssignments[node] = 0;
                                    pendingAssignments[node]++;

                                    break; // Нашли!
                                }
                            }

                            if (targetNode != null)
                            {
                                StartCoroutine(AutoAssignDelay(unit, targetNode));
                            }
                        }
                    }
                }
            }
        }

        // Небольшая задержка, чтобы юнит прогрузился перед получением приказа
        private IEnumerator AutoAssignDelay(Unit unit, ResourceNode node)
        {
            yield return null; // Ждем 1 кадр
            if (unit != null && node != null && unit.TryGetComponent<UnitAI>(out var ai))
            {
                ai.CommandGather(node);
            }
        }
    }
}