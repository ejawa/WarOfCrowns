using UnityEngine;
using WarOfCrowns.Buildings;

namespace WarOfCrowns.UI
{
    [RequireComponent(typeof(BarracksUI))]
    public class BarracksUIProxy : MonoBehaviour
    {
        public void Initialize(Barracks barracks)
        {
            // Передаем ссылку на казарму в основной UI скрипт
            GetComponent<BarracksUI>().Initialize(barracks);
        }
    }
}