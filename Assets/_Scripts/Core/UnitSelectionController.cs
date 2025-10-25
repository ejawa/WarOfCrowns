using UnityEngine;
using UnityEngine.InputSystem;
using WarOfCrowns.Units;
using WarOfCrowns.World; // <-- Added this

namespace WarOfCrowns.Core
{
    public class UnitSelectionController : MonoBehaviour
    {
        [SerializeField] private LayerMask unitLayerMask;
        [SerializeField] private LayerMask groundLayerMask;
        [SerializeField] private LayerMask resourceLayerMask; // <-- Added this

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

            Vector2 worldPoint = _mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());

            // Check if we clicked on a resource
            RaycastHit2D resourceHit = Physics2D.Raycast(worldPoint, Vector2.zero, 0f, resourceLayerMask);
            if (resourceHit.collider != null && resourceHit.collider.TryGetComponent<ResourceNode>(out var resourceNode))
            {
                // If we did, command the unit to gather
                if (_selectedUnit.TryGetComponent<UnitGatherer>(out var gatherer))
                {
                    gatherer.SetTarget(resourceNode);
                }
            }
            else
            {
                // If not, it's a simple move command
                if (_selectedUnit.TryGetComponent<UnitMotor>(out var unitMotor))
                {
                    // We stop the gatherer if we give a move command
                    if (_selectedUnit.TryGetComponent<UnitGatherer>(out var gatherer))
                    {
                        // This is not implemented yet, but good to have in mind for later
                        // gatherer.StopGathering(); 
                    }

                    // Here we need to make sure we hit the ground, otherwise the unit will move to a random Z
                    RaycastHit2D groundHit = Physics2D.Raycast(worldPoint, Vector2.zero, 0f, groundLayerMask);
                    if (groundHit.collider != null)
                    {
                        unitMotor.MoveTo(groundHit.point);
                    }
                }
            }
        }
    }
}