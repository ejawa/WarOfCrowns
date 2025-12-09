using UnityEngine;
using UnityEngine.InputSystem;
using WarOfCrowns.Buildings;

namespace WarOfCrowns.Core
{
    public class DemolishManager : MonoBehaviour
    {
        public static DemolishManager Instance { get; private set; }
        [SerializeField] private Texture2D demolishCursor;
        [SerializeField] private LayerMask buildingLayer;

        private bool _isDemolishMode = false;
        private Camera _mainCamera;
        public bool IsDemolishMode => _isDemolishMode;

        private void Awake() { Instance = this; _mainCamera = Camera.main; }

        private void Update()
        {
            if (!_isDemolishMode) return;
            if (Mouse.current.rightButton.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame) { ExitDemolishMode(); return; }
            if (Mouse.current.leftButton.wasPressedThisFrame && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) TryDemolish();
        }

        public void ToggleDemolishMode() { if (_isDemolishMode) ExitDemolishMode(); else EnterDemolishMode(); }
        public void EnterDemolishMode() { _isDemolishMode = true; if (demolishCursor != null) Cursor.SetCursor(demolishCursor, Vector2.zero, CursorMode.Auto); }
        public void ExitDemolishMode() { _isDemolishMode = false; Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto); }

        private void TryDemolish()
        {
            Vector3 mousePos = _mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero, 0f, buildingLayer);

            if (hit.collider != null)
            {
                Building building = hit.collider.GetComponent<Building>();
                if (building != null && Kingdom.PlayerKingdom != null)
                {
                    // »—œ–¿¬À≈ÕŒ: .Value
                    if (building.ownerKingdomID.Value == Kingdom.PlayerKingdom.kingdomID.Value)
                    {
                        building.Demolish();
                        ExitDemolishMode();
                    }
                }
            }
        }
    }
}