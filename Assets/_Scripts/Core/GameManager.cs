using UnityEngine;
using UnityEngine.InputSystem;
using WarOfCrowns.Buildings;
using WarOfCrowns.Core;

namespace WarOfCrowns.Core
{
    public enum GameState { Setup, Playing }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState CurrentState { get; private set; }

        [Header("Prefabs")]
        [SerializeField] private GameObject townHallGhostPrefab;
        [SerializeField] private GameObject townHallPrefab;
        [SerializeField] private GameObject peasantPrefab;

        [Header("Game Settings")]
        [SerializeField] private float setupTime = 60f;
        [SerializeField] private int startingPeasants = 10;

        [Header("UI References")]
     

        private GameObject _currentGhost;
        private float _timer;
        private Camera _mainCamera;

        private void Awake()
        {
            if (Instance != null && Instance != this) Destroy(gameObject);
            else Instance = this;

            _mainCamera = Camera.main;
        }

        private void Start()
        {
            StartSetupPhase();
        }

        private void Update()
        {
            if (CurrentState == GameState.Setup)
            {
                UpdateSetupPhase();
            }
        }

        private void StartSetupPhase()
        {
            CurrentState = GameState.Setup;
            _timer = setupTime;
            _currentGhost = Instantiate(townHallGhostPrefab, Vector3.zero, Quaternion.identity);
        }

        private void UpdateSetupPhase()
        {
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            Vector3 mouseWorldPos = _mainCamera.ScreenToWorldPoint(mouseScreenPos);
            mouseWorldPos.z = 0;
            _currentGhost.transform.position = mouseWorldPos;

            _timer -= Time.deltaTime;
            if (_timer <= 0) PlaceTownHall();
            if (Mouse.current.leftButton.wasPressedThisFrame) PlaceTownHall();
        }

        private void PlaceTownHall()
        {
            if (CurrentState != GameState.Setup) return;

            Vector3 placementPosition = _currentGhost.transform.position;

            // Просто создаем Мэрию. И всё.
            GameObject townHallInstance = Instantiate(townHallPrefab, placementPosition, Quaternion.identity);

            Destroy(_currentGhost);
            StartGamePhase(placementPosition);
        }

        private void StartGamePhase(Vector3 townHallPosition)
        {
            CurrentState = GameState.Playing;
            Debug.Log("Game has started!");

            if (PopulationManager.Instance != null)
            {
                PopulationManager.Instance.SetInitialPopulation(0, 10);
            }

            for (int i = 0; i < startingPeasants; i++)
            {
                float angle = i * (360f / startingPeasants);
                Vector3 spawnOffset = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0) * 5f;
                Instantiate(peasantPrefab, townHallPosition + spawnOffset, Quaternion.identity);
            }
        }
    }
}