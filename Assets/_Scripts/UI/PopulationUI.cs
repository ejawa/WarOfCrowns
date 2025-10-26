using TMPro;
using UnityEngine;
using WarOfCrowns.Core;

namespace WarOfCrowns.UI
{
    public class PopulationUI : MonoBehaviour
    {
        private TextMeshProUGUI _text;
        private void Awake() { _text = GetComponent<TextMeshProUGUI>(); }
        private void OnEnable() { PopulationManager.OnPopulationChanged += UpdateUI; }
        private void OnDisable() { PopulationManager.OnPopulationChanged -= UpdateUI; }
        private void Start() { UpdateUI(); } // Initial update

        void UpdateUI()
        {
            if (PopulationManager.Instance != null)
            {
                _text.text = $"Pop: {PopulationManager.Instance.CurrentPopulation} / {PopulationManager.Instance.PopulationCap}";
            }
        }
    }
}