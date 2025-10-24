using UnityEngine;

namespace WarOfCrowns.Core
{
    /// <summary>
    /// Manages camera movement and zoom in an RTS style.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 50f;
        [SerializeField] private float edgePanThreshold = 20f; // Screen edge thickness for panning

        [Header("Zoom Settings")]
        [SerializeField] private float zoomSpeed = 20f;
        [SerializeField] private float minZoomSize = 5f;
        [SerializeField] private float maxZoomSize = 50f;

        private Camera _mainCamera;

        // Caching for performance to avoid calling GetComponent every frame
        private void Awake()
        {
            _mainCamera = GetComponent<Camera>();
        }

        private void Update()
        {
            HandleMovement();
            HandleZoom();
        }

        /// <summary>
        /// Handles camera panning via keyboard input (WASD/Arrows) and screen edge.
        /// </summary>
        private void HandleMovement()
        {
            // Vector3 is used because transform.position is a Vector3
            Vector3 inputDirection = Vector3.zero;

            // Keyboard input uses the legacy Input Manager axes by default
            inputDirection.x = Input.GetAxis("Horizontal"); // A/D or Left/Right arrows
            inputDirection.y = Input.GetAxis("Vertical");   // W/S or Up/Down arrows

            // Mouse edge panning
            if (Input.mousePosition.x < edgePanThreshold)
            {
                inputDirection.x -= 1;
            }
            else if (Input.mousePosition.x > Screen.width - edgePanThreshold)
            {
                inputDirection.x += 1;
            }

            if (Input.mousePosition.y < edgePanThreshold)
            {
                inputDirection.y -= 1;
            }
            else if (Input.mousePosition.y > Screen.height - edgePanThreshold)
            {
                inputDirection.y += 1;
            }

            // Apply movement
            // We use transform.up and transform.right to ensure movement is relative to the camera's orientation (though it's fixed in 2D)
            // Time.deltaTime makes the movement frame-rate independent
            Vector3 moveDirection = transform.up * inputDirection.y + transform.right * inputDirection.x;
            transform.position += moveDirection.normalized * (moveSpeed * Time.deltaTime);
        }

        /// <summary>
        /// Handles camera zooming via mouse scroll wheel.
        /// </summary>
        private void HandleZoom()
        {
            // Input.mouseScrollDelta.y gives a positive value for scrolling up/forward and negative for down/backward
            float scrollInput = Input.mouseScrollDelta.y;

            // We check for a small value to avoid floating point inaccuracies
            if (Mathf.Abs(scrollInput) > 0.01f)
            {
                // For an orthographic camera, zooming is changing the 'orthographicSize'
                float newSize = _mainCamera.orthographicSize - scrollInput * zoomSpeed;

                // Mathf.Clamp prevents the zoom from going beyond our defined min/max values
                _mainCamera.orthographicSize = Mathf.Clamp(newSize, minZoomSize, maxZoomSize);
            }
        }
    }
}