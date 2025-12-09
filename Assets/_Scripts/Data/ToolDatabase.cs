using UnityEngine;
using System.Collections.Generic;
using WarOfCrowns.Core; // <-- ВАЖНО: Чтобы видеть ResourceType

namespace WarOfCrowns.Data
{
    [System.Serializable]
    public struct ToolData
    {
        public ResourceType toolType;
        [Tooltip("Множитель скорости (1.0 = стандарт, 2.0 = в 2 раза быстрее)")]
        public float speedMultiplier;
    }

    [CreateAssetMenu(fileName = "ToolDatabase", menuName = "WarOfCrowns/Tool Database")]
    public class ToolDatabase : ScriptableObject
    {
        public List<ToolData> tools;

        public float GetMultiplier(ResourceType type)
        {
            if (tools == null) return 1.0f;
            foreach (var tool in tools)
            {
                if (tool.toolType == type) return tool.speedMultiplier;
            }
            return 1.0f; // Если инструмента нет (руки), скорость обычная
        }
    }
}