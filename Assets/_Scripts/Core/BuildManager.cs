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
        [Header("Префабы")]
        public List<GameObject> buildableFoundations;
        public GameObject townHallPrefab;
        public GameObject peasantPrefab; // Не используется здесь, но пусть будет для ссылок

        [Header("Правила")]
        public LayerMask obstacleLayer;
        public LayerMask resourcesToClearLayer;
        public float clearanceModifier = 2.0f;

        private GameObject _ghostInstance;
        private GameObject _foundationToBuild;
        private bool _isBuildingMode;
        private SpriteRenderer _ghostRenderer;

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentState.Value != GameState.Playing)
            {
                if (_isBuildingMode) ExitBuildMode();
                return;
            }
            if (_isBuildingMode) HandleGhost();
        }

        public void RequestPlaceInitialTownHall(Vector3 pos, GameObject townHallPrefabRef)
        {
            RequestPlaceTownHallServerRpc(pos);
        }

        // ... (EnterBuildMode, HandleGhost, PlaceFoundation, IsValidPlacement, ClearResources - БЕЗ ИЗМЕНЕНИЙ) ...
        // ... (Скопируй их из предыдущей версии, они работают верно) ...
        // Для экономии места я их свернул, но они нужны!
        public void EnterBuildMode(GameObject p) { if (_isBuildingMode) ExitBuildMode(); _foundationToBuild = p; if (!_foundationToBuild) return; _isBuildingMode = true; _ghostInstance = Instantiate(p); if (_ghostInstance.TryGetComponent(out SpriteRenderer s)) _ghostRenderer = s; foreach (var r in _ghostInstance.GetComponentsInChildren<SpriteRenderer>()) if (r.gameObject != _ghostInstance) r.gameObject.SetActive(false); if (_ghostInstance.GetComponent<Collider2D>()) Destroy(_ghostInstance.GetComponent<Collider2D>()); if (_ghostInstance.GetComponent<ConstructionSite>()) Destroy(_ghostInstance.GetComponent<ConstructionSite>()); }
        private void HandleGhost() { Vector3 m = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue()); m.z = 0; if (_ghostInstance) { _ghostInstance.transform.position = m; bool v = IsValidPlacement(m, _foundationToBuild); if (_ghostRenderer) _ghostRenderer.color = v ? new Color(0, 1, 0, 0.5f) : new Color(1, 0, 0, 0.5f); if (Mouse.current.leftButton.wasPressedThisFrame && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject() && v) { int i = buildableFoundations.IndexOf(_foundationToBuild); if (i != -1) RequestPlaceFoundationServerRpc(i, m); ExitBuildMode(); } } if (Mouse.current.rightButton.wasPressedThisFrame) ExitBuildMode(); }
        private void ExitBuildMode() { _isBuildingMode = false; if (_ghostInstance) Destroy(_ghostInstance); _foundationToBuild = null; }
        public bool IsValidPlacement(Vector3 p, GameObject o) { if (!o) return false; var c = o.GetComponent<Collider2D>(); Vector2 s = c ? c.bounds.size : Vector2.one; if (Physics2D.OverlapBox(p, s * 0.9f, 0, obstacleLayer)) return false; if (WorldGenerator.Instance) { for (int x = -1; x <= 1; x++) for (int y = -1; y <= 1; y++) { Vector3 pt = p + new Vector3(s.x / 2f * x, s.y / 2f * y, 0); string b = WorldGenerator.Instance.GetBiomeAt(pt); if (b.Contains("Water") || b.Contains("Ocean") || b.Contains("Sea") || b.Contains("Mountain") || b.Contains("Rock")) return false; } } if (Kingdom.PlayerKingdom) { int id = Kingdom.PlayerKingdom.kingdomID; Vector3 sp = WorldGenerator.Instance.GetSpawnPosition(id); if (Vector3.Distance(p, sp) > 120f) return false; } return true; }
        public void ClearResources(Vector3 p, GameObject o) { Vector2 s = (o.GetComponent<Collider2D>() ? o.GetComponent<Collider2D>().bounds.size : Vector2.one) + new Vector2(3, 3); foreach (var h in Physics2D.OverlapBoxAll(p, s, 0, resourcesToClearLayer)) { if (h.GetComponentInParent<ResourceNode>() is var r && r) Destroy(r.gameObject); } }

        // --- СЕТЕВАЯ ЧАСТЬ ---

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

            // Владелец = ID Клиента
            int ownerId = (int)rpc.Receive.SenderClientId;
            obj.GetComponent<Building>().SetOwnerID(ownerId);

            // ВМЕСТО СПАВНА ЮНИТОВ -> РЕГИСТРИРУЕМ ГОТОВНОСТЬ В GAMEMANAGER
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RegisterPlayerReady(rpc.Receive.SenderClientId, pos);
            }
        }

        private void ClearResourcesOnServer(Vector3 pos, GameObject prefab)
        {
            var col = prefab.GetComponent<Collider2D>();
            Vector2 size = col ? col.bounds.size : Vector2.one;
            Collider2D[] hits = Physics2D.OverlapBoxAll(pos, size * clearanceModifier, 0f, resourcesToClearLayer);
            foreach (var h in hits) if (h.GetComponentInParent<ResourceNode>() is var r && r) Destroy(r.gameObject);
        }
    }
}