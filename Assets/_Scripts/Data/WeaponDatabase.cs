using UnityEngine;
using System.Collections.Generic;
using WarOfCrowns.Core; // Чтобы видеть ResourceType

namespace WarOfCrowns.Data
{
    [System.Serializable]
    public struct WeaponData
    {
        public ResourceType weaponType;
        public int damage;           // Урон
        public float attackSpeed;    // Задержка между атаками (сек)
        public float range;          // Дальность атаки
        public bool isRanged;        // Стреляет или бьет?
        public GameObject projectilePrefab; // Префаб стрелы (если isRanged)
    }

    [CreateAssetMenu(fileName = "WeaponDatabase", menuName = "WarOfCrowns/Weapon Database")]
    public class WeaponDatabase : ScriptableObject
    {
        public List<WeaponData> weapons;

        // Метод поиска статов
        public WeaponData GetWeaponStats(ResourceType type)
        {
            if (weapons != null)
            {
                foreach (var w in weapons)
                {
                    if (w.weaponType == type) return w;
                }
            }

            // Дефолтные статы (Кулаки / Базовый инструмент)
            return new WeaponData
            {
                weaponType = type,
                damage = 5,
                attackSpeed = 1.5f,
                range = 0.5f,
                isRanged = false,
                projectilePrefab = null
            };
        }
    }
}