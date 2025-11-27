using UnityEngine;
using WarOfCrowns.Buildings;

namespace WarOfCrowns.UI
{
    public class TownHallUI : MonoBehaviour
    {
        private TownHall _linkedTownHall;

        // Этот метод вызывается из SelectableBuilding
        public void Initialize(TownHall townHall)
        {
            _linkedTownHall = townHall;
        }

        // Этот метод привязан к кнопке "Создать юнита" в Инспекторе
        public void OnCreatePeasantClick()
        {
            if (_linkedTownHall != null)
            {
                // Отправляем команду в КОНКРЕТНУЮ Мэрию
                _linkedTownHall.TryProducePeasant();
            }
            else
            {
                // Вот здесь и была твоя ошибка
                Debug.LogError("ОШИБКА: UI Мэрии открыт, но не привязан к зданию!");
            }
        }
    }
}