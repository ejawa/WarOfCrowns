using UnityEngine;
using UnityEngine.InputSystem;
using WarOfCrowns.Units;
using WarOfCrowns.World;
using WarOfCrowns.Buildings; // <-- ƒÓ·‡‚ËÎË using ‰Îˇ Á‰‡ÌËÈ

namespace WarOfCrowns.Core
{
    public class UnitSelectionController : MonoBehaviour
    {
        [SerializeField] private LayerMask unitLayerMask;
        [SerializeField] private LayerMask groundLayerMask;
        [SerializeField] private LayerMask resourceLayerMask;
        [SerializeField] private LayerMask constructionLayerMask; // <-- ƒÓ·‡‚ËÎË LayerMask ‰Îˇ ÒÚÓÈÍË

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

            // --- ¬Œ“ “¿ —¿Ã¿ﬂ ÀŒ√» ¿ ---

            // 1. œ–Œ¬≈–ﬂ≈Ã, Õ≈  À» Õ”À» À» Ã€ Õ¿ —“–Œ… ”
            RaycastHit2D constructionHit = Physics2D.Raycast(worldPoint, Vector2.zero, 0f, constructionLayerMask);
            if (constructionHit.collider != null && constructionHit.collider.TryGetComponent<ConstructionSite>(out var site))
            {
                if (_selectedUnit.TryGetComponent<UnitBuilder>(out var builder))
                {
                    builder.SetTarget(site);
                    return; // Õ‡¯ÎË Á‡‰‡˜Û, ‚˚ıÓ‰ËÏ
                }
            }

            // 2. ≈—À» Õ≈“, œ–Œ¬≈–ﬂ≈Ã, Õ≈  À» Õ”À» À» Ã€ Õ¿ –≈—”–—
            RaycastHit2D resourceHit = Physics2D.Raycast(worldPoint, Vector2.zero, 0f, resourceLayerMask);
            if (resourceHit.collider != null && resourceHit.collider.TryGetComponent<ResourceNode>(out var resourceNode))
            {
                if (_selectedUnit.TryGetComponent<UnitGatherer>(out var gatherer))
                {
                    gatherer.SetTarget(resourceNode);
                    return; // Õ‡¯ÎË Á‡‰‡˜Û, ‚˚ıÓ‰ËÏ
                }
            }

            // 3. ≈—À» » ›“Œ Õ≈“, «Õ¿◊»“, ›“Œ  ŒÃ¿Õƒ¿ ƒ¬»∆≈Õ»ﬂ
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