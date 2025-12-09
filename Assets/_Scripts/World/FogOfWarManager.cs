using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;
using WarOfCrowns.World;
using WarOfCrowns.Units;

namespace WarOfCrowns.Core
{
    public class FogOfWarManager : MonoBehaviour
    {
        public static FogOfWarManager Instance { get; private set; }

        [Header("Настройки")]
        [SerializeField] private Tilemap shroudTilemap;
        [SerializeField] private Tilemap fogTilemap;
        [SerializeField] private TileBase fogTileAsset;

        [Header("Параметры")]
        [SerializeField] private float updateRate = 0.2f;
        [SerializeField] private int baseVisionRadius = 8;

        private float _timer;
        private HashSet<Vector3Int> _exploredTiles = new HashSet<Vector3Int>();
        private HashSet<Vector3Int> _currentlyVisibleTiles = new HashSet<Vector3Int>();
        private bool _isInitialized = false;

        private void Awake() { Instance = this; }

        private void Start()
        {
            FillMapWithShroud();
            StartCoroutine(InitializeRoutine());
        }

        private IEnumerator InitializeRoutine()
        {
            // Ждем генерации карты
            while (WorldGenerator.Instance == null || !WorldGenerator.Instance.IsWorldGenerated)
            {
                yield return new WaitForSeconds(0.1f);
            }

            // Ждем инициализации игрока
            while (Kingdom.PlayerKingdom == null || Kingdom.PlayerKingdom.kingdomID.Value == -1)
            {
                yield return new WaitForSeconds(0.1f);
            }

            // Инициализация
            RevealMyTerritory();
            _isInitialized = true;
        }

        private void RevealMyTerritory()
        {
            if (WorldGenerator.Instance == null || Kingdom.PlayerKingdom == null) return;

            int myID = Kingdom.PlayerKingdom.kingdomID.Value;
            int w = WorldGenerator.Instance.width;
            int h = WorldGenerator.Instance.height;
            int halfW = w / 2;
            int halfH = h / 2;

            for (int x = -halfW; x < halfW; x++)
            {
                for (int y = -halfH; y < halfH; y++)
                {
                    Vector3 worldPos = new Vector3(x, y, 0);
                    // Определяем, чей это остров, по координатам
                    int tileOwnerID = WorldGenerator.Instance.GetKingdomIDAtPosition(worldPos);
                    Vector3Int cellPos = new Vector3Int(x, y, 0);

                    // Если это мой остров - убираем черный туман
                    if (tileOwnerID == myID)
                    {
                        if (shroudTilemap) shroudTilemap.SetTile(cellPos, null);
                        if (fogTilemap) fogTilemap.SetTile(cellPos, fogTileAsset);
                        _exploredTiles.Add(cellPos);
                    }
                    else
                    {
                        // Если чужой - заливаем черным
                        if (shroudTilemap) shroudTilemap.SetTile(cellPos, fogTileAsset);
                    }
                }
            }
        }

        private void Update()
        {
            if (!_isInitialized) return;
            _timer += Time.deltaTime;
            if (_timer >= updateRate)
            {
                _timer = 0;
                UpdateFog();
            }
        }

        public bool IsVisible(Vector3 worldPos)
        {
            if (shroudTilemap == null) return true;
            Vector3Int cellPos = shroudTilemap.WorldToCell(worldPos);
            return _currentlyVisibleTiles.Contains(cellPos);
        }

        private void UpdateFog()
        {
            if (Kingdom.PlayerKingdom == null || PopulationManager.Instance == null) return;

            int myKingdomID = Kingdom.PlayerKingdom.kingdomID.Value;
            HashSet<Vector3Int> newVisibleTiles = new HashSet<Vector3Int>();

            // Юниты
            if (PopulationManager.Instance.AllUnits != null)
            {
                foreach (var unit in PopulationManager.Instance.AllUnits)
                {
                    if (unit != null && unit.ownerKingdomID.Value == myKingdomID)
                    {
                        int radius = baseVisionRadius;
                        if (unit.TryGetComponent<UnitAI>(out var ai)) radius = Mathf.CeilToInt(ai.viewRadius);
                        AddVisibleTiles(unit.transform.position, radius, newVisibleTiles);
                    }
                }
            }

            // Здания
            var buildings = FindObjectsOfType<Buildings.Building>();
            foreach (var b in buildings)
            {
                if (b.GetComponent<Unity.Netcode.NetworkObject>() == null || !b.GetComponent<Unity.Netcode.NetworkObject>().IsSpawned) continue;
                if (b.ownerKingdomID.Value == myKingdomID)
                {
                    AddVisibleTiles(b.transform.position, baseVisionRadius + 2, newVisibleTiles);
                }
            }

            // Обновление тайлов
            foreach (var pos in _currentlyVisibleTiles)
            {
                if (!newVisibleTiles.Contains(pos))
                    if (fogTilemap && _exploredTiles.Contains(pos)) fogTilemap.SetTile(pos, fogTileAsset);
            }

            foreach (var pos in newVisibleTiles)
            {
                if (!_exploredTiles.Contains(pos))
                {
                    if (shroudTilemap) shroudTilemap.SetTile(pos, null);
                    _exploredTiles.Add(pos);
                }
                if (fogTilemap) fogTilemap.SetTile(pos, null);
            }

            _currentlyVisibleTiles = newVisibleTiles;
        }

        private void AddVisibleTiles(Vector3 center, int radius, HashSet<Vector3Int> tileSet)
        {
            if (shroudTilemap == null) return;
            Vector3Int centerCell = shroudTilemap.WorldToCell(center);
            for (int x = -radius; x <= radius; x++)
                for (int y = -radius; y <= radius; y++)
                    if (x * x + y * y <= radius * radius)
                        tileSet.Add(new Vector3Int(centerCell.x + x, centerCell.y + y, 0));
        }

        private void FillMapWithShroud()
        {
            int w = 512; int h = 512;
            if (WorldGenerator.Instance != null) { w = WorldGenerator.Instance.width; h = WorldGenerator.Instance.height; }
            int halfW = w / 2; int halfH = h / 2;
            if (shroudTilemap != null) shroudTilemap.BoxFill(Vector3Int.zero, fogTileAsset, -halfW - 10, -halfH - 10, halfW + 10, halfH + 10);
        }
    }
}