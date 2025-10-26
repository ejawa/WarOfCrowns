using System.Collections;
using UnityEngine;
using WarOfCrowns.Core;

namespace WarOfCrowns.Buildings
{
    public class TownHall : MonoBehaviour
    {
        [SerializeField] private GameObject peasantPrefab;
        [SerializeField] private float productionTime = 5f;
        [SerializeField] private Transform spawnPoint;

        public void TryProducePeasant()
        {
            if (PopulationManager.Instance.IsCapReached())
            {
                Debug.Log("Population cap reached! Cannot produce more units.");
                return;
            }
            if (ResourceManager.Instance.GetResourceAmount(ResourceType.Food) >= 50)
            {
                ResourceManager.Instance.AddResource(ResourceType.Food, -50);
                StartCoroutine(ProductionRoutine());
            }
            else
            {
                Debug.Log("Not enough food!");
            }
        }

        private IEnumerator ProductionRoutine()
        {
            Debug.Log("Producing peasant...");
            yield return new WaitForSeconds(productionTime);
            if (spawnPoint != null)
            {
                Instantiate(peasantPrefab, spawnPoint.position, Quaternion.identity);
                Debug.Log("Peasant produced!");
            }
        }
    }
}