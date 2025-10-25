using UnityEngine;
using UnityEngine.InputSystem; // <-- ВАЖНО: Мы подключаем новую систему ввода

namespace WarOfCrowns.Core
{
    /// <summary>
    /// Manages camera movement and zoom using the new Input System.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 50f;
        [SerializeField] private float edgePanThreshold = 20f;

        [Header("Zoom Settings")]
        [SerializeField] private float minZoomSize = 5f;
        [SerializeField] private float maxZoomSize = 50f;
        [SerializeField] private float zoomStep = 2f; // How much each scroll tick zooms

        private Camera _mainCamera;

        private void Awake()
        {
            _mainCamera = GetComponent<Camera>();
        }

        private void Update()
        {
            HandleMovement();
            HandleZoom();
        }

        private void HandleMovement()
        {
            Vector2 inputDirection = Vector2.zero;

            // Keyboard input using the new Input System
            var keyboard = Keyboard.current;
            if (keyboard == null) return; // Guard clause in case there's no keyboard

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            {
                inputDirection.y += 1;
            }
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            {
                inputDirection.y -= 1;
            }
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            {
                inputDirection.x -= 1;
            }
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                inputDirection.x += 1;
            }

            // Mouse edge panning using the new Input System
            var mouse = Mouse.current;
            if (mouse != null)
            {
                Vector2 mousePosition = mouse.position.ReadValue();
                if (mousePosition.x < edgePanThreshold)
                {
                    inputDirection.x -= 1;
                }
                else if (mousePosition.x > Screen.width - edgePanThreshold)
                {
                    inputDirection.x += 1;
                }

                if (mousePosition.y < edgePanThreshold)
                {
                    inputDirection.y -= 1;
                }
                else if (mousePosition.y > Screen.height - edgePanThreshold)
                {
                    inputDirection.y += 1;
                }
            }

            // Apply movement
            Vector3 moveDirection = transform.up * inputDirection.y + transform.right * inputDirection.x;
            transform.position += moveDirection.normalized * (moveSpeed * Time.deltaTime);
        }

        private void HandleZoom()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            float scrollValue = mouse.scroll.ReadValue().y;

            if (Mathf.Abs(scrollValue) > 0.1f)
            {
                // Normalize scroll value to be either +1 or -1
                float direction = Mathf.Sign(scrollValue);

                float newSize = _mainCamera.orthographicSize - direction * zoomStep;
                _mainCamera.orthographicSize = Mathf.Clamp(newSize, minZoomSize, maxZoomSize);
            }
        }
    }
}