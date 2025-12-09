using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using WarOfCrowns.Units;
using WarOfCrowns.Buildings;
using WarOfCrowns.UI;

namespace WarOfCrowns.Core
{
    public class InfoToolManager : MonoBehaviour
    {
        public static InfoToolManager Instance { get; private set; }

        [Header("Настройки")]
        [SerializeField] private Texture2D infoCursor;
        [SerializeField] private LayerMask interactLayer;

        [Header("UI Окна")]
        [SerializeField] private UnitDetailUI unitDetailUI;
        [SerializeField] private BuildingDetailUI buildingDetailUI;

        public bool IsInfoMode { get; private set; } = false;
        private Camera _mainCamera;

        private void Awake()
        {
            Instance = this;
            _mainCamera = Camera.main;
        }

        public void ToggleInfoMode()
        {
            SetInfoMode(!IsInfoMode);
        }

        public void SetInfoMode(bool active)
        {
            IsInfoMode = active;
            if (IsInfoMode)
            {
                Cursor.SetCursor(infoCursor, Vector2.zero, CursorMode.Auto);
            }
            else
            {
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                if (unitDetailUI) unitDetailUI.Close();
                if (buildingDetailUI) buildingDetailUI.Close();
            }
        }

        private void Update()
        {
            if (!IsInfoMode) return;

            // ПКМ - Выход из режима
            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                SetInfoMode(false);
                return;
            }

            // ЛКМ - Выбор
            if (Mouse.current.leftButton.wasPressedThisFrame && !EventSystem.current.IsPointerOverGameObject())
            {
                Vector3 mPos = _mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                RaycastHit2D hit = Physics2D.Raycast(mPos, Vector2.zero, 0f, interactLayer);

                if (hit.collider != null)
                {
                    Unit unit = hit.collider.GetComponentInParent<Unit>();
                    if (unit != null)
                    {
                        if (unitDetailUI) unitDetailUI.Open(unit);
                        if (buildingDetailUI) buildingDetailUI.Close();
                        return;
                    }

                    Building building = hit.collider.GetComponentInParent<Building>();
                    if (building != null)
                    {
                        if (buildingDetailUI) buildingDetailUI.Open(building);
                        if (unitDetailUI) unitDetailUI.Close();
                        return;
                    }
                }
            }
        }
    }
}