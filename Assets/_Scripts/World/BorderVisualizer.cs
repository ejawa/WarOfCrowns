using UnityEngine;
using System.Collections.Generic;
using WarOfCrowns.World;

namespace WarOfCrowns.Core
{
    public class BorderVisualizer : MonoBehaviour
    {
        [Header("Настройки")]
        [SerializeField] private GameObject linePrefab;
        [SerializeField] private float heightOffset = 0.15f;

        private List<GameObject> _spawnedLines = new List<GameObject>();
        private bool _isVisible = false;

        private void Start()
        {
            StartCoroutine(InitRoutine());
        }

        private System.Collections.IEnumerator InitRoutine()
        {
            // 1. Ждем появления инстанса
            while (WorldGenerator.Instance == null || Kingdom.PlayerKingdom == null)
            {
                yield return null;
            }

            // 2. Ждем, пока карта сгенерируется (флаг станет true)
            // Это самое важное изменение!
            while (!WorldGenerator.Instance.IsWorldGenerated)
            {
                yield return null;
            }

            // 3. Ждем еще полсекунды для верности
            yield return new WaitForSeconds(0.5f);

            GenerateBorders();
        }

        private void GenerateBorders()
        {
            foreach (var l in _spawnedLines) Destroy(l);
            _spawnedLines.Clear();

            // Теперь это число гарантированно правильное (3, 4 и т.д.)
            int kingdomsCount = WorldGenerator.Instance.CurrentKingdomsCount;

            float mapWidth = WorldGenerator.Instance.width;
            float mapHeight = WorldGenerator.Instance.height;
            float drawWidth = mapWidth - 2f;
            float drawHeight = mapHeight - 2f;

            Debug.Log($"[BorderSystem] Генерирую границы. Игроков: {kingdomsCount}");

            DrawBoxBorder(drawWidth, drawHeight);

            if (kingdomsCount < 2)
            {
                SetVisibility(true);
                return;
            }

            float sliceSize = 360f / kingdomsCount;

            for (int i = 0; i < kingdomsCount; i++)
            {
                // Углы границ
                float angleDeg = (i * sliceSize) + (sliceSize / 2f);
                Vector3 edgePoint = GetPointOnRectEdge(angleDeg, drawWidth, drawHeight);
                CreateLine(Vector3.zero, edgePoint);
            }

            SetVisibility(true);
        }

        private Vector3 GetPointOnRectEdge(float angleDeg, float w, float h)
        {
            float angleRad = angleDeg * Mathf.Deg2Rad;
            float cos = Mathf.Cos(angleRad);
            float sin = Mathf.Sin(angleRad);

            float absCos = Mathf.Abs(cos);
            float absSin = Mathf.Abs(sin);

            float xEdge = w / 2f;
            float yEdge = h / 2f;

            float distX = (absCos > 0.001f) ? xEdge / absCos : float.MaxValue;
            float distY = (absSin > 0.001f) ? yEdge / absSin : float.MaxValue;

            float dist = Mathf.Min(distX, distY);

            return new Vector3(cos * dist, sin * dist, 0);
        }

        private void DrawBoxBorder(float w, float h)
        {
            if (linePrefab == null) return;

            GameObject lineObj = Instantiate(linePrefab, Vector3.zero, Quaternion.identity, transform);
            LineRenderer lr = lineObj.GetComponent<LineRenderer>();

            if (lr != null)
            {
                lr.useWorldSpace = true;
                lr.loop = true;
                lr.positionCount = 4;

                float x = w / 2f;
                float y = h / 2f;
                float z = heightOffset;

                lr.SetPosition(0, new Vector3(-x, y, z));
                lr.SetPosition(1, new Vector3(x, y, z));
                lr.SetPosition(2, new Vector3(x, -y, z));
                lr.SetPosition(3, new Vector3(-x, -y, z));
            }
            _spawnedLines.Add(lineObj);
        }

        private void CreateLine(Vector3 start, Vector3 end)
        {
            if (linePrefab == null) return;

            GameObject lineObj = Instantiate(linePrefab, Vector3.zero, Quaternion.identity, transform);
            LineRenderer lr = lineObj.GetComponent<LineRenderer>();

            if (lr != null)
            {
                lr.useWorldSpace = true;
                lr.loop = false;
                lr.positionCount = 2;
                lr.SetPosition(0, start + Vector3.up * heightOffset);
                lr.SetPosition(1, end + Vector3.up * heightOffset);
            }
            _spawnedLines.Add(lineObj);
        }

        public void ToggleVisibility()
        {
            SetVisibility(!_isVisible);
        }

        public void SetVisibility(bool state)
        {
            _isVisible = state;
            foreach (var line in _spawnedLines)
            {
                if (line) line.SetActive(_isVisible);
            }
        }
    }
}