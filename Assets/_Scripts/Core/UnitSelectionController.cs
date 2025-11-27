using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using WarOfCrowns.Units;
using WarOfCrowns.World;
using WarOfCrowns.Buildings;
using System.Collections.Generic;

namespace WarOfCrowns.Core
{
    public class UnitSelectionController : MonoBehaviour
    {
        [Header("Слои")]
        [SerializeField] private LayerMask unitLayerMask;
        [SerializeField] private LayerMask groundLayerMask;
        [SerializeField] private LayerMask resourceLayerMask;
        [SerializeField] private LayerMask constructionLayerMask; // Сюда входят и здания, и стройплощадки
        [SerializeField] private LayerMask enemiesLayerMask;

        [Header("UI Рамки")]
        [SerializeField] private RectTransform selectionBox;

        private Camera _mainCamera;
        private RectTransform _canvasRect;

        // Списки выделения
        private List<Unit> _selectedUnits = new List<Unit>();
        private List<Unit> _unitsBeforeDrag = new List<Unit>();
        private SelectableBuilding _selectedBuilding;

        private Vector2 _startMousePos;
        private bool _isDragging;

        public static event System.Action<List<Unit>> OnSelectionChanged;

        private void Awake()
        {
            _mainCamera = Camera.main;
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null) _canvasRect = canvas.GetComponent<RectTransform>();
        }

        private void Update()
        {
            // Блокировка кликов через UI (но не во время драга)
            if (!_isDragging && EventSystem.current.IsPointerOverGameObject()) return;

            HandleSelectionInput();
            HandleRightClick();
        }

        // --- ГЛАВНАЯ ПРОВЕРКА: МОЙ ЛИ ЭТО ЮНИТ? ---
        private bool IsMyUnit(Unit unit)
        {
            if (Kingdom.PlayerKingdom == null) return false;
            return unit.ownerKingdomID.Value == Kingdom.PlayerKingdom.kingdomID;
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
                if (hit.TryGetComponent<Unit>(out var unit))
                {
                    // ФИЛЬТР: Выделяем только своих!
                    if (!IsMyUnit(unit)) continue;
                    if (!newSelection.Contains(unit)) newSelection.Add(unit);
                }
            }

            foreach (var oldUnit in _selectedUnits) if (!newSelection.Contains(oldUnit)) oldUnit.Deselect();
            foreach (var newUnit in newSelection) newUnit.Select();
            _selectedUnits = newSelection;
        }

        private void HandleSingleClick()
        {
            Vector2 worldPoint = _mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());

            // 1. Юниты
            RaycastHit2D hitUnit = Physics2D.Raycast(worldPoint, Vector2.zero, 0f, unitLayerMask);
            if (hitUnit.collider != null && hitUnit.collider.TryGetComponent<Unit>(out var unit))
            {
                // ФИЛЬТР: Свои юниты
                if (!IsMyUnit(unit)) return;

                if (IsAdditiveKeyHeld())
                {
                    if (_selectedUnits.Contains(unit)) { unit.Deselect(); _selectedUnits.Remove(unit); }
                    else { unit.Select(); _selectedUnits.Add(unit); }
                }
                else
                {
                    DeselectAll(); unit.Select(); _selectedUnits.Add(unit);
                }
                return;
            }

            // 2. Здания (Здания можно выделять любые, чтобы видеть инфо, но управлять только своими)
            // (Управление ограничивается внутри самих UI скриптов типа TownHallUI)
            RaycastHit2D hitBuilding = Physics2D.Raycast(worldPoint, Vector2.zero, 0f, constructionLayerMask);
            if (hitBuilding.collider != null && hitBuilding.collider.TryGetComponent<SelectableBuilding>(out var building))
            {
                DeselectAll();
                _selectedBuilding = building;
                _selectedBuilding.Select();
            }
        }

        private void HandleRightClick()
        {
            if (_selectedUnits.Count == 0) return;
            if (!Mouse.current.rightButton.wasPressedThisFrame) return;

            Vector3 worldPoint = _mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            worldPoint.z = 0;

            // --- ПРИОРИТЕТЫ КЛИКОВ ---

            // 1. Атака врага
            RaycastHit2D enemyHit = Physics2D.Raycast(worldPoint, Vector2.zero, 0f, enemiesLayerMask);
            if (enemyHit.collider != null && enemyHit.collider.TryGetComponent<Health>(out var targetEnemy))
            {
                foreach (var unit in _selectedUnits)
                {
                    // Если есть компонент бойца - атакуем
                    if (unit.TryGetComponent<Fighter>(out var fighter)) fighter.Attack(targetEnemy);
                }
                return;
            }

            // 2. Вход в БАШНЮ (Гарнизон)
            RaycastHit2D towerHit = Physics2D.Raycast(worldPoint, Vector2.zero, 0f, constructionLayerMask);
            if (towerHit.collider != null && towerHit.collider.TryGetComponent<DefenseTower>(out var tower))
            {
                // Проверяем, наша ли это башня (через компонент Building)
                var bLogic = tower.GetComponent<Building>();
                if (bLogic != null && Kingdom.PlayerKingdom != null && bLogic.ownerKingdomID.Value == Kingdom.PlayerKingdom.kingdomID)
                {
                    foreach (var unit in _selectedUnits)
                    {
                        if (unit.TryGetComponent<UnitAI>(out var ai)) ai.CommandGarrison(tower);
                    }
                    return;
                }
            }

            // 3. Работа / Стройка (JobBuilding или ConstructionSite)
            RaycastHit2D jobHit = Physics2D.Raycast(worldPoint, Vector2.zero, 0f, constructionLayerMask);
            if (jobHit.collider != null)
            {
                // Если это Стройка
                if (jobHit.collider.TryGetComponent<ConstructionSite>(out var site))
                {
                    // Можно добавить проверку "своя стройка", если нужно
                    foreach (var unit in _selectedUnits)
                        if (unit.TryGetComponent<UnitBuilder>(out var builder)) builder.SetTarget(site);
                    return;
                }
                // Если это Работа (Ферма и т.д.)
                if (jobHit.collider.TryGetComponent<JobBuilding>(out var job))
                {
                    foreach (var unit in _selectedUnits)
                        if (unit.TryGetComponent<UnitWorker>(out var worker)) worker.SetTarget(job);
                    return;
                }
            }

            // 4. Сбор ресурсов
            RaycastHit2D resourceHit = Physics2D.Raycast(worldPoint, Vector2.zero, 0f, resourceLayerMask);
            if (resourceHit.collider != null && resourceHit.collider.TryGetComponent<ResourceNode>(out var resource))
            {
                foreach (var unit in _selectedUnits)
                    if (unit.TryGetComponent<UnitGatherer>(out var gatherer)) gatherer.SetTarget(resource);
                return;
            }

            // 5. Обычное движение
            bool isMoveCommand = true; // Если никуда не попали - идем
            if (isMoveCommand)
            {
                // Разброс юнитов, чтобы не слипались
                for (int i = 0; i < _selectedUnits.Count; i++)
                {
                    Vector2 randomOffset = Random.insideUnitCircle * 0.2f * Mathf.Min(_selectedUnits.Count, 5);
                    if (_selectedUnits[i].TryGetComponent<UnitMotor>(out var motor))
                        motor.MoveTo(worldPoint + (Vector3)randomOffset);
                }
            }
        }

        private void DeselectAll()
        {
            foreach (var u in _selectedUnits) u.Deselect();
            _selectedUnits.Clear();
            if (_selectedBuilding != null) { _selectedBuilding.Deselect(); _selectedBuilding = null; }
            OnSelectionChanged?.Invoke(new List<Unit>());
        }

        private bool IsAdditiveKeyHeld() => Keyboard.current.ctrlKey.isPressed || Keyboard.current.shiftKey.isPressed;

        private void UpdateSelectionBoxVisual()
        {
            if (!selectionBox) return;
            selectionBox.gameObject.SetActive(true);
            Vector2 localStart;
            Vector2 localCurrent;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, _startMousePos, _mainCamera, out localStart);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, Mouse.current.position.ReadValue(), _mainCamera, out localCurrent);

            Vector2 size = localCurrent - localStart;
            Vector2 center = localStart + (size / 2);
            selectionBox.sizeDelta = new Vector2(Mathf.Abs(size.x), Mathf.Abs(size.y));
            selectionBox.anchoredPosition = center;
        }
    }
}