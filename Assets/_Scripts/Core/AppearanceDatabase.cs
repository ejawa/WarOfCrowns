using System.Collections.Generic;
using UnityEngine;

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
        [Header("Ãåíåòèêà (Íàáîğû)")]
        public List<SpriteSet> bodies;
        public List<SpriteSet> maleHeads;
        public List<SpriteSet> femaleHeads;

        [Header("Îäåæäà (Ñïèñêè Âàğèàíòîâ)")]
        // --- ÈÇÌÅÍÅÍÈÅ: ÒÅÏÅĞÜ İÒÎ ÑÏÈÑÊÈ ---
        public List<SpriteSet> peasantClothes;
        public List<SpriteSet> soldierClothes;
        // ------------------------------------

        [Header("İêèïèğîâêà")]
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

        public SpriteSet GetRandomBody()
        {
            if (bodies == null || bodies.Count == 0) return null;
            return bodies[Random.Range(0, bodies.Count)];
        }

        public SpriteSet GetRandomHead(Gender gender)
        {
            List<SpriteSet> list = (gender == Gender.Male) ? maleHeads : femaleHeads;
            if (list == null || list.Count == 0) return null;
            return list[Random.Range(0, list.Count)];
        }

        // --- ÍÎÂÛÅ ÌÅÒÎÄÛ ÄËß ÎÄÅÆÄÛ ---
        public SpriteSet GetRandomPeasantClothes()
        {
            if (peasantClothes == null || peasantClothes.Count == 0) return null;
            return peasantClothes[Random.Range(0, peasantClothes.Count)];
        }

        public SpriteSet GetRandomSoldierClothes()
        {
            if (soldierClothes == null || soldierClothes.Count == 0) return null;
            return soldierClothes[Random.Range(0, soldierClothes.Count)];
        }
        // -------------------------------
    }
}