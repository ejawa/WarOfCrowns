using UnityEngine;
using UnityEngine.InputSystem;

namespace WarOfCrowns.Core
{
    public class CameraController : MonoBehaviour
    {
        [Header("Клавиатура")]
        [SerializeField] private float keyboardMoveSpeed = 20f;

        [Header("Мышь (Перетаскивание)")]
        [SerializeField] private float dragSpeed = 0.5f; // Чувствительность мыши
        [Tooltip("Инвертировать ли движение мыши (как на тачпадах)")]
        [SerializeField] private bool invertDrag = false;

        [Header("Зум")]
        [SerializeField] private float minZoomSize = 5f;
        [SerializeField] private float maxZoomSize = 30f;
        [SerializeField] private float zoomStep = 2f;

        private Camera _mainCamera;

        private void Awake()
        {
            _mainCamera = GetComponent<Camera>();
        }

        private void LateUpdate()
        {
            HandleKeyboardMovement();
            HandleMouseDrag();
            HandleZoom();
        }

        private void HandleKeyboardMovement()
        {
            if (Keyboard.current == null) return;

            Vector3 inputDirection = Vector3.zero;

            // WASD и Стрелки
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) inputDirection.y += 1;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) inputDirection.y -= 1;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) inputDirection.x -= 1;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) inputDirection.x += 1;

            transform.position += inputDirection.normalized * (keyboardMoveSpeed * Time.deltaTime);
        }

        private void HandleMouseDrag()
        {
            if (Mouse.current == null) return;

            // ИЗМЕНЕНИЕ: Теперь проверяем ТОЛЬКО среднюю кнопку (колесико)
            if (Mouse.current.middleButton.isPressed)
            {
                Vector2 delta = Mouse.current.delta.ReadValue();

                float direction = invertDrag ? 1f : -1f;

                Vector3 move = new Vector3(delta.x * direction, delta.y * direction, 0);

                // Масштабируем скорость от зума
                float zoomFactor = _mainCamera.orthographicSize / 10f;

                transform.position += move * (dragSpeed * zoomFactor * Time.deltaTime);
            }
        }

        private void HandleZoom()
        {
            // --- ИСПРАВЛЕНИЕ: Если курсор на UI, не зумим ---
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;
            // ------------------------------------------------

            if (Mouse.current == null) return;

            float scrollValue = Mouse.current.scroll.ReadValue().y;

            if (Mathf.Abs(scrollValue) > 0.1f)
            {
                float direction = Mathf.Sign(scrollValue);
                float newSize = _mainCamera.orthographicSize - direction * zoomStep;

                _mainCamera.orthographicSize = Mathf.Clamp(newSize, minZoomSize, maxZoomSize);
            }
        }
    }
}