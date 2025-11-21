using UnityEngine;
using WarOfCrowns.Buildings;
namespace WarOfCrowns.UI
{
    public class JobUIProxy : MonoBehaviour, ISelectionUI
    { // Реализуем интерфейс, если он есть
        public void LinkToKingdom(Core.Kingdom k) { } // Заглушка
        public void Initialize(JobBuilding building) { GetComponent<JobUI>().Initialize(building); }
    }
}