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
        [SerializeField] private Button sendToWorkButton;

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

            if (sendToWorkButton != null)
            {
                sendToWorkButton.onClick.RemoveAllListeners();
                sendToWorkButton.onClick.AddListener(SendEveryoneToWork);
            }

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

            // Ищем название здания через компонент Building
            var bData = _currentBuilding.GetComponent<Building>();
            if (bData != null) titleText.text = bData.buildingName;
            else titleText.text = _currentBuilding.name.Replace("(Clone)", "");

            openCandidatesButton.interactable = _currentBuilding.CanAddWorker();

            foreach (Transform child in currentWorkersParent) Destroy(child.gameObject);

            foreach (Unit worker in _currentBuilding.GetWorkers())
            {
                GameObject slot = Instantiate(workerSlotPrefab, currentWorkersParent);
                slot.GetComponent<WorkerSlotUI>().Setup(worker, "-", FireWorker);
            }
        }

        private void OpenCandidateList()
        {
            if (candidatesPanel == null) return;
            candidatesPanel.SetActive(true);

            foreach (Transform child in candidatesParent) Destroy(child.gameObject);

            if (PopulationManager.Instance != null)
            {
                foreach (Unit unit in PopulationManager.Instance.AllUnits)
                {
                    // --- ИСПРАВЛЕНО: Profession с большой буквы ---
                    if (unit.Profession == ProfessionType.Unemployed)
                    {
                        GameObject slot = Instantiate(workerSlotPrefab, candidatesParent);
                        slot.GetComponent<WorkerSlotUI>().Setup(unit, "+", HireWorker);
                    }
                }
            }
        }

        private void CloseCandidateList() { if (candidatesPanel != null) candidatesPanel.SetActive(false); }

        private void HireWorker(Unit unit)
        {
            if (_currentBuilding.CanAddWorker())
            {
                _currentBuilding.AddWorker(unit);
                RefreshUI();
                if (_currentBuilding.CanAddWorker()) OpenCandidateList(); else CloseCandidateList();
            }
        }

        private void FireWorker(Unit unit)
        {
            _currentBuilding.RemoveWorker(unit);
            RefreshUI();
        }

        private void SendEveryoneToWork()
        {
            if (_currentBuilding == null) return;
            foreach (var worker in _currentBuilding.GetWorkers())
            {
                if (worker != null)
                {
                    if (worker.TryGetComponent<UnitAI>(out var ai) && worker.TryGetComponent<UnitWorker>(out var workerScript))
                    {
                        if (workerScript.CurrentJob == _currentBuilding && ai.CurrentState == UnitState.Working)
                        {
                            continue;
                        }

                        ai.SetState(UnitState.Working);
                        workerScript.SetTarget(_currentBuilding);

                        // --- ИСПРАВЛЕНО: UnitName с большой буквы ---
                        Debug.Log($"Sent {worker.UnitName} back to work!");
                    }
                }
            }
        }
    }
}