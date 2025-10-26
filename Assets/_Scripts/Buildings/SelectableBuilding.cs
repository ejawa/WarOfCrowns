using UnityEngine;
using UnityEngine.UI;
using WarOfCrowns.Buildings;

namespace WarOfCrowns.Buildings
{
    public class SelectableBuilding : MonoBehaviour
    {
        private GameObject _selectionUI;

        public void SetSelectionUI(GameObject uiPanel)
        {
            _selectionUI = uiPanel;
            if (_selectionUI == null)
            {
                Debug.LogError($"CRITICAL: Building '{gameObject.name}' could not find the UI Panel named 'TownHall_UIPanel' on the scene!");
                return;
            }
            Debug.Log($"SUCCESS: UI Panel was assigned to building '{gameObject.name}'.");

            Button createPeasantBtn = _selectionUI.GetComponentInChildren<Button>();
            if (createPeasantBtn != null)
            {
                TownHall townHall = GetComponent<TownHall>();
                if (townHall != null)
                {
                    createPeasantBtn.onClick.RemoveAllListeners();
                    createPeasantBtn.onClick.AddListener(townHall.TryProducePeasant);
                    Debug.Log("Button 'CreatePeasant' has been successfully linked to TownHall production.");
                }
            }
        }

        public void Select()
        {
            Debug.Log($"Select() method called on '{gameObject.name}'!");
            if (_selectionUI != null)
            {
                Debug.Log("UI Panel is assigned. Activating it now.");
                _selectionUI.SetActive(true);
            }
            else
            {
                Debug.LogError($"Trying to Select '{gameObject.name}', but its _selectionUI is NULL!");
            }
        }

        public void Deselect()
        {
            if (_selectionUI != null)
            {
                _selectionUI.SetActive(false);
            }
        }
    }
}