using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using WarOfCrowns.Units;
using WarOfCrowns.World;
using WarOfCrowns.Buildings;
using System.Collections.Generic;
// Fix ambiguity for Unit class
using Unit = WarOfCrowns.Units.Unit;

namespace WarOfCrowns.Core
{
    public class UnitSelectionController : MonoBehaviour
    {
        [Header("Layers")]
        [SerializeField] private LayerMask unitLayerMask;
        // GroundLayerMask нам больше не нужен для движения, но оставим, если нужен для чего-то еще
        [SerializeField] private LayerMask groundLayerMask;
        [SerializeField] private LayerMask resourceLayerMask;
        [SerializeField] private LayerMask constructionLayerMask;
        [SerializeField] private LayerMask enemiesLayerMask;

        [Header("UI Selection Box")]
        [SerializeField] private RectTransform selectionBox;

        private Camera _mainCamera;
        private RectTransform _canvasRect;
        private List<Unit> _selectedUnits = new List<Unit>();
        private List<Unit> _unitsBeforeDrag = new List<Unit>();
        private SelectableBuilding _selectedBuilding;

        private Vector2 _startMousePos;
        private bool _isDragging;
        private float _selectionCooldown = 0f;

        public static event System.Action<List<Unit>> OnSelectionChanged;

        public List<Unit> GetSelectedUnits() => _selectedUnits;

        private void Awake()
        {
            _mainCamera = Camera.main;
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null) _canvasRect = canvas.GetComponent<RectTransform>();
        }

        private void Update()
        {
            if (_selectedUnits.Count > 0)
            {
                int removed = _selectedUnits.RemoveAll(u => u == null);
                if (removed > 0) OnSelectionChanged?.Invoke(_selectedUnits);
            }

            if (BuildManager.IsInBuildMode) { _selectionCooldown = 0.2f; return; }
            if (_selectionCooldown > 0) { _selectionCooldown -= Time.deltaTime; return; }

            if (!_isDragging && EventSystem.current.IsPointerOverGameObject()) return;
            if (FormationManager.Instance != null && FormationManager.Instance.IsDrawing) return;

            HandleSelectionInput();
            HandleRightClick();
        }

        private bool IsMyUnit(Unit unit)
        {
            if (unit == null || Kingdom.PlayerKingdom == null) return false;
            return unit.ownerKingdomID.Value == Kingdom.PlayerKingdom.kingdomID.Value;
        }

        private void HandleSelectionInput()
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                _startMousePos = Mouse.current.position.ReadValue();
                _isDragging = true;
                if (IsAdditiveKeyHeld()) _unitsBeforeDrag = new List<Unit>(_selectedUnits);
                else { _unitsBeforeDrag.Clear(); DeselectAll(); }
            }

            if (_isDragging && Mouse.current.leftButton.isPressed)
            {
                UpdateSelectionBoxVisual();
                if (Vector2.Distance(_startMousePos, Mouse.current.position.ReadValue()) > 10f)
                    UpdateRealtimeSelection();
            }

            if (_isDragging && Mouse.current.leftButton.wasReleasedThisFrame)
            {
                _isDragging = false;
                if (selectionBox != null) selectionBox.gameObject.SetActive(false);

                if (Vector2.Distance(_startMousePos, Mouse.current.position.ReadValue()) < 10f)
                    HandleSingleClick();

                OnSelectionChanged?.Invoke(_selectedUnits);
            }
        }

        private void UpdateRealtimeSelection()
        {
            Vector2 currentMousePos = Mouse.current.position.ReadValue();
            Vector2 worldStart = _mainCamera.ScreenToWorldPoint(_startMousePos);
            Vector2 worldEnd = _mainCamera.ScreenToWorldPoint(currentMousePos);

            Collider2D[] hits = Physics2D.OverlapAreaAll(worldStart, worldEnd, unitLayerMask);
            List<Unit> newSelection = new List<Unit>(_unitsBeforeDrag);

            foreach (var hit in hits)
            {
                if (hit.TryGetComponent<Unit>(out var unit) && IsMyUnit(unit) && !newSelection.Contains(unit))
                {
                    newSelection.Add(unit);
                }
            }

            foreach (var old in _selectedUnits) if (old != null && !newSelection.Contains(old)) old.Deselect();
            foreach (var newU in newSelection) if (newU != null) newU.Select();

            _selectedUnits = newSelection;
        }

        private void HandleSingleClick()
        {
            Vector2 p = _mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());

            // 1. Units
            RaycastHit2D hitUnit = Physics2D.Raycast(p, Vector2.zero, 0f, unitLayerMask);
            if (hitUnit.collider != null)
            {
                if (hitUnit.collider.TryGetComponent<Unit>(out var unit))
                {
                    if (!IsMyUnit(unit)) return;

                    if (IsAdditiveKeyHeld())
                    {
                        if (_selectedUnits.Contains(unit)) { unit.Deselect(); _selectedUnits.Remove(unit); }
                        else { unit.Select(); _selectedUnits.Add(unit); }
                    }
                    else { DeselectAll(); unit.Select(); _selectedUnits.Add(unit); }
                    return;
                }
            }

            // 2. Buildings
            RaycastHit2D hitBuilding = Physics2D.Raycast(p, Vector2.zero, 0f, constructionLayerMask);
            if (hitBuilding.collider != null)
            {
                if (hitBuilding.collider.TryGetComponent<SelectableBuilding>(out var b))
                {
                    DeselectAll();
                    _selectedBuilding = b;
                    _selectedBuilding.Select();
                }
            }
            else if (!IsAdditiveKeyHeld())
            {
                DeselectAll();
            }
        }

        // --- ИСПРАВЛЕННЫЙ МЕТОД ---
        private void HandleRightClick()
        {
            if (_selectedUnits.Count == 0 || !Mouse.current.rightButton.wasPressedThisFrame) return;

            Vector3 worldPoint = _mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            worldPoint.z = 0;

            // 1. Сначала ищем ВАЖНЫЕ цели (Враги, Стройка, Ресурсы)
            // Исключаем groundLayerMask, чтобы земля не перекрывала ресурсы
            RaycastHit2D hit = Physics2D.Raycast(worldPoint, Vector2.zero, 0f,
                enemiesLayerMask | constructionLayerMask | resourceLayerMask);

            if (hit.collider != null)
            {
                // Атака врага
                if (((1 << hit.collider.gameObject.layer) & enemiesLayerMask) != 0)
                {
                    if (hit.collider.TryGetComponent<Health>(out var enemyHealth))
                    {
                        foreach (var u in _selectedUnits) if (u && u.TryGetComponent<Fighter>(out var f)) f.SetTarget(enemyHealth);
                        return;
                    }
                }

                // Взаимодействие со зданиями
                if (((1 << hit.collider.gameObject.layer) & constructionLayerMask) != 0)
                {
                    var building = hit.collider.GetComponent<Building>();

                    // Атака вражеского здания
                    bool isEnemy = (building != null && Kingdom.PlayerKingdom != null
                        && building.ownerKingdomID.Value != Kingdom.PlayerKingdom.kingdomID.Value);

                    if (isEnemy && hit.collider.TryGetComponent<Health>(out var buildingHealth))
                    {
                        foreach (var u in _selectedUnits) if (u && u.TryGetComponent<Fighter>(out var f)) f.SetTarget(buildingHealth);
                        return;
                    }

                    // Гарнизон в башню
                    if (hit.collider.TryGetComponent<DefenseTower>(out var t))
                    {
                        foreach (var u in _selectedUnits) if (u) u.GetComponent<UnitAI>().CommandGarrison(t);
                        return;
                    }

                    // Строительство
                    if (hit.collider.TryGetComponent<ConstructionSite>(out var s))
                    {
                        foreach (var u in _selectedUnits) if (u) u.GetComponent<UnitAI>().CommandBuild(s);
                        return;
                    }

                    // Работа
                    if (hit.collider.TryGetComponent<JobBuilding>(out var j))
                    {
                        foreach (var u in _selectedUnits) if (u) u.GetComponent<UnitWorker>().SetTarget(j);
                        return;
                    }
                }

                // Сбор ресурсов
                if (((1 << hit.collider.gameObject.layer) & resourceLayerMask) != 0)
                {
                    var res = hit.collider.GetComponentInParent<ResourceNode>();
                    if (res != null) { foreach (var u in _selectedUnits) if (u) u.GetComponent<UnitAI>().CommandGather(res); return; }
                }
            }

            // 2. Если никуда не попали (или попали в землю/воду) - просто ИДЕМ ТУДА
            // Рассчитываем случайный разброс, чтобы юниты не слипались в одной точке
            for (int i = 0; i < _selectedUnits.Count; i++)
            {
                if (_selectedUnits[i] && _selectedUnits[i].TryGetComponent<UnitAI>(out var ai))
                {
                    Vector3 offset = Vector3.zero;
                    if (_selectedUnits.Count > 1)
                    {
                        offset = (Vector3)(UnityEngine.Random.insideUnitCircle * 0.2f * Mathf.Min(_selectedUnits.Count, 5));
                    }
                    ai.CommandMoveTo(worldPoint + offset);
                }
            }
        }
        // ---------------------------

        private void DeselectAll()
        {
            foreach (var u in _selectedUnits) if (u) u.Deselect();
            _selectedUnits.Clear();

            if (_selectedBuilding != null)
            {
                _selectedBuilding.Deselect();
                _selectedBuilding = null;
            }
            OnSelectionChanged?.Invoke(new List<Unit>());
        }

        private bool IsAdditiveKeyHeld() => Keyboard.current.ctrlKey.isPressed || Keyboard.current.shiftKey.isPressed;

        private void UpdateSelectionBoxVisual()
        {
            if (!selectionBox) return;
            selectionBox.gameObject.SetActive(true);

            Vector2 s, c;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, _startMousePos, _mainCamera, out s);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, Mouse.current.position.ReadValue(), _mainCamera, out c);

            selectionBox.sizeDelta = new Vector2(Mathf.Abs(c.x - s.x), Mathf.Abs(c.y - s.y));
            selectionBox.anchoredPosition = s + (c - s) / 2;
        }
    }
}