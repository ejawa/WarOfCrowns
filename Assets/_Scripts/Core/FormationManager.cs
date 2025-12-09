using UnityEngine;
using UnityEngine.InputSystem;
using WarOfCrowns.Units;
using System.Collections.Generic;

namespace WarOfCrowns.Core
{
    [RequireComponent(typeof(LineRenderer))]
    public class FormationManager : MonoBehaviour
    {
        public static FormationManager Instance { get; private set; }

        [Header("Настройки Линии")]
        [SerializeField] private float minVertexDistance = 0.5f;
        [SerializeField] private Color lineColor = new Color(0, 1, 0, 0.5f);
        [SerializeField] private float lineWidth = 0.2f;

        private LineRenderer _lineRenderer;
        private UnitSelectionController _selectionController;
        private Camera _mainCamera;
        private List<Vector3> _drawnPoints = new List<Vector3>();
        private bool _isDrawing;

        public bool IsDrawing => _isDrawing;

        private void Awake()
        {
            Instance = this;
            _lineRenderer = GetComponent<LineRenderer>();
            _mainCamera = Camera.main;

            _lineRenderer.startWidth = lineWidth;
            _lineRenderer.endWidth = lineWidth;
            _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            _lineRenderer.startColor = lineColor;
            _lineRenderer.endColor = lineColor;
            _lineRenderer.positionCount = 0;
        }

        private void Start()
        {
            _selectionController = FindObjectOfType<UnitSelectionController>();
        }

        private void Update()
        {
            if (_selectionController == null) return;
            var units = _selectionController.GetSelectedUnits();
            if (units == null || units.Count == 0) return;

            // Логика: держим B -> Рисуем ЛКМ
            if (Keyboard.current.bKey.isPressed)
            {
                HandleInput(units);
            }
            else if (_isDrawing)
            {
                CancelFormation();
            }
        }

        private void HandleInput(List<Unit> units)
        {
            Vector3 mousePos = GetMouseWorldPos();

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                _isDrawing = true;
                _drawnPoints.Clear();
                AddPoint(mousePos);
            }

            if (_isDrawing && Mouse.current.leftButton.isPressed)
            {
                if (_drawnPoints.Count > 0)
                {
                    float dist = Vector3.Distance(_drawnPoints[_drawnPoints.Count - 1], mousePos);
                    if (dist > minVertexDistance) AddPoint(mousePos);
                }
            }

            if (_isDrawing && Mouse.current.leftButton.wasReleasedThisFrame)
            {
                if (_drawnPoints.Count > 0 && Vector3.Distance(_drawnPoints[_drawnPoints.Count - 1], mousePos) > 0.1f)
                    AddPoint(mousePos);

                ExecuteFormationMove(units);
                CancelFormation();
            }
        }

        private void AddPoint(Vector3 pos)
        {
            _drawnPoints.Add(pos);
            _lineRenderer.positionCount = _drawnPoints.Count;
            _lineRenderer.SetPosition(_drawnPoints.Count - 1, pos);
        }

        private void ExecuteFormationMove(List<Unit> units)
        {
            if (units.Count == 0) return;

            // Если просто кликнули (одна точка), все идут в эту точку
            if (_drawnPoints.Count < 2)
            {
                Vector3 target = _drawnPoints.Count > 0 ? _drawnPoints[0] : GetMouseWorldPos();
                foreach (var unit in units)
                    if (unit.TryGetComponent<UnitAI>(out var ai)) ai.CommandMoveTo(target);
                return;
            }

            // Расчет длины
            float totalLength = 0f;
            List<float> segmentLengths = new List<float>();
            for (int i = 0; i < _drawnPoints.Count - 1; i++)
            {
                float dist = Vector3.Distance(_drawnPoints[i], _drawnPoints[i + 1]);
                segmentLengths.Add(dist);
                totalLength += dist;
            }

            // Распределение
            float spacing = units.Count > 1 ? totalLength / (units.Count - 1) : 0;
            for (int i = 0; i < units.Count; i++)
            {
                float targetDist = spacing * i;
                if (units.Count == 1) targetDist = totalLength / 2f;

                Vector3 targetPos = GetPointOnPath(targetDist, segmentLengths);
                if (units[i].TryGetComponent<UnitAI>(out var ai))
                {
                    ai.CommandMoveTo(targetPos);
                }
            }
        }

        private Vector3 GetPointOnPath(float targetDist, List<float> segmentLengths)
        {
            float currentDist = 0f;
            for (int i = 0; i < segmentLengths.Count; i++)
            {
                float segmentLen = segmentLengths[i];
                if (currentDist + segmentLen >= targetDist)
                {
                    float remaining = targetDist - currentDist;
                    float t = remaining / segmentLen;
                    return Vector3.Lerp(_drawnPoints[i], _drawnPoints[i + 1], t);
                }
                currentDist += segmentLen;
            }
            return _drawnPoints[_drawnPoints.Count - 1];
        }

        private void CancelFormation()
        {
            _isDrawing = false;
            _drawnPoints.Clear();
            _lineRenderer.positionCount = 0;
        }

        private Vector3 GetMouseWorldPos()
        {
            Vector3 mPos = Mouse.current.position.ReadValue();
            Vector3 wPos = _mainCamera.ScreenToWorldPoint(mPos);
            wPos.z = 0;
            return wPos;
        }
    }
}