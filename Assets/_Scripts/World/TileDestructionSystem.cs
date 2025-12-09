using UnityEngine;
using UnityEngine.Tilemaps;
using Unity.Netcode;

namespace WarOfCrowns.World
{
    public class TileDestructionSystem : NetworkBehaviour
    {
        public static TileDestructionSystem Instance;

        [Header("Ссылки")]
        [SerializeField] private Tilemap _baseTilemap;

        [Header("Тайлы (Слои земли)")]
        [SerializeField] private TileBase _grassTile;
        [SerializeField] private TileBase _meadowTile;
        [SerializeField] private TileBase _soilTile;
        [SerializeField] private TileBase _sandTile;
        [SerializeField] private TileBase _stoneTile;
        [SerializeField] private TileBase _bedrockTile;

        private void Awake() { Instance = this; }

        // Метод вызывается, когда юнит "копает"
        public void DestroyTileAt(Vector3 worldPos)
        {
            if (NetworkManager.Singleton.IsClient)
            {
                // Клиент просит сервер выкопать
                DestroyTileServerRpc(worldPos);
            }
            else
            {
                // Хост копает сразу
                PerformDig(worldPos);
                // И сообщает другим
                DestroyTileClientRpc(worldPos);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void DestroyTileServerRpc(Vector3 worldPos)
        {
            PerformDig(worldPos);
            DestroyTileClientRpc(worldPos);
        }

        [ClientRpc]
        private void DestroyTileClientRpc(Vector3 worldPos)
        {
            if (!IsServer) // Хост уже выкопал в ServerRpc
            {
                PerformDig(worldPos);
            }
        }

        private void PerformDig(Vector3 worldPos)
        {
            Vector3Int cellPos = _baseTilemap.WorldToCell(worldPos);
            TileBase currentTile = _baseTilemap.GetTile(cellPos);
            TileBase tileBelow = GetTileBelow(currentTile);

            if (tileBelow != null)
            {
                _baseTilemap.SetTile(cellPos, tileBelow);
                // Тут можно добавить эффекты (пыль)
            }
        }

        private TileBase GetTileBelow(TileBase current)
        {
            if (current == _grassTile) return _meadowTile;
            if (current == _meadowTile) return _soilTile;
            if (current == _soilTile) return _sandTile;
            if (current == _sandTile) return _stoneTile;
            if (current == _stoneTile) return _bedrockTile;
            return null;
        }
    }
}