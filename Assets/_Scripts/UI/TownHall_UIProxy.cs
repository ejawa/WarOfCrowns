using UnityEngine;
using WarOfCrowns.Buildings;
public class TownHall_UIProxy : MonoBehaviour
{
    private TownHall _linkedTownHall;
    public void LinkToTownHall(TownHall townHall) { _linkedTownHall = townHall; }
    public void OnCreatePeasantClick()
    {
        if (_linkedTownHall != null) _linkedTownHall.TryProducePeasant();
    }
}