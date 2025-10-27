using UnityEngine;
using WarOfCrowns.Core;
using WarOfCrowns.Core.Items;

namespace WarOfCrowns.World
{
    public class ResourceNode : MonoBehaviour
    {
        public enum NodeType { Resource, Item }
        public enum DepletionBehaviour { Destroy, Respawn }

        [Header("Node Type")]
        public NodeType nodeType;

        [Header("Resource Settings (if NodeType is Resource)")]
        public ResourceType resourceType;

        [Header("Item Settings (if NodeType is Item)")]
        public ItemType itemType;

        [Header("General Settings")]
        public int maxAmount = 250;
        [HideInInspector] public int currentAmount;

        [Header("Depletion Settings")]
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