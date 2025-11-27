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
        [Range(0.01f, 0.3f)] public float centralRiverRadius = 0.1f; // Дырка в центре
        [Range(5f, 30f)] public float radialRiverWidth = 10f; // Ширина лучей-рек в градусах

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

        private void Awake() { Instance = this; }

        // --- ВАЖНЫЙ МЕТОД: ОПРЕДЕЛЕНИЕ ВЛАДЕЛЬЦА ЗЕМЛИ ---
        // BuildManager будет использовать его, чтобы понять, можно ли тут строить
        public int GetKingdomIDAtPosition(Vector3 worldPos)
        {
            if (_currentKingdomsCount <= 1) return 0; // Если игрок один, вся карта его

            // Переводим мировые координаты в локальные относительно центра карты
            float x = worldPos.x; // (0,0) в мире это центр карты, так что это ок
            float y = worldPos.y;

            // Вычисляем угол в градусах (-180..180)
            float angle = Mathf.Atan2(y, x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f; // Переводим в 0..360

            // Сектор каждого игрока = 360 / кол-во игроков
            float sliceSize = 360f / _currentKingdomsCount;

            // Сдвигаем угол, чтобы центр острова был в середине сектора
            // Острова генерируются начиная с угла 0.
            // Значит, Игрок 0 владеет сектором от -slice/2 до +slice/2 (грубо говоря)
            // Но наша математика Atan2 идет от оси X.
            // Подгоняем под математику генерации:

            // Формула индекса:
            float shiftedAngle = angle + (sliceSize / 2f);
            if (shiftedAngle >= 360f) shiftedAngle -= 360f;

            int id = Mathf.FloorToInt(shiftedAngle / sliceSize);

            // Защита от выхода за границы
            return Mathf.Clamp(id, 0, _currentKingdomsCount - 1);
        }
        // -------------------------------------------------

        public void GenerateWorld(string forceSeed = "", int kingdomsCount = 2)
        {
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

            Vector2 mapCenter = new Vector2(width / 2f, height / 2f);
            float sliceAngle = 360f / _currentKingdomsCount; // Угол одного сектора

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int i = y * width + x;

                    // Координаты относительно центра (для вычисления угла)
                    float relX = x - (width / 2f);
                    float relY = y - (height / 2f);
                    float distFromMapCenter = Mathf.Sqrt(relX * relX + relY * relY);
                    float currentAngle = Mathf.Atan2(relY, relX) * Mathf.Rad2Deg;
                    if (currentAngle < 0) currentAngle += 360f;

                    // --- 1. РАДИАЛЬНЫЕ РЕКИ (РОЗОВЫЕ ЛИНИИ) ---
                    // Нам нужно узнать, насколько этот пиксель близок к ГРАНИЦЕ сектора.
                    // Границы находятся на углах: sliceAngle/2, sliceAngle*1.5, sliceAngle*2.5...

                    // Сдвигаем угол так, чтобы границы стали кратны sliceAngle
                    float angleForCut = currentAngle + (sliceAngle / 2f);
                    float angleInSlice = angleForCut % sliceAngle;

                    // Разница с ближайшей границей (0 = мы на границе, sliceAngle/2 = мы в центре острова)
                    // Нам нужно: если мы близко к границе (0 или 360), стираем.
                    // Проще: если остаток от деления близок к 0 или к sliceAngle.

                    float distToCut = Mathf.Min(angleInSlice, sliceAngle - angleInSlice);

                    // Фактор реки (0 = вода, 1 = суша). 
                    // radialRiverWidth / 2 - это половина ширины реки в градусах
                    float radialRiverFactor = Mathf.Clamp01((distToCut * distFromMapCenter * 0.05f) - (radialRiverWidth / 2f));
                    // (distFromMapCenter * 0.05f) - хитрость: чем дальше от центра, тем шире река в пикселях, 
                    // но в градусах она постоянная.

                    // --- 2. ОСТРОВА ---
                    // Логика SafeZone (не спавнить острова у базы)
                    bool allowSmallIslands = distFromMapCenter > 70f && distFromMapCenter < (width / 2f - 10f);

                    float islandValue = 0f;
                    if (allowSmallIslands)
                    {
                        if (islandNoise[i] > (1f - islandPrevalence))
                            islandValue = (islandNoise[i] - (1f - islandPrevalence)) / islandPrevalence;
                    }

                    // --- 3. СЛИЯНИЕ ---
                    float baseShape = Mathf.Max(continentMask[i], islandValue);

                    // Применяем ЦЕНТРАЛЬНУЮ реку (круг)
                    float centerRiverFactor = Mathf.Clamp01((distFromMapCenter - 30f) / 50f);

                    // Применяем РАДИАЛЬНЫЕ реки (лучи)
                    // Умножаем форму суши на факторы рек. Если фактор 0, суша исчезает.
                    baseShape *= centerRiverFactor;

                    // Применяем радиальную резку, но чуть мягче
                    // Mathf.SmoothStep делает края реки более естественными
                    float radialCut = Mathf.SmoothStep(0, 1, distToCut / (radialRiverWidth / 2f));
                    baseShape *= radialCut;

                    // --- 4. ВЫСОТА ---
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

                        float mountainNoise = Mathf.Pow(terrainNoise[i], 3f);
                        float relief = mountainNoise * terrainStrength;
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
        }

        // (Остальные методы без изменений)
        public string GetBiomeAt(Vector3 worldPos)
        {
            if (_noiseMap == null) return "Void";
            if (baseTilemap == null)
            {
                // Защита от отсутствия ссылки (если забыл в инспекторе)
                return "Void";
            }

            // 1. Используем Unity, чтобы перевести мировые координаты в координаты клетки
            // Это автоматически учитывает сдвиг Grid'а, масштаб и всё остальное.
            Vector3Int cellPos = baseTilemap.WorldToCell(worldPos);

            // 2. Переводим координаты клетки обратно в индекс массива
            // В DrawMap мы делали: cellPos = x - width/2
            // Значит обратно: x = cellPos + width/2
            int x = cellPos.x + (width / 2);
            int y = cellPos.y + (height / 2);

            // 3. Проверка границ массива
            if (x < 0 || x >= width || y < 0 || y >= height) return "Void";

            int index = y * width + x;
            float heightVal = _noiseMap[index];

            for (int i = layers.Count - 1; i >= 0; i--)
            {
                if (heightVal >= layers[i].heightThreshold) return layers[i].name;
            }
            return "DeepWater";
        }
        public string GetCurrentSeed() => seed;
        public void RegenerateWorldFromSave(string s) => GenerateWorld(s, 2);

        public Vector3 GetSpawnPosition(int kingdomID)
        {
            if (kingdomID >= _currentKingdomsCount) kingdomID = 0;
            if (baseTilemap == null) { Debug.LogError("WorldGenerator: BaseTilemap is null!"); return Vector3.zero; }

            Vector2 mapCenter = new Vector2(width / 2f, height / 2f);
            float placementRadius = width * 0.30f;

            float angleStep = 360f / _currentKingdomsCount;
            float angle = (kingdomID * angleStep) * Mathf.Deg2Rad;

            Vector2 islandCenter = mapCenter + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * placementRadius;

            int centerX = Mathf.RoundToInt(islandCenter.x);
            int centerY = Mathf.RoundToInt(islandCenter.y);
            Vector3 bestPos = baseTilemap.GetCellCenterWorld(new Vector3Int(centerX - width / 2, centerY - height / 2, 0));

            float sandLvl = GetThreshold("Sand");
            float hillLvl = GetThreshold("Hill");
            int range = 80;
            for (int r = 0; r < range; r += 3)
            {
                for (int x = centerX - r; x <= centerX + r; x += 5)
                {
                    for (int y = centerY - r; y <= centerY + r; y += 5)
                    {
                        if (x < 0 || x >= width || y < 0 || y >= height) continue;
                        int index = y * width + x;
                        if (_noiseMap[index] >= sandLvl && _noiseMap[index] < hillLvl)
                            return baseTilemap.GetCellCenterWorld(new Vector3Int(x - width / 2, y - height / 2, 0));
                    }
                }
            }
            return bestPos;
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
                        float sizeMod = (count > 2) ? 5.0f : 3.5f;
                        float normalizedDist = dist / (width / sizeMod);
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
                        float perlinValue = Mathf.PerlinNoise(sampleX, sampleY); noiseHeight += perlinValue * amplitude; amplitude *= persistance; frequency *= lacunarity;
                    }
                    if (noiseHeight > maxNoiseHeight) maxNoiseHeight = noiseHeight; if (noiseHeight < minNoiseHeight) minNoiseHeight = noiseHeight; map[y * width + x] = noiseHeight;
                }
            }
            for (int i = 0; i < map.Length; i++) map[i] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, map[i]); return map;
        }
    }
}