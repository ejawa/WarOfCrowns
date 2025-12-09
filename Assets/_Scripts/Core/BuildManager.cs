using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using Unity.Netcode;
using WarOfCrowns.Buildings;
using WarOfCrowns.World;

namespace WarOfCrowns.Core
{
    public class BuildManager : NetworkBehaviour
    {
        public static bool IsInBuildMode { get; private set; }

        [Header("Префабы")]
        public List<GameObject> buildableFoundations;
        public GameObject townHallPrefab;

        [Header("Правила")]
        public LayerMask obstacleLayer;
        public LayerMask resourcesToClearLayer;
        public float clearanceModifier = 0.8f;

        [Header("Визуал")]
        public Color validColor = new Color(0.2f, 1f, 0.2f, 0.6f);
        public Color invalidColor = new Color(1f, 0.2f, 0.2f, 0.6f);

        private GameObject _ghostInstance;
        private GameObject _foundationToBuild;
        private bool _isBuildingMode;
        private bool _isInitialSetup;
        private SpriteRenderer[] _allGhostRenderers;

        // Для отладки
        private List<Vector3Int> _debugCheckedTiles = new List<Vector3Int>();
        private List<bool> _debugTileStatus = new List<bool>();

        private void Update()
        {
            IsInBuildMode = _isBuildingMode;

            if (WorldState.Instance == null) return;
            if (WorldState.Instance.CurrentPhase.Value == WorldPhase.Setup ||
                WorldState.Instance.CurrentPhase.Value == WorldPhase.Game)
            {
                if (_isBuildingMode) HandleGhost();
            }
            else
            {
                if (_isBuildingMode) ExitBuildMode();
            }
        }

        public void EnterBuildMode(GameObject p, bool isInitial = false)
        {
            if (_isBuildingMode) ExitBuildMode();

            _foundationToBuild = p;
            if (!_foundationToBuild) return;

            _isBuildingMode = true;
            _isInitialSetup = isInitial;

            _ghostInstance = Instantiate(p);

            var bData = p.GetComponent<Building>();
            Sprite iconToUse = (bData != null) ? bData.buildingIcon : null;

            if (iconToUse != null)
            {
                Transform iconSlot = _ghostInstance.transform.Find("Icon_Overlay");
                if (iconSlot == null) iconSlot = _ghostInstance.transform.Find("Icon");

                if (iconSlot != null && iconSlot.TryGetComponent(out SpriteRenderer iconSR))
                {
                    iconSR.sprite = iconToUse;
                    iconSlot.gameObject.SetActive(true);
                }
            }

            _allGhostRenderers = _ghostInstance.GetComponentsInChildren<SpriteRenderer>();

            foreach (var c in _ghostInstance.GetComponentsInChildren<Collider2D>()) Destroy(c);
            if (_ghostInstance.GetComponent<NetworkObject>()) Destroy(_ghostInstance.GetComponent<NetworkObject>());
            if (_ghostInstance.GetComponent<ConstructionSite>()) Destroy(_ghostInstance.GetComponent<ConstructionSite>());
            if (_ghostInstance.GetComponent<Building>()) Destroy(_ghostInstance.GetComponent<Building>());
            // Footprint на призраке нам не нужен для логики, но можно оставить для визуализации
        }

        private void HandleGhost()
        {
            if (Mouse.current == null) return;
            Vector3 m = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            m.z = 0;

            if (_ghostInstance)
            {
                // Привязка к сетке (опционально, если хочешь чтобы здания вставали ровно по клеткам)
                // m = new Vector3(Mathf.Round(m.x), Mathf.Round(m.y), 0);

                _ghostInstance.transform.position = m;
                bool v = IsValidPlacement(m, _foundationToBuild);

                Color targetColor = v ? validColor : invalidColor;
                if (_allGhostRenderers != null)
                {
                    foreach (var sr in _allGhostRenderers) if (sr != null) sr.color = targetColor;
                }

                if (Mouse.current.leftButton.wasPressedThisFrame && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject() && v)
                {
                    if (_isInitialSetup) RequestPlaceTownHallServerRpc(m);
                    else
                    {
                        int i = buildableFoundations.IndexOf(_foundationToBuild);
                        if (i != -1) RequestPlaceFoundationServerRpc(i, m);
                    }
                    ExitBuildMode();
                }
            }

            if (!_isInitialSetup && Mouse.current.rightButton.wasPressedThisFrame) ExitBuildMode();
        }

        private void ExitBuildMode()
        {
            _isBuildingMode = false;
            _isInitialSetup = false;
            IsInBuildMode = false;
            if (_ghostInstance) Destroy(_ghostInstance);
            _foundationToBuild = null;
            _allGhostRenderers = null;
            _debugCheckedTiles.Clear();
        }

        public bool IsValidPlacement(Vector3 p, GameObject o)
        {
            _debugCheckedTiles.Clear();
            _debugTileStatus.Clear();

            if (!o) return false;

            // --- 1. ОПРЕДЕЛЯЕМ ЗОНУ ПРОВЕРКИ ---
            Vector2 checkSize;
            Vector2 checkOffset;

            // Пытаемся найти наш новый скрипт ручной настройки
            var footprint = o.GetComponent<BuildingFootprint>();

            if (footprint != null)
            {
                // Если есть Footprint - верим ему
                checkSize = new Vector2(footprint.width, footprint.height);
                checkOffset = footprint.offset;
            }
            else
            {
                // Иначе по старинке берем коллайдер
                var c = o.GetComponent<BoxCollider2D>();
                checkSize = c ? c.size : new Vector2(1, 1);
                checkOffset = c ? c.offset : Vector2.zero;
            }

            // Центр проверки в мире
            Vector3 center = p + (Vector3)checkOffset;

            // --- 2. ФИЗИЧЕСКИЕ ПРЕПЯТСТВИЯ ---
            // Немного уменьшаем (0.9), чтобы можно было строить вплотную к другим зданиям
            if (Physics2D.OverlapBox(center, checkSize * 0.9f, 0, obstacleLayer))
            {
                return false;
            }

            // --- 3. ПРОВЕРКА ТАЙЛОВ ---
            if (WorldGenerator.Instance && WorldGenerator.Instance.baseTilemap != null)
            {
                var tilemap = WorldGenerator.Instance.baseTilemap;

                // Вычисляем углы зоны проверки
                Vector3 minPos = center - (Vector3)(checkSize / 2f);

                // Чтобы захватить тайлы корректно, мы идем от нижнего левого угла
                // и шагаем на +1 тайл вправо и вверх

                // Переводим нижний левый угол в координаты сетки
                // Добавляем маленький отступ (0.1), чтобы не захватить соседний тайл, если стоим на границе
                Vector3Int startCell = tilemap.WorldToCell(minPos + new Vector3(0.1f, 0.1f, 0));

                // Проходим ровно столько тайлов, сколько указано в Width/Height
                // Если Width = 4, то проверяем: x, x+1, x+2, x+3
                // Используем Mathf.CeilToInt для размера, если он дробный (но лучше используй int в Footprint)
                int w = Mathf.CeilToInt(checkSize.x);
                int h = Mathf.CeilToInt(checkSize.y);

                for (int x = 0; x < w; x++)
                {
                    for (int y = 0; y < h; y++)
                    {
                        Vector3Int cellToCheck = startCell + new Vector3Int(x, y, 0);

                        bool isOk = WorldGenerator.Instance.IsCellBuildable(cellToCheck);

                        // Сохраняем для отладки
                        _debugCheckedTiles.Add(cellToCheck);
                        _debugTileStatus.Add(isOk);

                        if (!isOk) return false;
                    }
                }
            }

            // --- 4. ПРОВЕРКА ВЛАДЕНИЯ ---
            if (Kingdom.PlayerKingdom && WorldGenerator.Instance)
            {
                int myKingdomID = Kingdom.PlayerKingdom.kingdomID.Value;
                int tileOwnerID = WorldGenerator.Instance.GetKingdomIDAtPosition(p);

                if (myKingdomID != -1 && tileOwnerID != myKingdomID) return false;

                if (myKingdomID == -1)
                {
                    var localPlayer = ConnectionManager.Instance.GetLocalPlayer();
                    if (localPlayer != null && tileOwnerID != localPlayer.Value.KingdomId)
                        return false;
                }
            }

            return true;
        }

        // РИСУЕМ ОТЛАДКУ
        private void OnDrawGizmos()
        {
            if (_debugCheckedTiles == null || _debugCheckedTiles.Count == 0) return;

            for (int i = 0; i < _debugCheckedTiles.Count; i++)
            {
                // Зеленый если можно, Красный если нельзя
                Gizmos.color = _debugTileStatus[i] ? new Color(0, 1, 0, 0.3f) : new Color(1, 0, 0, 0.5f);
                // Рисуем кубик в центре тайла
                Vector3 pos = new Vector3(_debugCheckedTiles[i].x + 0.5f, _debugCheckedTiles[i].y + 0.5f, 0);
                Gizmos.DrawCube(pos, Vector3.one * 0.9f);
            }
        }

        // ... (RPC методы без изменений, скопируй из прошлого ответа) ...
        [ServerRpc(RequireOwnership = false)]
        private void RequestPlaceFoundationServerRpc(int idx, Vector3 pos, ServerRpcParams rpc = default)
        {
            if (idx < 0 || idx >= buildableFoundations.Count) return;
            ClearResourcesOnServer(pos, buildableFoundations[idx]);
            GameObject obj = Instantiate(buildableFoundations[idx], pos, Quaternion.identity);
            obj.GetComponent<NetworkObject>().Spawn();
            int ownerId = (int)rpc.Receive.SenderClientId;
            obj.GetComponent<Building>().SetOwnerID(ownerId);
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestPlaceTownHallServerRpc(Vector3 pos, ServerRpcParams rpc = default)
        {
            if (townHallPrefab == null) return;
            ClearResourcesOnServer(pos, townHallPrefab);
            GameObject obj = Instantiate(townHallPrefab, pos, Quaternion.identity);
            obj.GetComponent<NetworkObject>().Spawn();
            int ownerId = (int)rpc.Receive.SenderClientId;

            var b = obj.GetComponent<Building>();
            if (b != null)
            {
                b.SetOwnerID(ownerId);
                b.CheckPopulationRegistration();
            }

            if (GameFlowManager.Instance != null)
                GameFlowManager.Instance.OnTownHallPlaced(rpc.Receive.SenderClientId, pos);
        }

        private void ClearResourcesOnServer(Vector3 pos, GameObject prefab)
        {
            var col = prefab.GetComponent<Collider2D>();
            Vector2 size = col ? col.bounds.size : new Vector2(2, 2);
            Collider2D[] hits = Physics2D.OverlapBoxAll(pos, size * clearanceModifier, 0f, resourcesToClearLayer);
            foreach (var h in hits)
            {
                ResourceNode node = h.GetComponentInParent<ResourceNode>();
                if (node != null)
                {
                    if (node.TryGetComponent(out NetworkObject netObj)) netObj.Despawn();
                    else Destroy(node.gameObject);
                }
            }
        }
    }
}