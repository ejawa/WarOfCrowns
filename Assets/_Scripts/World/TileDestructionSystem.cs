using UnityEngine;
using UnityEngine.Tilemaps;

namespace WarOfCrowns.World
{
    public class TileDestructionSystem : MonoBehaviour
    {
        public static TileDestructionSystem Instance;

        [Header("Ссылки")]
        [SerializeField] private Tilemap _baseTilemap;

        [Header("Тайлы (Слои земли)")]
        [SerializeField] private TileBase _grassTile;
        [SerializeField] private TileBase _meadowTile; // Поляна
        [SerializeField] private TileBase _soilTile;   // Почва
        [SerializeField] private TileBase _sandTile;   // Песок
        [SerializeField] private TileBase _stoneTile;  // Камень
        [SerializeField] private TileBase _bedrockTile; // Бедрок

        private void Awake() { Instance = this; }

        public void DestroyTileAt(Vector3 worldPos)
        {
            Vector3Int cellPos = _baseTilemap.WorldToCell(worldPos);
            TileBase currentTile = _baseTilemap.GetTile(cellPos);

            TileBase tileBelow = GetTileBelow(currentTile);

            if (tileBelow != null)
            {
                _baseTilemap.SetTile(cellPos, tileBelow);
                // TODO: Спавнить партиклы земли/пыли
            }
        }

        // ТВОЯ ЛОГИКА СЛОЕВ:
        private TileBase GetTileBelow(TileBase current)
        {
            if (current == _grassTile) return _meadowTile;     // Трава -> Поляна
            if (current == _meadowTile) return _soilTile;      // Поляна -> Почва
            if (current == _soilTile) return _sandTile;        // Почва -> Песок
            if (current == _sandTile) return _stoneTile;       // Песок -> Камень
            if (current == _stoneTile) return _bedrockTile;    // Камень -> Бедрок

            return null; // Воду и Бедрок копать нельзя
        }
    }
}