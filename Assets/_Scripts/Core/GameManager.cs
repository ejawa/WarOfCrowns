using UnityEngine;
using UnityEngine.InputSystem; // <-- ÏÎÄÊËÞ×ÀÅÌ ÍÎÂÓÞ ÑÈÑÒÅÌÓ
using WarOfCrowns.Buildings;

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

        private GameObject _currentGhost;
        private float _timer;
        private Camera _mainCamera; // <-- Êýøèðóåì êàìåðó

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                _mainCamera = Camera.main; // <-- Íàõîäèì êàìåðó îäèí ðàç
            }
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
            Debug.Log("Setup phase started. Choose a location for your Town Hall.");
        }

        private void UpdateSetupPhase()
        {
            // ÏÐÀÂÈËÜÍÛÉ ÑÏÎÑÎÁ ÏÎËÓ×ÅÍÈß ÏÎÇÈÖÈÈ ÌÛØÈ
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            Vector3 mouseWorldPos = _mainCamera.ScreenToWorldPoint(mouseScreenPos);
            mouseWorldPos.z = 0;
            _currentGhost.transform.position = mouseWorldPos;

            // Countdown timer
            _timer -= Time.deltaTime;
            if (_timer <= 0)
            {
                PlaceTownHall();
            }

            // ÏÐÀÂÈËÜÍÛÉ ÑÏÎÑÎÁ ÏÐÎÂÅÐÊÈ ÊËÈÊÀ
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                PlaceTownHall();
            }
        }

        private void PlaceTownHall()
        {
            if (CurrentState != GameState.Setup) return;

            Vector3 placementPosition = _currentGhost.transform.position;
            Instantiate(townHallPrefab, placementPosition, Quaternion.identity);

            Destroy(_currentGhost);

            StartGamePhase(placementPosition);
        }

        private void StartGamePhase(Vector3 townHallPosition)
        {
            CurrentState = GameState.Playing;
            Debug.Log("Game has started!");

            for (int i = 0; i < startingPeasants; i++)
            {
                float angle = i * (360f / startingPeasants);
                Vector3 spawnOffset = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0) * 5f;
                Instantiate(peasantPrefab, townHallPosition + spawnOffset, Quaternion.identity);
            }
        }
    }
}