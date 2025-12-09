using Unity.Netcode;
using System;

namespace WarOfCrowns.Core
{
    public enum PlayerStatus
    {
        Connecting, // Только зашел
        Lobby,      // Сидит в лобби
        Loading,    // Генерирует карту
        Ready,      // Поставил Мэрию / Готов играть
        Playing     // В игре
    }

    // Структура данных об одном игроке
    public struct PlayerData : INetworkSerializable, IEquatable<PlayerData>
    {
        public ulong ClientId; // Сетевой ID (технический)
        public int KingdomId;  // Игровой ID (0 - Хост, 1 - Клиент)
        public PlayerStatus Status;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref KingdomId);
            serializer.SerializeValue(ref Status);
        }

        public bool Equals(PlayerData other)
        {
            return ClientId == other.ClientId && KingdomId == other.KingdomId;
        }
    }
}