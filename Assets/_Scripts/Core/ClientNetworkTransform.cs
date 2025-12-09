using Unity.Netcode.Components;
using UnityEngine;

namespace Unity.Netcode.Samples
{
    [DisallowMultipleComponent]
    public class ClientNetworkTransform : NetworkTransform
    {
        protected override bool OnIsServerAuthoritative()
        {
            return false; // <-- ЭТО ГЛАВНОЕ: Разрешаем клиенту двигать объект
        }
    }
}