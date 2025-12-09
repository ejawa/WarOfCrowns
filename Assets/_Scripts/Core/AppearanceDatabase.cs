using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace WarOfCrowns.Core
{
    [System.Serializable]
    public class SpriteSet
    {
        public Sprite idle;
        public Sprite[] walk;

        [Header("Плавание")]
        public Sprite[] swim;

        [Header("Смерть в воде (Утопление)")]
        public Sprite[] drown; // <-- НОВОЕ: Кадры утопления
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

        [Header("Аксессуары")]
        public List<SpriteSet> soldierPlumes;

        [Header("Одежда (Белые спрайты!)")]
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

        public SpriteSet GetEquipmentSprites(ResourceType type)
        {
            if (_equipmentMap == null) Initialize();
            if (_equipmentMap.ContainsKey(type)) return _equipmentMap[type];
            return null;
        }

        public SpriteSet GetRandomBody() => GetRandomFrom(bodies);
        public SpriteSet GetRandomPeasantClothes() => GetRandomFrom(peasantClothes);
        public SpriteSet GetRandomSoldierClothes() => GetRandomFrom(soldierClothes);
        public SpriteSet GetRandomPlume() => GetRandomFrom(soldierPlumes);

        public SpriteSet GetRandomHead(Gender gender)
        {
            return gender == Gender.Male ? GetRandomFrom(maleHeads) : GetRandomFrom(femaleHeads);
        }
        public SpriteSet GetBodyByIndex(int index) => GetSafe(bodies, index);

        public SpriteSet GetHeadByIndex(int index, Gender gender)
        {
            return gender == Gender.Male ? GetSafe(maleHeads, index) : GetSafe(femaleHeads, index);
        }

        public SpriteSet GetClothesByIndex(int index, ProfessionType prof)
        {
            var list = (prof == ProfessionType.Soldier) ? soldierClothes : peasantClothes;
            // Для одежды используем остаток от деления (на случай смены списка)
            if (list == null || list.Count == 0) return null;
            return list[Mathf.Abs(index) % list.Count];
        }

        public SpriteSet GetPlumeByIndex(int index)
        {
            if (soldierPlumes == null || soldierPlumes.Count == 0) return null;
            return soldierPlumes[Mathf.Abs(index) % soldierPlumes.Count];
        }

        private SpriteSet GetSafe(List<SpriteSet> list, int index)
        {
            if (list == null || list.Count == 0) return null;
            if (index < 0 || index >= list.Count) return list[0]; // Fallback
            return list[index];
        }
        // --------
        private SpriteSet GetRandomFrom(List<SpriteSet> list)
        {
            if (list == null || list.Count == 0) return null;
            return list[Random.Range(0, list.Count)];
        }

        public SpriteSet GetSpriteSetByName(string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName)) return null;
            var found = FindInList(bodies, spriteName);
            if (found != null) return found;
            found = FindInList(maleHeads, spriteName);
            if (found != null) return found;
            found = FindInList(femaleHeads, spriteName);
            if (found != null) return found;
            found = FindInList(peasantClothes, spriteName);
            if (found != null) return found;
            found = FindInList(soldierClothes, spriteName);
            if (found != null) return found;
            found = FindInList(soldierPlumes, spriteName);
            return found;
        }

        private SpriteSet FindInList(List<SpriteSet> list, string name)
        {
            return list?.FirstOrDefault(s => s.idle != null && s.idle.name == name);
        }
    }
}