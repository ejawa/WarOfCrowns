using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WarOfCrowns.Buildings;
using WarOfCrowns.Units;
using WarOfCrowns.Core;
using Unity.Netcode;
using System.Collections.Generic;

namespace WarOfCrowns.UI
{
    public class BuildingDetailUI : MonoBehaviour
    {
        [Header("Инфо")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI kingdomText;
        [SerializeField] private Slider hpSlider;
        [SerializeField] private TextMeshProUGUI hpText;

        [Header("Жители / Рабочие")]
        [SerializeField] private Transform residentsContainer;
        [SerializeField] private GameObject residentSlotPrefab; // Тот же WorkerSlotUI
        [SerializeField] private TextMeshProUGUI capacityText;

        [Header("Действия")]
        [SerializeField] private Button callResidentsButton; // "Позвать домой"
        [SerializeField] private Button ejectAllButton;// "Выгнать всех на улицу"
        [SerializeField] private Button closeButton;

        private Building _currentBuilding;
        private Residence _currentResidence;

        public void Open(Building building)
        {
            _currentBuilding = building;
            _currentResidence = building.GetComponent<Residence>();

            // ВАЖНО: Закрываем нижние вкладки и другие окна
            if (MainUIController.Instance)
                MainUIController.Instance.CloseEverythingForBuildingView();

            gameObject.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            _currentBuilding = null;
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (gameObject.activeSelf && _currentBuilding != null)
            {
                var hp = _currentBuilding.GetComponent<Health>();
                if (hp)
                {
                    hpSlider.value = hp.CurrentHealth;
                    hpText.text = $"{hp.CurrentHealth}/{hp.MaxHealth}";
                }
            }
        }

        public void Refresh()
        {
            if (_currentBuilding == null) return;

            nameText.text = _currentBuilding.buildingName;

            if (_currentBuilding.OwningKingdom != null)
                kingdomText.text = _currentBuilding.OwningKingdom.kingdomName.Value.ToString();

            // Если это Дом
            if (_currentResidence != null)
            {
                capacityText.text = $"Жильцов: {_currentResidence.GetResidents().Count} / {_currentResidence.capacity}";
                callResidentsButton.gameObject.SetActive(true);
                ejectAllButton.gameObject.SetActive(true);

                RefreshResidentsList();

                callResidentsButton.onClick.RemoveAllListeners();
                callResidentsButton.onClick.AddListener(() => {
                    // Отправляем RPC на сервер: "Загони всех домой"
                    _currentResidence.CallResidentsServerRpc();
                });

                ejectAllButton.onClick.RemoveAllListeners();
                ejectAllButton.onClick.AddListener(() => {
                    _currentResidence.EjectAllResidentsServerRpc();
                });
            }
            else
            {
                // Если не дом - прячем кнопки жилья
                capacityText.text = "";
                callResidentsButton.gameObject.SetActive(false);
                ejectAllButton.gameObject.SetActive(false);
                // Тут можно показать рабочих (JobBuilding) по аналогии
            }
        }

        private void RefreshResidentsList()
        {
            foreach (Transform child in residentsContainer) Destroy(child.gameObject);

            foreach (ulong unitID in _currentResidence.GetResidents())
            {
                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(unitID, out var netObj))
                {
                    var unit = netObj.GetComponent<Unit>();
                    if (unit != null)
                    {
                        var slot = Instantiate(residentSlotPrefab, residentsContainer);
                        // Настраиваем слот (используем WorkerSlotUI)
                        slot.GetComponent<WorkerSlotUI>().Setup(unit, "Выгнать", (u) => {
                            _currentResidence.KickResidentServerRpc(u.GetComponent<NetworkObject>().NetworkObjectId);
                            Invoke(nameof(Refresh), 0.2f);
                        });
                    }
                }
            }
        }
    }
}