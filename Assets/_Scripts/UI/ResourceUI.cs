using TMPro;
using UnityEngine;
using WarOfCrowns.Core;

namespace WarOfCrowns.UI
{
    public class ResourceUI : MonoBehaviour
    {
        [SerializeField] private ResourceType resourceTypeToDisplay;
        private TextMeshProUGUI _resourceText;

        private void Awake()
        {
            _resourceText = GetComponent<TextMeshProUGUI>();
        }

        private void Start()
        {
            // Subscribe to the event
            ResourceManager.OnResourceChanged += UpdateText;
            // Update text on start to show initial values
            UpdateText(resourceTypeToDisplay, ResourceManager.Instance.GetResourceAmount(resourceTypeToDisplay));
        }

        private void OnDestroy()
        {
            // Unsubscribe to prevent memory leaks
            ResourceManager.OnResourceChanged -= UpdateText;
        }

        private void UpdateText(ResourceType type, int amount)
        {
            if (type == resourceTypeToDisplay)
            {
                _resourceText.text = $"{type}: {amount}";
            }
        }
    }
}