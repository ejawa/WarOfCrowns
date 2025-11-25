using UnityEngine;
using UnityEngine.InputSystem;
using WarOfCrowns.Buildings;
using System.Collections.Generic;
using WarOfCrowns.Core;
using WarOfCrowns.World;

namespace WarOfCrowns.Core
{
    public class BuildManager : MonoBehaviour
    {
        [Header("Настройки")]
        [SerializeField] private GameObject buildMenuPanel;
        public List<GameObject> buildableFoundations;

        [Header("Ограничения")]
        public LayerMask obstacleLayer;

        [Header("Расчистка")]
        public LayerMask resourcesToClearLayer;

        // УВЕЛИЧИЛ ЛИМИТ ДО 10
        [Tooltip("Множитель радиуса очистки. 1 = размер здания. 2 = двойной размер. 5 = огромная поляна.")]
        public float fixedClearanceBuffer = 2.0f;

        private GameObject _ghostInstance;
        private GameObject _foundationToBuild;
        private bool _isBuildingMode;
        private SpriteRenderer _ghostRenderer;

        private void Update()
        {
            if (_isBuildingMode)
            {
                Vector3 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                mousePos.z = 0;

                if (_ghostInstance != null)
                {
                    _ghostInstance.transform.position = mousePos;

                    bool canBuild = IsValidPlacement(mousePos, _foundationToBuild);
                    UpdateGhostColor(canBuild);

                    if (Mouse.current.leftButton.wasPressedThisFrame && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                    {
                        if (canBuild) PlaceFoundation();
                        else Debug.Log("BuildManager: Нельзя строить здесь!");
                    }
                }

                if (Mouse.current.rightButton.wasPressedThisFrame) ExitBuildMode();
            }
        }

        public bool IsValidPlacement(Vector3 position, GameObject prefab)
        {
            if (prefab == null) return false;

            Vector2 size = GetPrefabSize(prefab);

            // Проверка препятствий (чуть уже здания)
            Collider2D hit = Physics2D.OverlapBox(position, size * 0.9f, 0f, obstacleLayer);
            if (hit != null) return false;

            if (WorldGenerator.Instance != null)
            {
                Vector3[] checkPoints = new Vector3[]
                {
                    position,
                    position + new Vector3(size.x/2, size.y/2, 0),
                    position + new Vector3(-size.x/2, size.y/2, 0),
                    position + new Vector3(size.x/2, -size.y/2, 0),
                    position + new Vector3(-size.x/2, -size.y/2, 0)
                };

                foreach (var p in checkPoints)
                {
                    string biome = WorldGenerator.Instance.GetBiomeAt(p);
                    if (biome.Contains("Water") || biome.Contains("Ocean") || biome.Contains("Sea") ||
                        biome.Contains("Mountain") || biome.Contains("Rock") || biome.Contains("Shallow"))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public void ClearResources(Vector3 position, GameObject prefab)
        {
            Vector2 buildingSize = GetPrefabSize(prefab);

            // НОВАЯ ФОРМУЛА: Размер здания + Фиксированный запас со всех сторон
            // Если здание 2x2, а буфер 2.0, то зона будет 6x6.
            Vector2 finalSize = buildingSize + new Vector2(fixedClearanceBuffer, fixedClearanceBuffer);

            // Используем OverlapBoxAll
            Collider2D[] hits = Physics2D.OverlapBoxAll(position, finalSize, 0f, resourcesToClearLayer);

            foreach (var hit in hits)
            {
                // Ищем скрипт на самом объекте или его родителе
                var resource = hit.GetComponentInParent<ResourceNode>();

                if (resource != null)
                {
                    Destroy(resource.gameObject);
                }
                else
                {
                    // Если скрипта нет, но слой совпал - удаляем объект
                    Destroy(hit.gameObject);
                }
            }
        }

        // Вспомогательный метод для получения размера
        private Vector2 GetPrefabSize(GameObject prefab)
        {
            if (prefab == null) return Vector2.one;
            var col = prefab.GetComponent<Collider2D>();
            return col != null ? col.bounds.size : Vector2.one;
        }

        private void UpdateGhostColor(bool allowed)
        {
            if (_ghostRenderer != null)
            {
                Color c = allowed ? Color.green : Color.red;
                c.a = 0.5f;
                _ghostRenderer.color = c;
            }
        }

        // --- ВИЗУАЛИЗАЦИЯ РАДИУСА В РЕДАКТОРЕ ---
        private void OnDrawGizmos()
        {
            if (_isBuildingMode && _ghostInstance != null && _foundationToBuild != null)
            {
                Vector2 buildingSize = GetPrefabSize(_foundationToBuild);
                Vector2 finalSize = buildingSize + new Vector2(fixedClearanceBuffer, fixedClearanceBuffer);

                Gizmos.color = new Color(1, 0, 0, 0.4f); // Красный полупрозрачный
                Gizmos.DrawCube(_ghostInstance.transform.position, finalSize);

                Gizmos.color = Color.red; // Рамка
                Gizmos.DrawWireCube(_ghostInstance.transform.position, finalSize);
            }
        }
        // ----------------------------------------

        public void ToggleBuildMenu()
        {
            if (buildMenuPanel != null) buildMenuPanel.SetActive(!buildMenuPanel.activeSelf);
        }

        public void EnterBuildMode(GameObject foundationPrefab)
        {
            if (_isBuildingMode) ExitBuildMode();
            _foundationToBuild = foundationPrefab;
            if (_foundationToBuild == null) return;
            _isBuildingMode = true;
            _ghostInstance = Instantiate(_foundationToBuild);
            if (_ghostInstance.TryGetComponent<SpriteRenderer>(out var mainRenderer)) _ghostRenderer = mainRenderer;

            SpriteRenderer[] allRenderers = _ghostInstance.GetComponentsInChildren<SpriteRenderer>();
            foreach (var r in allRenderers) if (r.gameObject != _ghostInstance) r.gameObject.SetActive(false);

            if (_ghostInstance.GetComponent<Collider2D>() != null) Destroy(_ghostInstance.GetComponent<Collider2D>());
            if (_ghostInstance.GetComponent<ConstructionSite>() != null) Destroy(_ghostInstance.GetComponent<ConstructionSite>());

            if (buildMenuPanel != null) buildMenuPanel.SetActive(false);
        }

        private void PlaceFoundation()
        {
            ClearResources(_ghostInstance.transform.position, _foundationToBuild);
            GameObject foundationInstance = Instantiate(_foundationToBuild, _ghostInstance.transform.position, Quaternion.identity);
            if (foundationInstance.TryGetComponent<ConstructionSite>(out var site))
            {
                site.OwningKingdom = Kingdom.PlayerKingdom;
            }
            ExitBuildMode();
        }

        private void ExitBuildMode()
        {
            _isBuildingMode = false;
            _foundationToBuild = null;
            if (_ghostInstance != null) Destroy(_ghostInstance);
        }
    }
}