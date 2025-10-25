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

            Vector2 worldPoint = _mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());

            var gatherer = _selectedUnit.GetComponent<UnitGatherer>();

            // Priority 1: Construction (мы пока это не трогаем)
            // ...

            // Priority 2: Resources
            RaycastHit2D resourceHit = Physics2D.Raycast(worldPoint, Vector2.zero, 0f, resourceLayerMask);
            if (resourceHit.collider != null && resourceHit.collider.TryGetComponent<ResourceNode>(out var resourceNode))
            {
                gatherer?.SetTarget(resourceNode);
                return;
            }

            // --- ПРИОРИТЕТ 3: Движение (отменяет все) ---
            // Если мы дошли до сюда, значит, это приказ на движение
            gatherer?.StopGathering(); // <-- ГЛАВНАЯ КОМАНДА ОСТАНОВКИ

            RaycastHit2D groundHit = Physics2D.Raycast(worldPoint, Vector2.zero, 0f, groundLayerMask);
            if (groundHit.collider != null)
            {
                if (_selectedUnit.TryGetComponent<UnitMotor>(out var unitMotor))
                {
                    unitMotor.MoveTo(groundHit.point);
                }
            }
        }
    }
}