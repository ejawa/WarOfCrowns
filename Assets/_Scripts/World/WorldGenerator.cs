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
        [Range(0.5f, 3f)] public float verticalStretch = 2.0f;
        [Range(0.1f, 1f)] public float edgeWaterBuffer = 0.3f;
        [Range(0.01f, 0.3f)] public float centralRiverWidth = 0.1f; // Река в центре

        [Header("Острова")]
        public float islandScale = 35f;
        [Range(0, 1)] public float islandPrevalence = 0.45f;
        public float islandMinDist = 85f;
        public float islandMaxDist = 160f;

        [Header("Рельеф (Горы)")]
        public float terrainDetailScale = 45f;
        public int octaves = 5;
        [Range(0, 1)] public float persistance = 0.5f;
        public float lacunarity = 2f;
        public float terrainStrength = 1.0f;
        public float beachSteepness = 15f;

        [Header("ПЯТНА И ОЗЕРА (Digging System)")]
        [Tooltip("Размер пятен. 10 = мелкие лужи, 30 = большие озера.")]
        public float patchScale = 20f;

        [Tooltip("Насколько сильно пятна 'проедают' землю. 0 = нет пятен. 0.3 = песок. 0.6 = глубокие озера.")]
        [Range(0, 1f)] public float patchDigStrength = 0.35f; // <--- ИСПРАВЛЕНО (Был пробел)

        [Tooltip("Порог, после которого пятно считается озером/песком.")]
        [Range(0, 1f)] public float patchThreshold = 0.6f;

        [Header("Слои Биомов")]
        public List<BiomeLayer> layers;

        private float[] _noiseMap;

        private void Awake() { Instance = this; }
        private void Start() { GenerateWorld(); }

        public void GenerateWorld()
        {
            if (useRandomSeed) seed = Random.Range(0, 1000000).ToString();
            Debug.Log($"WorldGenerator v13.1 (Syntax Fix): Seed {seed}");

            float[] continentMask = GenerateDualContinentMask();
            float[] islandNoise = GenerateNoiseMap(islandScale, 3, 0.6f, 2.5f, seed + "_islands");
            float[] terrainNoise = GenerateNoiseMap(terrainDetailScale, octaves, persistance, lacunarity, seed + "_terrain");
            float[] waterWarpNoise = GenerateNoiseMap(40f, 2, 0.5f, 2f, seed + "_waterwarp");

            // Шум для пятен (копания)
            float[] patchNoise = GenerateNoiseMap(patchScale, 2, 0.5f, 2f, seed + "_patches");

            _noiseMap = new float[width * height];

            float waterLvl = GetThreshold("Water");
            float sandLvl = GetThreshold("Sand");
            float grassLvl = GetThreshold("Grass");
            float meadowLvl = GetThreshold("Meadow");

            Vector2 leftCenter = new Vector2(width * 0.25f, height * 0.5f);
            Vector2 rightCenter = new Vector2(width * 0.75f, height * 0.5f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int i = y * width + x;

                    // 1. Острова
                    float distLeft = Vector2.Distance(new Vector2(x, y), leftCenter);
                    float distRight = Vector2.Distance(new Vector2(x, y), rightCenter);
                    bool inLeftZone = distLeft > islandMinDist && distLeft < islandMaxDist;
                    bool inRightZone = distRight > islandMinDist && distRight < islandMaxDist;

                    float islandValue = 0f;
                    if (inLeftZone || inRightZone)
                    {
                        float fade = 1f;
                        if (inLeftZone) fade = Mathf.Min((distLeft - islandMinDist) / 20f, (islandMaxDist - distLeft) / 20f);
                        else fade = Mathf.Min((distRight - islandMinDist) / 20f, (islandMaxDist - distRight) / 20f);
                        fade = Mathf.Clamp01(fade);
                        if (islandNoise[i] > (1f - islandPrevalence))
                            islandValue = (islandNoise[i] - (1f - islandPrevalence)) / islandPrevalence * fade;
                    }

                    // 2. Форма материка + Река
                    float baseShape = Mathf.Max(continentMask[i], islandValue);

                    float distFromCenterX = Mathf.Abs(x - width / 2f);
                    float riverZoneSize = width * centralRiverWidth;
                    float riverFactor = Mathf.Clamp01((distFromCenterX - 5f) / (riverZoneSize * 0.5f));
                    riverFactor = Mathf.Pow(riverFactor, 0.5f); // Резкий обрыв реки
                    baseShape *= riverFactor;

                    // 3. Итоговая высота
                    if (baseShape < waterLvl)
                    {
                        _noiseMap[i] = baseShape + (waterWarpNoise[i] * 0.05f);
                    }
                    else
                    {
                        // СУША
                        float landFactor = Mathf.Clamp01((baseShape - waterLvl) * beachSteepness);

                        // Базовая высота (поднимаемся в горы)
                        float landBaseHeight;
                        if (landFactor < 0.2f) landBaseHeight = Mathf.Lerp(sandLvl, meadowLvl, landFactor * 5f);
                        else landBaseHeight = Mathf.Lerp(meadowLvl, grassLvl, landFactor);

                        float mountainNoise = Mathf.Pow(terrainNoise[i], 3f);
                        float relief = mountainNoise * terrainStrength;

                        float finalHeight = landBaseHeight + (relief * landFactor);

                        // --- 4. СИСТЕМА ПЯТЕН (DIGGING) ---
                        if (patchNoise[i] > patchThreshold)
                        {
                            float digPower = (patchNoise[i] - patchThreshold) / (1f - patchThreshold);
                            // Используем исправленную переменную
                            finalHeight -= digPower * patchDigStrength;
                        }

                        _noiseMap[i] = Mathf.Clamp01(finalHeight);
                    }
                }
            }

            DrawMapLayers();
        }

        private void DrawMapLayers()
        {
            foreach (var layer in layers) if (layer.targetTilemap != null) layer.targetTilemap.ClearAllTiles();
            List<Vector3Int>[] positionsPerLayer = new List<Vector3Int>[layers.Count];
            List<TileBase>[] tilesPerLayer = new List<TileBase>[layers.Count];
            for (int i = 0; i < layers.Count; i++)
            {
                positionsPerLayer[i] = new List<Vector3Int>();
                tilesPerLayer[i] = new List<TileBase>();
            }
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    float heightVal = _noiseMap[index];
                    Vector3Int pos = new Vector3Int(x - width / 2, y - height / 2, 0);
                    for (int i = layers.Count - 1; i >= 0; i--)
                    {
                        if (heightVal >= layers[i].heightThreshold)
                        {
                            positionsPerLayer[i].Add(pos);
                            tilesPerLayer[i].Add(layers[i].tile);
                            break;
                        }
                    }
                }
            }
            for (int i = 0; i < layers.Count; i++)
            {
                if (layers[i].targetTilemap != null && positionsPerLayer[i].Count > 0)
                    layers[i].targetTilemap.SetTiles(positionsPerLayer[i].ToArray(), tilesPerLayer[i].ToArray());
            }
        }

        private float GetThreshold(string namePart)
        {
            foreach (var layer in layers) if (layer.name.Contains(namePart)) return layer.heightThreshold;
            return 0.5f;
        }

        private float[] GenerateDualContinentMask()
        {
            float[] map = new float[width * height];
            Vector2 leftCenter = new Vector2(width * 0.25f, height * 0.5f);
            Vector2 rightCenter = new Vector2(width * 0.75f, height * 0.5f);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dxLeft = Mathf.Abs(x - leftCenter.x);
                    float dyLeft = Mathf.Abs(y - leftCenter.y) / verticalStretch;
                    float distLeft = Mathf.Sqrt(dxLeft * dxLeft + dyLeft * dyLeft);
                    float dxRight = Mathf.Abs(x - rightCenter.x);
                    float dyRight = Mathf.Abs(y - rightCenter.y) / verticalStretch;
                    float distRight = Mathf.Sqrt(dxRight * dxRight + dyRight * dyRight);
                    float distToNearest = Mathf.Min(distLeft, distRight);
                    float normalizedDist = distToNearest / (width / 3.5f);
                    float value = 1 - (normalizedDist * (1f + edgeWaterBuffer));
                    float warp = Mathf.PerlinNoise(x / 60f, y / 60f) * 0.2f - 0.1f;
                    map[y * width + x] = Mathf.Clamp01(value + warp);
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
            float maxNoiseHeight = float.MinValue; float minNoiseHeight = float.MaxValue;
            float halfWidth = width / 2f; float halfHeight = height / 2f;
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
                        noiseHeight += perlinValue * amplitude;
                        amplitude *= persistance; frequency *= lacunarity;
                    }
                    if (noiseHeight > maxNoiseHeight) maxNoiseHeight = noiseHeight;
                    if (noiseHeight < minNoiseHeight) minNoiseHeight = noiseHeight;
                    map[y * width + x] = noiseHeight;
                }
            }
            for (int i = 0; i < map.Length; i++) map[i] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, map[i]);
            return map;
        }

        // --- НОВЫЙ МЕТОД: Узнать биом в точке (нужен для Спавнера и Строительства) ---
        public string GetBiomeAt(Vector3 worldPos)
        {
            if (_noiseMap == null) return "Void";

            // Переводим мировые координаты в координаты массива (0..512)
            int x = Mathf.RoundToInt(worldPos.x + width / 2f);
            int y = Mathf.RoundToInt(worldPos.y + height / 2f);

            // Проверка границ
            if (x < 0 || x >= width || y < 0 || y >= height) return "Void";

            int index = y * width + x;
            float heightVal = _noiseMap[index];

            // Ищем слой сверху вниз
            for (int i = layers.Count - 1; i >= 0; i--)
            {
                if (heightVal >= layers[i].heightThreshold)
                {
                    return layers[i].name;
                }
            }
            return "DeepWater"; // Если ничего не нашли
        }
        public string GetCurrentSeed()
        {
            return seed;
        }

        // 2. Принудительная генерация по сиду (для загрузки)
        public void RegenerateWorldFromSave(string loadedSeed)
        {
            seed = loadedSeed;
            useRandomSeed = false; // Важно! Отключаем рандом, чтобы использовать загруженный сид

            Debug.Log($"WorldGenerator: Regenerating world from SAVE with seed: {seed}");

            GenerateWorld(); // Пересоздаем карту

            // Если есть спавнер ресурсов, просим его перезаселить мир (или загрузим ресурсы отдельно)
            // Но обычно при загрузке SaveManager сам расставит сохраненные ресурсы поверх.
        }

        public Vector3 GetSpawnPosition(int kingdomID)
        {
            float targetXPercent = (kingdomID == 0) ? 0.25f : 0.75f;
            int centerX = Mathf.FloorToInt(width * targetXPercent);
            int centerY = height / 2;
            int range = 150;
            float sandLvl = GetThreshold("Sand");
            float hillLvl = GetThreshold("Hill");
            for (int r = 0; r < range; r++)
            {
                for (int x = centerX - r; x <= centerX + r; x++)
                {
                    for (int y = centerY - r; y <= centerY + r; y++)
                    {
                        if (x < 0 || x >= width || y < 0 || y >= height) continue;
                        int index = y * width + x;
                        if (_noiseMap[index] >= sandLvl && _noiseMap[index] < hillLvl)
                            return new Vector3(x - width / 2, y - height / 2, 0);
                    }
                }
            }
            return Vector3.zero;
        }
    }
}