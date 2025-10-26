using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems; // <-- Добавили using для UI
using WarOfCrowns.Units;
using WarOfCrowns.World;
using WarOfCrowns.Buildings;

namespace WarOfCrowns.Core
{
    public class UnitSelectionController : MonoBehaviour
    {
        [SerializeField] private LayerMask unitLayerMask;
        [SerializeField] private LayerMask groundLayerMask;
        [SerializeField] private LayerMask resourceLayerMask;
        [SerializeField] private LayerMask constructionLayerMask;

        private Camera _mainCamera;
        private Unit _selectedUnit;
        private SelectableBuilding _selectedBuilding;

        private void Awake() { _mainCamera = Camera.main; }
        private void Update() { HandleLeftClick(); HandleRightClick(); }

        private void HandleLeftClick()
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;
            if (!Mouse.current.leftButton.wasPressedThisFrame) return;

            bool clickedOnSomething = false;
            Vector2 worldPoint = _mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());

            RaycastHit2D unitHit = Physics2D.Raycast(worldPoint, Vector2.zero, 0f, unitLayerMask);
            if (unitHit.collider != null && unitHit.collider.TryGetComponent<Unit>(out var hitUnit))
            {
                if (_selectedBuilding != null) _selectedBuilding.Deselect();
                _selectedBuilding = null;
                if (_selectedUnit != null && _selectedUnit != hitUnit) _selectedUnit.Deselect();
                _selectedUnit = hitUnit;
                _selectedUnit.Select();
                clickedOnSomething = true;
            }
            else
            {
                RaycastHit2D buildingHit = Physics2D.Raycast(worldPoint, Vector2.zero, 0f, constructionLayerMask);
                if (buildingHit.collider != null)
                {
                    Debug.Log($"Raycast HIT the collider of '{buildingHit.collider.name}'!");
                    if (buildingHit.collider.TryGetComponent<SelectableBuilding>(out var hitBuilding))
                    {
                        Debug.Log("Found SelectableBuilding component! Calling Select().");
                        if (_selectedUnit != null) _selectedUnit.Deselect();
                        _selectedUnit = null;
                        if (_selectedBuilding != null && _selectedBuilding != hitBuilding) _selectedBuilding.Deselect();
                        _selectedBuilding = hitBuilding;
                        _selectedBuilding.Select();
                        clickedOnSomething = true;
                    }
                    else
                    {
                        Debug.LogError("HIT a building collider, but it has NO SelectableBuilding SCRIPT!", buildingHit.collider.gameObject);
                    }
                }
            }

            if (!clickedOnSomething)
            {
                if (_selectedUnit != null) _selectedUnit.Deselect();
                _selectedUnit = null;
                if (_selectedBuilding != null) _selectedBuilding.Deselect();
                _selectedBuilding = null;
            }
        }

        private void HandleRightClick()
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;
            if (!Mouse.current.rightButton.wasPressedThisFrame || _selectedUnit == null) return;

            Vector2 worldPoint = _mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());

            RaycastHit2D constructionHit = Physics2D.Raycast(worldPoint, Vector2.zero, 0f, constructionLayerMask);
            if (constructionHit.collider != null && constructionHit.collider.TryGetComponent<ConstructionSite>(out var site))
            {
                if (_selectedUnit.TryGetComponent<UnitBuilder>(out var builder))
                {
                    builder.SetTarget(site);
                    return;
                }
            }

            RaycastHit2D resourceHit = Physics2D.Raycast(worldPoint, Vector2.zero, 0f, resourceLayerMask);
            if (resourceHit.collider != null && resourceHit.collider.TryGetComponent<ResourceNode>(out var resourceNode))
            {
                if (_selectedUnit.TryGetComponent<UnitGatherer>(out var gatherer))
                {
                    gatherer.SetTarget(resourceNode);
                    return;
                }
            }

            RaycastHit2D groundHit = Physics2D.Raycast(worldPoint, Vector2.zero, 0f, groundLayerMask);
            if (groundHit.collider != null)
            {
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