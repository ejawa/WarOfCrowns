using UnityEngine;
using WarOfCrowns.Core; // <--- ÈÑÏÐÀÂËÅÍÎ: Äîáàâëåíà ýòà ñòðîêà

namespace WarOfCrowns.Buildings
{
    public class ToolStorage : MonoBehaviour
    {
        private void Start()
        {
            ToolStorageManager.Instance?.RegisterStorage(this);
        }

        private void OnDestroy()
        {
            ToolStorageManager.Instance?.UnregisterStorage(this);
        }
    }
}