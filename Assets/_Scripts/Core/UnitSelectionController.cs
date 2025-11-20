using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using WarOfCrowns.Units;
using WarOfCrowns.World;
using WarOfCrowns.Buildings;
using WarOfCrowns.Core; // Важно для Kingdom

namespace WarOfCrowns.Core
{
    public class UnitSelectionController : MonoBehaviour
    {
        [SerializeField] private LayerMask unitLayerMask;
        [SerializeField] private LayerMask groundLayerMask;
        [SerializeField] private LayerMask resourceLayerMask;
        [SerializeField] private LayerMask constructionLayerMask;
        [SerializeField] private LayerMask enemiesLayerMask;

        private Camera _mainCamera;
        private Unit _selectedUnit;
        private SelectableBuilding _selectedBuilding;

        private void Awake()
        {
            _mainCamera = Camera.main;
        }

        private void Update()
        {
            // Убираем if(!enabled) return;, чтобы видеть логи даже если он выключен

            // --- СУПЕР-ПРОСТАЯ ПРОВЕРКА ---
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
            
            }
            // --- ---

            if (!enabled) return;
            HandleLeftClick();
            HandleRightClick();
        }

        private void HandleLeftClick()
        {
            if (EventSystem.current.IsPointerOverGameObject())
            {
               
                return;
            }
            if (!Mouse.current.leftButton.wasPressedThisFrame) return;

            Debug.Log("--- Left Click Detected ---");

            Vector2 worldPoint = _mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());

            // Сначала пускаем луч ТОЛЬКО за юнитами
            RaycastHit2D unitHit = Physics2D.Raycast(worldPoint, Vector2.zero, Mathf.Infinity, unitLayerMask);
            if (unitHit.collider != null)
            {
                Debug.Log($"SUCCESS: Raycast hit '{unitHit.collider.name}' on the UNIT layer!");
                if (unitHit.collider.TryGetComponent<Unit>(out var hitUnit))
                {
                    Debug.Log("Unit component found. Selecting.");
                    if (_selectedBuilding != null) _selectedBuilding.Deselect();
                    _selectedBuilding = null;
                    if (_selectedUnit != null && _selectedUnit != hitUnit) _selectedUnit.Deselect();
                    _selectedUnit = hitUnit;
                    _selectedUnit.Select();
                }
                else
                {
                    Debug.LogError($"CRITICAL: Hit a collider on the 'Units' layer, but it has NO 'Unit' SCRIPT!", unitHit.collider.gameObject);
                }
                return; // Выходим, так как мы нашли юнита
            }

            // Если не попали в юнита, пускаем луч за зданиями (они на слое Construction)
            RaycastHit2D buildingHit = Physics2D.Raycast(worldPoint, Vector2.zero, Mathf.Infinity, constructionLayerMask);
            if (buildingHit.collider != null)
            {
                Debug.Log($"HIT SOMETHING ON BUILDING/CONSTRUCTION LAYER: '{buildingHit.collider.name}'");
                if (buildingHit.collider.TryGetComponent<SelectableBuilding>(out var hitBuilding))
                {
                    Debug.Log("SelectableBuilding component found. Selecting building.");
                    if (_selectedUnit != null) _selectedUnit.Deselect();
                    _selectedUnit = null;
                    if (_selectedBuilding != null && _selectedBuilding != hitBuilding) _selectedBuilding.Deselect();
                    _selectedBuilding = hitBuilding;
                    _selectedBuilding.Select();
                }
                return;
            }

            // Если не попали ни в юнита, ни в здание - значит, это земля
            Debug.Log("Raycast hit NO units and NO buildings. Deselecting all.");
            DeselectAll();
        }

        private void HandleRightClick()
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;
            if (!Mouse.current.rightButton.wasPressedThisFrame || _selectedUnit == null) return;

            Vector2 worldPoint = _mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());

            // Компоненты
            var motor = _selectedUnit.GetComponent<UnitMotor>();
            var gatherer = _selectedUnit.GetComponent<UnitGatherer>();
            var builder = _selectedUnit.GetComponent<UnitBuilder>();
            var fighter = _selectedUnit.GetComponent<Fighter>();
            var worker = _selectedUnit.GetComponent<UnitWorker>(); // <-- НОВЫЙ
            var unitLogic = _selectedUnit.GetComponent<Unit>(); // <-- НОВЫЙ

            // При любом приказе игрока говорим юниту: "Забудь про голод на 20 секунд!"
            unitLogic.SetManualCommandOverride();

            // 1. АТАКА
            RaycastHit2D enemyHit = Physics2D.Raycast(worldPoint, Vector2.zero, 0f, enemiesLayerMask);
            if (enemyHit.collider != null && enemyHit.collider.TryGetComponent<Health>(out var enemyHealth))
            {
                if (fighter != null)
                {
                    gatherer?.StopGathering();
                    builder?.Cancel();
                    worker?.StopWorking(); // <-- Отменяем работу
                    fighter.Attack(enemyHealth);
                    return;
                }
            }

            // 2. РАБОТА НА ЗДАНИИ (Новый приоритет)
            RaycastHit2D jobHit = Physics2D.Raycast(worldPoint, Vector2.zero, 0f, constructionLayerMask);
            if (jobHit.collider != null && jobHit.collider.TryGetComponent<JobBuilding>(out var jobBuilding))
            {
                // Убедимся, что это не стройка
                if (jobHit.collider.GetComponent<ConstructionSite>() == null)
                {
                    if (worker != null)
                    {
                        fighter?.Cancel();
                        builder?.Cancel();
                        gatherer?.StopGathering();
                        worker.SetTarget(jobBuilding);
                        return;
                    }
                }
            }

            // 3. СТРОИТЕЛЬСТВО
            RaycastHit2D constructionHit = Physics2D.Raycast(worldPoint, Vector2.zero, 0f, constructionLayerMask);
            if (constructionHit.collider != null && constructionHit.collider.TryGetComponent<ConstructionSite>(out var site))
            {
                if (builder != null)
                {
                    fighter?.Cancel();
                    gatherer?.StopGathering();
                    worker?.StopWorking(); // <-- Отменяем работу
                    builder.SetTarget(site);
                    return;
                }
            }

            // 4. СБОР РЕСУРСОВ
            RaycastHit2D resourceHit = Physics2D.Raycast(worldPoint, Vector2.zero, 0f, resourceLayerMask);
            if (resourceHit.collider != null && resourceHit.collider.TryGetComponent<ResourceNode>(out var resourceNode))
            {
                if (gatherer != null)
                {
                    fighter?.Cancel();
                    builder?.Cancel();
                    worker?.StopWorking(); // <-- Отменяем работу
                    gatherer.SetTarget(resourceNode);
                    return;
                }
            }

            // 5. ДВИЖЕНИЕ
            RaycastHit2D groundHit = Physics2D.Raycast(worldPoint, Vector2.zero, 0f, groundLayerMask);
            if (groundHit.collider != null)
            {
                if (motor != null)
                {
                    fighter?.Cancel();
                    builder?.Cancel();
                    gatherer?.StopGathering();
                    worker?.StopWorking(); // <-- Отменяем работу

                    motor.MoveTo(groundHit.point);
                }
            }
        }

        private void DeselectAll()
        {
            if (_selectedUnit != null) _selectedUnit.Deselect();
            _selectedUnit = null;
            if (_selectedBuilding != null) _selectedBuilding.Deselect();
            _selectedBuilding = null;
        }
    }
}