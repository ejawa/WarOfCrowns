using UnityEngine;
using UnityEngine.InputSystem;
using WarOfCrowns.Units;

namespace WarOfCrowns.Core
{
    public class UnitSelectionController : MonoBehaviour
    {
        [SerializeField] private LayerMask unitLayerMask;
        [SerializeField] private LayerMask groundLayerMask;

        private Camera _mainCamera;
        private Unit _selectedUnit;

        private void Awake()
        {
            _mainCamera = GetComponent<Camera>();
        }

        private void Update()
        {
            HandleLeftClick();
            HandleRightClick();
        }

        private void HandleLeftClick()
        {
            if (!Mouse.current.leftButton.wasPressedThisFrame) return;

            // Using Physics2D.Raycast - a much more reliable method
            Vector2 worldPoint = _mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            RaycastHit2D hit = Physics2D.Raycast(worldPoint, Vector2.zero, 0f, unitLayerMask);

            // Deselect previous unit first
            if (_selectedUnit != null)
            {
                _selectedUnit.Deselect();
                _selectedUnit = null;
            }

            // If the ray hit a collider on the correct layer
            if (hit.collider != null)
            {
                if (hit.collider.TryGetComponent<Unit>(out Unit hitUnit))
                {
                    _selectedUnit = hitUnit;
                    _selectedUnit.Select();
                }
            }
        }

        private void HandleRightClick()
        {
            if (!Mouse.current.rightButton.wasPressedThisFrame) return;
            if (_selectedUnit == null) return;

            Vector3 worldPoint = _mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            // Z coordinate is not important for the motor, it will be reset there

            if (_selectedUnit.TryGetComponent<UnitMotor>(out UnitMotor unitMotor))
            {
                unitMotor.MoveTo(worldPoint);
            }
        }
    }
}