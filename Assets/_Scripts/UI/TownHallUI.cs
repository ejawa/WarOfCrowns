using UnityEngine;
using WarOfCrowns.Buildings; // Чтобы видеть TownHall

namespace WarOfCrowns.UI
{
    public class TownHallUI : MonoBehaviour
    {
        private TownHall _linkedTownHall;

        // Эту функцию вызовет SelectableBuilding при создании окна
        public void Initialize(TownHall townHall)
        {
            _linkedTownHall = townHall;
        }

        // Эту функцию ты НАСТРОИШЬ В ИНСПЕКТОРЕ кнопки
        public void OnCreatePeasantClick()
        {
            if (_linkedTownHall != null)
            {
                _linkedTownHall.TryProducePeasant();
            }
            else
            {
                Debug.LogError("ОШИБКА: UI Мэрии открыт, но не привязан к зданию!");
            }
        }
    }
}