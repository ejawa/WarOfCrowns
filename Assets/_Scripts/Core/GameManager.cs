using UnityEngine;
using UnityEngine.InputSystem;
using WarOfCrowns.Buildings;
using WarOfCrowns.Core;
using WarOfCrowns.Units;
using System.Collections.Generic;
using System.Collections;

namespace WarOfCrowns.Core
{
    public enum GameState { PreGame, Setup, Playing }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public GameState CurrentState { get; private set; }

        [Header("Настройки Имен")]
        [SerializeField] private NameDatabase nameDatabase; // <-- НОВОЕ
                                                            // В GameManager.cs добавь:

        [Header("Внешность")]
        [SerializeField] private AppearanceDatabase appearanceDatabase;
        public AppearanceDatabase AppearanceDB => appearanceDatabase; // Свойство для доступа
        [Header("Prefabs")]
        [SerializeField] private GameObject townHallGhostPrefab;
        [SerializeField] private GameObject townHallPrefab;
        [SerializeField] private GameObject peasantPrefab;

        [Header("Game Settings")]
        [SerializeField] private float setupTime = 60f;
        [SerializeField] private int startingPeasants = 10;

        [Header("System References")]
        [SerializeField] private UnitSelectionController selectionController;

        private GameObject _currentGhost;
        private float _timer;
        private Camera _mainCamera;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _mainCamera = Camera.main;
            CurrentState = GameState.PreGame;
        }

        public void InitializeGame()
        {
            StartSetupPhase();
        }

        // --- НОВЫЙ МЕТОД ---
        public string GetRandomFullName(Gender gender)
        {
            if (nameDatabase != null) return nameDatabase.GetRandomName(gender);
            return "Unnamed";
        }
        // -------------------
        public Sprite GetRandomPortrait(Gender gender)
        {
            if (nameDatabase != null) return nameDatabase.GetRandomPortrait(gender);
            return null;
        }
        private void Update()
        {
            if (CurrentState == GameState.Setup) UpdateSetupPhase();
        }

        private void StartSetupPhase()
        {
            CurrentState = GameState.Setup;
            selectionController.enabled = false;
            _timer = setupTime;
            _currentGhost = Instantiate(townHallGhostPrefab, Vector3.zero, Quaternion.identity);
        }

        private void UpdateSetupPhase()
        {
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            Vector3 mouseWorldPos = _mainCamera.ScreenToWorldPoint(mouseScreenPos);
            mouseWorldPos.z = 0;
            if (_currentGhost != null) _currentGhost.transform.position = mouseWorldPos;

            _timer -= Time.deltaTime;
            if (CurrentState == GameState.Setup && (_timer <= 0 || Mouse.current.leftButton.wasPressedThisFrame))
            {
                PlaceTownHall();
            }
        }

        private void PlaceTownHall()
        {
            CurrentState = GameState.Playing;
            if (_currentGhost == null) return;
            Vector3 placementPosition = _currentGhost.transform.position;
            Destroy(_currentGhost);
            _currentGhost = null;

            GameObject townHallInstance = Instantiate(townHallPrefab, placementPosition, Quaternion.identity);
            if (townHallInstance.TryGetComponent<Building>(out var buildingLogic)) buildingLogic.OwningKingdom = Kingdom.PlayerKingdom;
            if (townHallInstance.TryGetComponent<TownHall>(out var townHallLogic)) townHallLogic.OwningKingdom = Kingdom.PlayerKingdom;

            StartGamePhase(placementPosition);
        }

        private void StartGamePhase(Vector3 townHallPosition)
        {
            selectionController.enabled = true;
            if (PopulationManager.Instance != null) PopulationManager.Instance.SetInitialPopulation(0, 10);

            for (int i = 0; i < startingPeasants; i++)
            {
                float angle = i * (360f / startingPeasants);
                Vector3 spawnOffset = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0) * 5f;
                GameObject peasantInstance = Instantiate(peasantPrefab, townHallPosition + spawnOffset, Quaternion.identity);
                if (peasantInstance.TryGetComponent<Unit>(out var unit))
                {
                    unit.OwningKingdom = Kingdom.PlayerKingdom;
                }
            }
        }
    }
}