using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WarOfCrowns.Buildings;
using WarOfCrowns.Units;
using WarOfCrowns.Core;
using System.Collections.Generic;

namespace WarOfCrowns.UI
{
    public class JobUI : MonoBehaviour
    {
        [Header("Основное")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI workersCountText;
        [SerializeField] private Button closeButton;

        // --- НОВАЯ КНОПКА ---
        [SerializeField] private Button sendToWorkButton;
        // --------------------

        [Header("Списки")]
        [SerializeField] private GameObject workerSlotPrefab;
        [SerializeField] private Transform currentWorkersParent;

        [SerializeField] private GameObject candidatesPanel;
        [SerializeField] private Transform candidatesParent;
        [SerializeField] private Button openCandidatesButton;
        [SerializeField] private Button closeCandidatesButton;

        private JobBuilding _currentBuilding;

        public void Initialize(JobBuilding building)
        {
            _currentBuilding = building;

            if (closeButton != null)
                closeButton.onClick.AddListener(() => gameObject.SetActive(false));

            if (openCandidatesButton != null)
                openCandidatesButton.onClick.AddListener(OpenCandidateList);

            if (closeCandidatesButton != null)
                closeCandidatesButton.onClick.AddListener(CloseCandidateList);

            // --- ПОДКЛЮЧАЕМ НОВУЮ КНОПКУ ---
            if (sendToWorkButton != null)
            {
                sendToWorkButton.onClick.RemoveAllListeners();
                sendToWorkButton.onClick.AddListener(SendEveryoneToWork);
            }
            // -------------------------------

            if (candidatesPanel != null) candidatesPanel.SetActive(false);

            RefreshUI();
        }

        private void Update()
        {
            if (gameObject.activeSelf && _currentBuilding != null)
            {
                workersCountText.text = $"Workers: {_currentBuilding.GetWorkers().Count} / {_currentBuilding.maxWorkers}";
            }
        }

        public void RefreshUI()
        {
            if (_currentBuilding == null) return;
            titleText.text = _currentBuilding.name.Replace("(Clone)", "");
            openCandidatesButton.interactable = _currentBuilding.CanAddWorker();
            foreach (Transform child in currentWorkersParent) Destroy(child.gameObject);
            foreach (Unit worker in _currentBuilding.GetWorkers())
            {
                GameObject slot = Instantiate(workerSlotPrefab, currentWorkersParent);
                slot.GetComponent<WorkerSlotUI>().Setup(worker, "-", FireWorker);
            }
        }

        private void OpenCandidateList()
        { /* код как был */
            if (candidatesPanel == null) return;
            candidatesPanel.SetActive(true);
            foreach (Transform child in candidatesParent) Destroy(child.gameObject);
            foreach (Unit unit in PopulationManager.Instance.AllUnits)
            {
                if (unit.profession == ProfessionType.Unemployed)
                {
                    GameObject slot = Instantiate(workerSlotPrefab, candidatesParent);
                    slot.GetComponent<WorkerSlotUI>().Setup(unit, "+", HireWorker);
                }
            }
        }
        private void CloseCandidateList() { if (candidatesPanel != null) candidatesPanel.SetActive(false); }

        private void HireWorker(Unit unit)
        {
            if (_currentBuilding.CanAddWorker())
            {
                _currentBuilding.AddWorker(unit);
                if (unit.TryGetComponent<UnitWorker>(out var workerAI)) workerAI.SetTarget(_currentBuilding);
                RefreshUI();
                if (_currentBuilding.CanAddWorker()) OpenCandidateList(); else CloseCandidateList();
            }
        }
        private void FireWorker(Unit unit)
        {
            _currentBuilding.RemoveWorker(unit);
            RefreshUI();
        }

        // --- НОВЫЙ МЕТОД ---
        private void SendEveryoneToWork()
        {
            if (_currentBuilding == null) return;

            foreach (var worker in _currentBuilding.GetWorkers())
            {
                if (worker != null)
                {
                    // 1. Отменяем поиск еды и другие дела
                    if (worker.TryGetComponent<UnitAI>(out var ai))
                    {
                        ai.SetState(UnitState.Working); // Принудительно ставим состояние
                    }

                    // 2. Отправляем работать
                    if (worker.TryGetComponent<UnitWorker>(out var workerScript))
                    {
                        workerScript.SetTarget(_currentBuilding);
                    }

                    Debug.Log($"Sent {worker.unitName} back to work!");
                }
            }
        }
    }
}