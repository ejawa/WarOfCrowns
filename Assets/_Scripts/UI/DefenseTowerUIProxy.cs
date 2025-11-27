using UnityEngine;
using WarOfCrowns.Buildings;

namespace WarOfCrowns.UI
{
    [RequireComponent(typeof(DefenseTowerUI))]
    public class DefenseTowerUIProxy : MonoBehaviour
    {
        public void Initialize(DefenseTower tower)
        {
            GetComponent<DefenseTowerUI>().Initialize(tower);
        }
    }
}