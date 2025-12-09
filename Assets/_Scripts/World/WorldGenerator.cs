using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

namespace WarOfCrowns.World
{
    [System.Serializable]
    public class BiomeLayer
    {
        public string name;
        [Range(0, 1)] public float heightThreshold;
        public TileBase tile;
        public Tilemap targetTilemap;
    }

    public class WorldGenerator : MonoBehaviour
    {
        public static WorldGenerator Instance;
        public bool IsWorldGenerated { get; private set; } = false;

        [Header("Настройки Карты")]
        public int width = 512;
        public int height = 512;
        public string seed;
        public bool useRandomSeed = true;

        [Header("Форма Мира")]
        public float continentShapeScale = 180f;
        [Range(0.5f, 3f)] public float verticalStretch = 1.5f;
        [Range(0.1f, 1f)] public float edgeWaterBuffer = 0.3f;

        [Header("ГРАНИЦЫ (Розовые линии)")]
        [Range(0.01f, 0.3f)] public float centralRiverRadius = 0.1f;
        [Range(5f, 30f)] public float radialRiverWidth = 10f;

        [Header("Острова")]
        public float islandScale = 35f;
        [Range(0, 1)] public float islandPrevalence = 0.45f;
        public float islandMinDist = 85f;
        public float islandMaxDist = 160f;

        [Header("Рельеф")]
        public float terrainDetailScale = 45f;
        public int octaves = 5;
        [Range(0, 1)] public float persistance = 0.5f;
        public float lacunarity = 2f;
        public float terrainStrength = 1.0f;
        public float beachSteepness = 15f;

        [Header("ПЯТНА И ОЗЕРА")]
        public float patchScale = 20f;
        [Range(0, 1f)] public float patchDigStrength = 0.35f;
        [Range(0, 1f)] public float patchThreshold = 0.6f;

        [Header("Слои Биомов")]
        public List<BiomeLayer> layers;

        [Header("Технические Ссылки")]
        public Tilemap baseTilemap;

        private float[] _noiseMap;
        private int _currentKingdomsCount = 2;
        public int CurrentKingdomsCount => _currentKingdomsCount;
        // --- МЕТОДЫ ДЛЯ СОХРАНЕНИЯ ---
      

        public void RegenerateWorldFromSave(string s)
        {
            // При загрузке восстанавливаем мир с тем же сидом
            // Количество игроков можно взять из CurrentKingdomsCount или дефолтное
            GenerateWorld(s, Mathf.Max(2, _currentKingdomsCount));
        }
    
private void Awake() { Instance = this; }

        // --- МЕТОДЫ ДОСТУПА К ДАННЫМ КАРТЫ ---

        // 1. Получить владельца земли по координатам (для камер и границ)
        public int GetKingdomIDAtPosition(Vector3 worldPos)
        {
            if (_currentKingdomsCount <= 1) return 0;
            float x = worldPos.x;
            float y = worldPos.y;
            float angle = Mathf.Atan2(y, x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;
            float sliceSize = 360f / _currentKingdomsCount;
            float shiftedAngle = angle + (sliceSize / 2f);
            if (shiftedAngle >= 360f) shiftedAngle -= 360f;
            int id = Mathf.FloorToInt(shiftedAngle / sliceSize);
            return id % _currentKingdomsCount;
        }

        // 2. Получить имя биома по мировым координатам (обертка)
        public string GetBiomeAt(Vector3 worldPos)
        {
            if (baseTilemap == null) return "Void";
            return GetBiomeAtCell(baseTilemap.WorldToCell(worldPos));
        }

        // 3. Получить имя биома по координатам КЛЕТКИ (для ResourceSpawner и BuildManager)
        // ИМЕННО ЭТОГО МЕТОДА НЕ ХВАТАЛО
        public string GetBiomeAtCell(Vector3Int cellPos)
        {
            if (_noiseMap == null) return "Void";

            // Преобразуем координаты сетки (0 в центре) в индекс массива (0 слева снизу)
            int x = cellPos.x + (width / 2);
            int y = cellPos.y + (height / 2);

            if (x < 0 || x >= width || y < 0 || y >= height) return "Void";

            int index = y * width + x;
            float heightVal = _noiseMap[index];

            // Проверяем слои сверху вниз (Горы -> Холмы -> Трава -> Вода)
            for (int i = layers.Count - 1; i >= 0; i--)
            {
                if (heightVal >= layers[i].heightThreshold) return layers[i].name;
            }
            return "DeepWater"; // Дефолт, если ничего не подошло
        }

        // 4. Проверка: можно ли строить на этой клетке?
        public bool IsCellBuildable(Vector3Int cellPos)
        {
            string biomeName = GetBiomeAtCell(cellPos);

            // Список запрещенных биомов (названия должны совпадать с Layers в инспекторе)
            if (biomeName.Contains("Deep") || biomeName.Contains("Ocean") || biomeName.Contains("Deep Ocean")) return false;
            if (biomeName.Contains("Sea")) return false;
            if (biomeName.Contains("Water")) return false; // Реки
            if (biomeName.Contains("Mountain")) return false;
            if (biomeName.Contains("Rock")) return false;
            if (biomeName.Contains("Bedrock")) return false;

            // Если биом не в списке запрещенных (например Grass, Sand, Meadow), то строить можно
            return true;
        }

        // --------------------------------------------------

        public void GenerateWorld(string forceSeed = "", int kingdomsCount = 2)
        {
            IsWorldGenerated = false;
            _currentKingdomsCount = Mathf.Max(2, kingdomsCount);
            if (!string.IsNullOrEmpty(forceSeed))
            {
                seed = forceSeed;
                useRandomSeed = false;
            }
            else if (useRandomSeed)
            {
                seed = Random.Range(0, 1000000).ToString();
            }

            Debug.Log($"WorldGenerator: Generating {_currentKingdomsCount} sectors. Seed: {seed}");

            float[] continentMask = GenerateMultiContinentMask(_currentKingdomsCount);
            float[] islandNoise = GenerateNoiseMap(islandScale, 3, 0.6f, 2.5f, seed + "_islands");
            float[] terrainNoise = GenerateNoiseMap(terrainDetailScale, octaves, persistance, lacunarity, seed + "_terrain");
            float[] waterWarpNoise = GenerateNoiseMap(40f, 2, 0.5f, 2f, seed + "_waterwarp");
            float[] patchNoise = GenerateNoiseMap(patchScale, 2, 0.5f, 2f, seed + "_patches");

            _noiseMap = new float[width * height];
            float waterLvl = GetThreshold("Water");
            float sandLvl = GetThreshold("Sand");
            float grassLvl = GetThreshold("Grass");
            float meadowLvl = GetThreshold("Meadow");
            float sliceAngle = 360f / _currentKingdomsCount;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int i = y * width + x;
                    float relX = x - (width / 2f);
                    float relY = y - (height / 2f);
                    float distFromMapCenter = Mathf.Sqrt(relX * relX + relY * relY);
                    float currentAngle = Mathf.Atan2(relY, relX) * Mathf.Rad2Deg;
                    if (currentAngle < 0) currentAngle += 360f;

                    float angleForCut = currentAngle + (sliceAngle / 2f);
                    float angleInSlice = angleForCut % sliceAngle;
                    float distToCut = Mathf.Min(angleInSlice, sliceAngle - angleInSlice);
                    float radialRiverFactor = Mathf.Clamp01((distToCut * distFromMapCenter * 0.05f) - (radialRiverWidth / 2f));

                    bool allowSmallIslands = distFromMapCenter > 70f && distFromMapCenter < (width / 2f - 10f);
                    float islandValue = 0f;
                    if (allowSmallIslands && islandNoise[i] > (1f - islandPrevalence))
                        islandValue = (islandNoise[i] - (1f - islandPrevalence)) / islandPrevalence;

                    float baseShape = Mathf.Max(continentMask[i], islandValue);
                    float centerRiverFactor = Mathf.Clamp01((distFromMapCenter - centralRiverRadius * (width / 2f)) / 50f);
                    baseShape *= centerRiverFactor;
                    float radialCut = Mathf.SmoothStep(0, 1, distToCut / (radialRiverWidth / 2f));
                    baseShape *= Mathf.Clamp01(radialCut + (10f / distFromMapCenter));

                    if (baseShape < waterLvl)
                    {
                        _noiseMap[i] = baseShape + (waterWarpNoise[i] * 0.05f);
                    }
                    else
                    {
                        float landFactor = Mathf.Clamp01((baseShape - waterLvl) * beachSteepness);
                        float landBaseHeight;
                        if (landFactor < 0.2f) landBaseHeight = Mathf.Lerp(sandLvl, meadowLvl, landFactor * 5f);
                        else landBaseHeight = Mathf.Lerp(meadowLvl, grassLvl, landFactor);

                        float relief = Mathf.Pow(terrainNoise[i], 3f) * terrainStrength;
                        float finalHeight = landBaseHeight + (relief * landFactor);
                        if (patchNoise[i] > patchThreshold)
                        {
                            float digPower = (patchNoise[i] - patchThreshold) / (1f - patchThreshold);
                            finalHeight -= digPower * patchDigStrength;
                        }
                        _noiseMap[i] = Mathf.Clamp01(finalHeight);
                    }
                }
            }
            DrawMapLayers();
            IsWorldGenerated = true;
        }

        public Vector3 GetSpawnPosition(int kingdomID)
        {
            if (_currentKingdomsCount <= 0) return Vector3.zero;
            if (kingdomID < 0 || kingdomID >= _currentKingdomsCount) kingdomID = 0;
            if (baseTilemap == null) return Vector3.zero;

            Vector2 mapCenter = new Vector2(width / 2f, height / 2f);
            float placementRadius = width * 0.30f;
            float angleStep = 360f / _currentKingdomsCount;
            float angle = (kingdomID * angleStep) * Mathf.Deg2Rad;
            Vector2 islandCenter = mapCenter + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * placementRadius;
            int centerX = Mathf.RoundToInt(islandCenter.x);
            int centerY = Mathf.RoundToInt(islandCenter.y);
            float sandLvl = GetThreshold("Sand");
            float hillLvl = GetThreshold("Hill");

            for (int r = 0; r < 80; r += 5)
            {
                for (int x = centerX - r; x <= centerX + r; x += 10)
                {
                    for (int y = centerY - r; y <= centerY + r; y += 10)
                    {
                        if (x < 0 || x >= width || y < 0 || y >= height) continue;
                        int index = y * width + x;
                        if (_noiseMap[index] >= sandLvl && _noiseMap[index] < hillLvl)
                            return baseTilemap.GetCellCenterWorld(new Vector3Int(x - width / 2, y - height / 2, 0));
                    }
                }
            }
            return baseTilemap.GetCellCenterWorld(new Vector3Int(centerX - width / 2, centerY - height / 2, 0));
        }

        private void DrawMapLayers()
        {
            foreach (var layer in layers) if (layer.targetTilemap != null) layer.targetTilemap.ClearAllTiles();
            List<Vector3Int>[] positionsPerLayer = new List<Vector3Int>[layers.Count];
            List<TileBase>[] tilesPerLayer = new List<TileBase>[layers.Count];
            for (int i = 0; i < layers.Count; i++) { positionsPerLayer[i] = new List<Vector3Int>(); tilesPerLayer[i] = new List<TileBase>(); }
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x; float heightVal = _noiseMap[index]; Vector3Int pos = new Vector3Int(x - width / 2, y - height / 2, 0);
                    for (int i = layers.Count - 1; i >= 0; i--) { if (heightVal >= layers[i].heightThreshold) { positionsPerLayer[i].Add(pos); tilesPerLayer[i].Add(layers[i].tile); break; } }
                }
            }
            for (int i = 0; i < layers.Count; i++) if (layers[i].targetTilemap != null && positionsPerLayer[i].Count > 0) layers[i].targetTilemap.SetTiles(positionsPerLayer[i].ToArray(), tilesPerLayer[i].ToArray());
        }

        private float GetThreshold(string namePart) { foreach (var layer in layers) if (layer.name.Contains(namePart)) return layer.heightThreshold; return 0.5f; }

        private float[] GenerateMultiContinentMask(int count)
        {
            float[] map = new float[width * height];
            Vector2 center = new Vector2(width / 2f, height / 2f);
            float placementRadius = width * 0.35f;
            List<Vector2> islandCenters = new List<Vector2>();
            for (int i = 0; i < count; i++)
            {
                float angle = i * (360f / count) * Mathf.Deg2Rad;
                Vector2 pos = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * placementRadius;
                islandCenters.Add(pos);
            }
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float maxShapeValue = 0f;
                    foreach (var islandPos in islandCenters)
                    {
                        float dx = Mathf.Abs(x - islandPos.x);
                        float dy = Mathf.Abs(y - islandPos.y) / verticalStretch;
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        float normalizedDist = dist / (width / (count > 2 ? 5.0f : 3.5f));
                        float val = 1 - (normalizedDist * (1f + edgeWaterBuffer));
                        if (val > maxShapeValue) maxShapeValue = val;
                    }
                    float warp = Mathf.PerlinNoise(x / 60f, y / 60f) * 0.2f - 0.1f;
                    map[y * width + x] = Mathf.Clamp01(maxShapeValue + warp);
                }
            }
            return map;
        }

        private float[] GenerateNoiseMap(float scale, int octaves, float persistance, float lacunarity, string localSeed)
        {
            float[] map = new float[width * height];
            System.Random prng = new System.Random(localSeed.GetHashCode());
            Vector2[] octaveOffsets = new Vector2[octaves];
            for (int i = 0; i < octaves; i++) octaveOffsets[i] = new Vector2(prng.Next(-100000, 100000), prng.Next(-100000, 100000));
            if (scale <= 0) scale = 0.0001f;
            float maxNoiseHeight = float.MinValue; float minNoiseHeight = float.MaxValue; float halfWidth = width / 2f; float halfHeight = height / 2f;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float amplitude = 1; float frequency = 1; float noiseHeight = 0;
                    for (int i = 0; i < octaves; i++)
                    {
                        float sampleX = (x - halfWidth) / scale * frequency + octaveOffsets[i].x;
                        float sampleY = (y - halfHeight) / scale * frequency + octaveOffsets[i].y;
                        float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
                        noiseHeight += perlinValue * amplitude; amplitude *= persistance; frequency *= lacunarity;
                    }
                    if (noiseHeight > maxNoiseHeight) maxNoiseHeight = noiseHeight;
                    if (noiseHeight < minNoiseHeight) minNoiseHeight = noiseHeight;
                    map[y * width + x] = noiseHeight;
                }
            }
            for (int i = 0; i < map.Length; i++) map[i] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, map[i]);
            return map;
        }

        public string GetCurrentSeed() => seed;
    }
}