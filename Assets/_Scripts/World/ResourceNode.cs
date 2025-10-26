using UnityEngine;
using WarOfCrowns.Core;

namespace WarOfCrowns.World
{
    public class ResourceNode : MonoBehaviour
    {
        public enum DepletionBehaviour { Destroy, Respawn }

        [Header("Resource Settings")]
        public ResourceType resourceType;
        [Tooltip("The maximum amount this node can hold.")]
        public int maxAmount = 500;

        [HideInInspector]
        public int currentAmount;

        [Header("Depletion Settings")]
        [Tooltip("What happens when the resource runs out?")]
        public DepletionBehaviour depletionBehaviour;

        [Tooltip("The name of this prefab AS IT IS IN THE RESOURCES FOLDER.")]
        [SerializeField] private string prefabNameInResources;

        [Tooltip("Prefab to spawn when this node is depleted. Only used if Behaviour is Respawn.")]
        [SerializeField] private GameObject depletedPrefab;

        [Tooltip("Time in seconds for the resource to respawn. Only used if Behaviour is Respawn.")]
        [SerializeField] private float respawnTime = 60f;

        private void Awake()
        {
            currentAmount = maxAmount;
        }

        public int Gather(int requestedAmount)
        {
            int amountToGive = Mathf.Min(requestedAmount, currentAmount);
            currentAmount -= amountToGive;

            if (currentAmount <= 0)
            {
                Deplete();
            }

            return amountToGive;
        }

        private void Deplete()
        {
            switch (depletionBehaviour)
            {
                // --- ÂÎÒ ÈÑÏÐÀÂËÅÍÈÅ ---
                case DepletionBehaviour.Destroy:
                    Destroy(gameObject); // Ïðîñòî óíè÷òîæàåì îáúåêò
                    break;
                // --- ÊÎÍÅÖ ÈÑÏÐÀÂËÅÍÈß ---

                case DepletionBehaviour.Respawn:
                    if (depletedPrefab != null && !string.IsNullOrEmpty(prefabNameInResources))
                    {
                        GameObject depletedObject = Instantiate(depletedPrefab, transform.position, transform.rotation);
                        depletedObject.AddComponent<RespawnController>().StartRespawning(prefabNameInResources, respawnTime);
                    }
                    Destroy(gameObject);
                    break;
            }
        }
    }
}