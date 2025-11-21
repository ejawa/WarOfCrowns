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
        [SerializeField] private LayerMask constructionLayerMask;
        [SerializeField] private LayerMask enemiesLayerMask;

        [Header("UI Рамки")]
        [SerializeField] private RectTransform selectionBox;

        private Camera _mainCamera;
        private RectTransform _canvasRect;

        // Основной список текущих выделенных
        private List<Unit> _selectedUnits = new List<Unit>();

        // Список тех, кто был выделен ДО начала перетаскивания (для Ctrl)
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
            if (!enabled) return;
            HandleSelectionInput();
            HandleRightClick();
        }

        // --- ЛОГИКА ВВОДА ---

        private bool IsAdditiveKeyHeld()
        {
            // Проверяем Ctrl или Shift (для удобства)
            return Keyboard.current.ctrlKey.isPressed || Keyboard.current.shiftKey.isPressed;
        }

        private void HandleSelectionInput()
        {
            // 1. НАЖАТИЕ
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (EventSystem.current.IsPointerOverGameObject()) return;

                _startMousePos = Mouse.current.position.ReadValue();
                _isDragging = true;

                // Запоминаем, кто был выделен до начала клика/драга
                if (IsAdditiveKeyHeld())
                {
                    _unitsBeforeDrag = new List<Unit>(_selectedUnits); // Копируем текущий список
                }
                else
                {
                    _unitsBeforeDrag.Clear(); // Если Ctrl не зажат, начинаем с чистого листа

                    // Сразу очищаем текущее выделение (визуально), чтобы начать новое
                    DeselectAllUnits();
                    if (_selectedBuilding != null) { _selectedBuilding.Deselect(); _selectedBuilding = null; }
                }
            }

            // 2. УДЕРЖАНИЕ (Драг)
            if (_isDragging && Mouse.current.leftButton.isPressed)
            {
                UpdateSelectionBoxVisual();

                // Если мышку немного сдвинули - начинаем "Живое выделение"
                if (Vector2.Distance(_startMousePos, Mouse.current.position.ReadValue()) > 5f)
                {
                    UpdateRealtimeSelection();
                }
            }

            // 3. ОТПУСКАНИЕ
            if (_isDragging && Mouse.current.leftButton.wasReleasedThisFrame)
            {
                _isDragging = false;
                if (selectionBox != null) selectionBox.gameObject.SetActive(false);

                // Если это был просто клик (не драг)
                if (Vector2.Distance(_startMousePos, Mouse.current.position.ReadValue()) < 5f)
                {
                    HandleSingleClick();
                }
                // Если был драг - мы уже выделили всех в UpdateRealtimeSelection, 
                // просто финально обновляем UI (на всякий случай)
                OnSelectionChanged?.Invoke(_selectedUnits);
            }
        }

        // --- ЖИВОЕ ВЫДЕЛЕНИЕ РАМКОЙ ---
        private void UpdateRealtimeSelection()
        {
            // 1. Вычисляем границы рамки в мире
            Vector2 currentMousePos = Mouse.current.position.ReadValue();
            Vector2 worldStart = _mainCamera.ScreenToWorldPoint(_startMousePos);
            Vector2 worldEnd = _mainCamera.ScreenToWorldPoint(currentMousePos);

            float minX = Mathf.Min(worldStart.x, worldEnd.x);
            float maxX = Mathf.Max(worldStart.x, worldEnd.x);
            float minY = Mathf.Min(worldStart.y, worldEnd.y);
            float maxY = Mathf.Max(worldStart.y, worldEnd.y);

            // 2. Находим всех, кто сейчас под рамкой
            Collider2D[] hits = Physics2D.OverlapAreaAll(new Vector2(minX, minY), new Vector2(maxX, maxY), unitLayerMask);

            // 3. Формируем НОВЫЙ список выделения
            // Начинаем с тех, кто был выделен ДО драга (если зажат Ctrl)
            List<Unit> newSelection = new List<Unit>(_unitsBeforeDrag);

            foreach (var hit in hits)
            {
                if (hit.TryGetComponent<Unit>(out var unit))
                {
                    // Добавляем только если его еще нет в списке
                    if (!newSelection.Contains(unit))
                    {
                        newSelection.Add(unit);
                    }
                }
            }

            // 4. СИНХРОНИЗАЦИЯ (Самая хитрая часть)
            // Нам нужно визуально выделить новых и снять выделение с тех, кто выпал из рамки

            // Снимаем выделение с тех, кого НЕТ в новом списке
            foreach (var oldUnit in _selectedUnits)
            {
                if (!newSelection.Contains(oldUnit)) oldUnit.Deselect();
            }

            // Выделяем тех, кто ЕСТЬ в новом списке
            foreach (var newUnit in newSelection)
            {
                newUnit.Select();
            }

            // Обновляем главный список
            _selectedUnits = newSelection;

            // Обновляем UI в реальном времени
            OnSelectionChanged?.Invoke(_selectedUnits);
        }

        // --- ОДИНОЧНЫЙ КЛИК (С поддержкой Ctrl) ---
        private void HandleSingleClick()
        {
            Vector2 worldPoint = _mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());

            // 1. Юнит
            RaycastHit2D hitUnit = Physics2D.Raycast(worldPoint, Vector2.zero, Mathf.Infinity, unitLayerMask);
            if (hitUnit.collider != null && hitUnit.collider.TryGetComponent<Unit>(out var unit))
            {
                if (IsAdditiveKeyHeld())
                {
                    // Если Ctrl зажат:
                    if (_selectedUnits.Contains(unit))
                    {
                        // Если уже был выделен - снимаем (Toggle)
                        unit.Deselect();
                        _selectedUnits.Remove(unit);
                    }
                    else
                    {
                        // Если не был - добавляем
                        unit.Select();
                        _selectedUnits.Add(unit);
                    }
                }
                else
                {
                    // Если Ctrl НЕ зажат:
                    DeselectAll(); // Сброс всех
                    unit.Select();
                    _selectedUnits.Add(unit);
                }
                OnSelectionChanged?.Invoke(_selectedUnits);
                return;
            }

            // 2. Здание (Здания обычно выделяются по одному, Ctrl тут редко нужен)
            RaycastHit2D hitBuilding = Physics2D.Raycast(worldPoint, Vector2.zero, Mathf.Infinity, constructionLayerMask);
            if (hitBuilding.collider != null && hitBuilding.collider.TryGetComponent<SelectableBuilding>(out var building))
            {
                DeselectAll();
                _selectedBuilding = building;
                _selectedBuilding.Select();
                return;
            }

            // 3. Пустота
            if (!IsAdditiveKeyHeld())
            {
                DeselectAll();
            }
        }

        private void DeselectAllUnits()
        {
            foreach (var unit in _selectedUnits) unit.Deselect();
            _selectedUnits.Clear();
        }

        private void DeselectAll()
        {
            DeselectAllUnits();

            if (_selectedBuilding != null) _selectedBuilding.Deselect();
            _selectedBuilding = null;

            OnSelectionChanged?.Invoke(new List<Unit>());
        }

        private void UpdateSelectionBoxVisual()
        {
            if (selectionBox == null || _canvasRect == null) return;
            if (!selectionBox.gameObject.activeInHierarchy) selectionBox.gameObject.SetActive(true);

            Vector2 currentMousePos = Mouse.current.position.ReadValue();
            Vector2 localStart;
            Vector2 localCurrent;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, _startMousePos, _mainCamera, out localStart);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, currentMousePos, _mainCamera, out localCurrent);

            Vector2 size = localCurrent - localStart;
            Vector2 center = localStart + (size / 2);

            selectionBox.sizeDelta = new Vector2(Mathf.Abs(size.x), Mathf.Abs(size.y));
            selectionBox.anchoredPosition = center;
        }

        // --- ПРИКАЗЫ (Без изменений, только вставляем в конец класса) ---
        private void HandleRightClick()
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;
            if (!Mouse.current.rightButton.wasPressedThisFrame) return;
            if (_selectedUnits.Count == 0) return;

            Vector2 worldPoint = _mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());

            // Определение цели
            Health targetEnemy = null;
            ConstructionSite targetSite = null;
            ResourceNode targetResource = null;
            JobBuilding targetJob = null;
            bool isMoveCommand = true;

            RaycastHit2D enemyHit = Physics2D.Raycast(worldPoint, Vector2.zero, 0f, enemiesLayerMask);
            if (enemyHit.collider != null && enemyHit.collider.TryGetComponent<Health>(out targetEnemy)) isMoveCommand = false;
            else
            {
                RaycastHit2D jobHit = Physics2D.Raycast(worldPoint, Vector2.zero, 0f, constructionLayerMask);
                if (jobHit.collider != null && jobHit.collider.TryGetComponent<JobBuilding>(out targetJob) && jobHit.collider.GetComponent<ConstructionSite>() == null) isMoveCommand = false;

                if (isMoveCommand)
                {
                    RaycastHit2D constructionHit = Physics2D.Raycast(worldPoint, Vector2.zero, 0f, constructionLayerMask);
                    if (constructionHit.collider != null && constructionHit.collider.TryGetComponent<ConstructionSite>(out targetSite)) isMoveCommand = false;
                }
            }
            if (isMoveCommand)
            {
                RaycastHit2D resourceHit = Physics2D.Raycast(worldPoint, Vector2.zero, 0f, resourceLayerMask);
                if (resourceHit.collider != null && resourceHit.collider.TryGetComponent<ResourceNode>(out targetResource)) isMoveCommand = false;
            }

            // Раздача приказов
            foreach (Unit unit in _selectedUnits)
            {
                var motor = unit.GetComponent<UnitMotor>();
                var gatherer = unit.GetComponent<UnitGatherer>();
                var builder = unit.GetComponent<UnitBuilder>();
                var fighter = unit.GetComponent<Fighter>();
                var worker = unit.GetComponent<UnitWorker>();

                unit.GetComponent<Unit>().SetManualCommandOverride();

                gatherer?.StopGathering();
                builder?.Cancel();
                fighter?.Cancel();
                worker?.StopWorking();

                if (targetEnemy != null && fighter != null) fighter.Attack(targetEnemy);
                else if (targetJob != null && worker != null) worker.SetTarget(targetJob);
                else if (targetSite != null && builder != null) builder.SetTarget(targetSite);
                else if (targetResource != null && gatherer != null) gatherer.SetTarget(targetResource);
                else if (isMoveCommand && motor != null)
                {
                    Vector2 randomOffset = Random.insideUnitCircle * 0.5f * Mathf.Min(_selectedUnits.Count, 5);
                    motor.MoveTo(worldPoint + randomOffset);
                }
            }
        }
    }
}