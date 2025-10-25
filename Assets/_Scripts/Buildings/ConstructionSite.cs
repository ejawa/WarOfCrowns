using UnityEngine;

namespace WarOfCrowns.Buildings
{
    public class ConstructionSite : MonoBehaviour
    {
        [SerializeField] private float _buildTime = 10f;
        [SerializeField] private GameObject _finishedBuildingPrefab;
        private float _currentBuildProgress;

        public bool AddBuildProgress(float amount)
        {
            _currentBuildProgress += amount;
            Debug.Log($"Build progress: {_currentBuildProgress} / {_buildTime}");
            if (_currentBuildProgress >= _buildTime)
            {
                Instantiate(_finishedBuildingPrefab, transform.position, transform.rotation);
                Destroy(gameObject);
                return true; // Construction is complete
            }
            return false; // Still building
        }
    }
}