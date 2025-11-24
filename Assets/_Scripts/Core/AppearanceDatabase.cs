using System.Collections.Generic;
using UnityEngine;
using System.Linq; // Для поиска

namespace WarOfCrowns.Core
{
    [System.Serializable]
    public class SpriteSet
    {
        public Sprite idle;
        public Sprite[] walk;
    }

    [System.Serializable]
    public class ItemVisual
    {
        public ResourceType itemType;
        public SpriteSet sprites;
    }

    [CreateAssetMenu(fileName = "AppearanceDatabase", menuName = "WarOfCrowns/Appearance Database")]
    public class AppearanceDatabase : ScriptableObject
    {
        [Header("Генетика")]
        public List<SpriteSet> bodies;
        public List<SpriteSet> maleHeads;
        public List<SpriteSet> femaleHeads;

        [Header("Одежда")]
        public List<SpriteSet> peasantClothes;
        public List<SpriteSet> soldierClothes;

        [Header("Экипировка")]
        [SerializeField] private List<ItemVisual> equipmentVisuals;

        private Dictionary<ResourceType, SpriteSet> _equipmentMap;

        public void Initialize()
        {
            _equipmentMap = new Dictionary<ResourceType, SpriteSet>();
            foreach (var item in equipmentVisuals)
            {
                if (!_equipmentMap.ContainsKey(item.itemType))
                    _equipmentMap.Add(item.itemType, item.sprites);
            }
        }

        // Получение визуалов предметов
        public SpriteSet GetEquipmentSprites(ResourceType type)
        {
            if (_equipmentMap == null) Initialize();
            if (_equipmentMap.ContainsKey(type)) return _equipmentMap[type];
            return null;
        }

        // Рандомизаторы
        public SpriteSet GetRandomBody() => GetRandomFrom(bodies);
        public SpriteSet GetRandomPeasantClothes() => GetRandomFrom(peasantClothes);
        public SpriteSet GetRandomSoldierClothes() => GetRandomFrom(soldierClothes);

        public SpriteSet GetRandomHead(Gender gender)
        {
            return gender == Gender.Male ? GetRandomFrom(maleHeads) : GetRandomFrom(femaleHeads);
        }

        private SpriteSet GetRandomFrom(List<SpriteSet> list)
        {
            if (list == null || list.Count == 0) return null;
            return list[Random.Range(0, list.Count)];
        }

        // --- НОВЫЙ МЕТОД: Поиск по имени (для Загрузки) ---
        public SpriteSet GetSpriteSetByName(string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName)) return null;

            // Ищем везде. Медленно, но надежно для загрузки.
            var found = FindInList(bodies, spriteName);
            if (found != null) return found;

            found = FindInList(maleHeads, spriteName);
            if (found != null) return found;
            found = FindInList(femaleHeads, spriteName);
            if (found != null) return found;

            found = FindInList(peasantClothes, spriteName);
            if (found != null) return found;
            found = FindInList(soldierClothes, spriteName);

            return found;
        }

        private SpriteSet FindInList(List<SpriteSet> list, string name)
        {
            // Ищем SpriteSet, у которого имя idle спрайта совпадает
            return list?.FirstOrDefault(s => s.idle != null && s.idle.name == name);
        }
    }
}