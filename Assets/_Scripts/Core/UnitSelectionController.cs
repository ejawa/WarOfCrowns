using UnityEngine;
using UnityEngine.InputSystem;
using WarOfCrowns.Units;
using WarOfCrowns.World;
using WarOfCrowns.Buildings; // <-- Добавили using для зданий


namespace WarOfCrowns.Core
{
    public class UnitSelectionController : MonoBehaviour
    {
        [SerializeField] private LayerMask unitLayerMask;
        [SerializeField] private LayerMask groundLayerMask;
        [SerializeField] private LayerMask resourceLayerMask;
        [SerializeField] private LayerMask constructionLayerMask; // <-- Добавили LayerMask для стройки

        private Camera _mainCamera;
        private Unit _selectedUnit;

        private void Awake() { _mainCamera = GetComponent<Camera>(); }
        private void Update() { HandleLeftClick(); HandleRightClick(); }

        private void HandleLeftClick()
        {
            if (!Mouse.current.leftButton.wasPressedThisFrame) return;
            Vector2 worldPoint = _mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            RaycastHit2D hit = Physics2D.Raycast(worldPoint, Vector2.zero, 0f, unitLayerMask);

            if (_selectedUnit != null) { _selectedUnit.Deselect(); _selectedUnit = null; }

            if (hit.collider != null && hit.collider.TryGetComponent<Unit>(out var hitUnit))
            {
                _selectedUnit = hitUnit;
                _selectedUnit.Select();
            }
        }

        private void HandleRightClick()
        {
            if (!Mouse.current.rightButton.wasPressedThisFrame || _selectedUnit == null) return;
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

            Vector2 worldPoint = _mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());

            // --- НАЧИНАЕМ ПРОВЕРКУ ---
            Debug.Log("Right Click Detected. Checking for targets...");

            RaycastHit2D constructionHit = Physics2D.Raycast(worldPoint, Vector2.zero, 0f, constructionLayerMask);
            if (constructionHit.collider != null)
            {
                Debug.Log($"Hit '{constructionHit.collider.name}' on Construction layer!");
                if (constructionHit.collider.TryGetComponent<ConstructionSite>(out var site))
                {
                    if (_selectedUnit.TryGetComponent<UnitBuilder>(out var builder))
                    {
                        builder.SetTarget(site);
                        return;
                    }
                }
            }

            RaycastHit2D resourceHit = Physics2D.Raycast(worldPoint, Vector2.zero, 0f, resourceLayerMask);
            if (resourceHit.collider != null)
            {
                Debug.Log($"Hit '{resourceHit.collider.name}' on Resource layer!");
                if (resourceHit.collider.TryGetComponent<ResourceNode>(out var resourceNode))
                {
                    if (_selectedUnit.TryGetComponent<UnitGatherer>(out var gatherer))
                    {
                        gatherer.SetTarget(resourceNode);
                        return;
                    }
                }
            }

            RaycastHit2D groundHit = Physics2D.Raycast(worldPoint, Vector2.zero, 0f, groundLayerMask);
            if (groundHit.collider != null)
            {
                Debug.Log("Hit Ground. Issuing MOVE command.");
                if (_selectedUnit.TryGetComponent<UnitMotor>(out var unitMotor))
                {
                    if (_selectedUnit.TryGetComponent<UnitBuilder>(out var builder)) builder.Cancel();
                    if (_selectedUnit.TryGetComponent<UnitGatherer>(out var gatherer)) gatherer.StopGathering();
                    unitMotor.MoveTo(groundHit.point);
                }
            }
        }
    }
}