using UnityEngine;

namespace WarOfCrowns.Buildings
{
    public class BuildingFootprint : MonoBehaviour
    {
        [Header("Размер основания в тайлах")]
        [Min(1)] public int width = 2;  // Ширина
        [Min(1)] public int height = 2; // Высота

        [Header("Смещение проверки")]
        [Tooltip("Сдвиг относительно центра курсора. Полезно, чтобы опустить проверку к ногам здания.")]
        public Vector2 offset = Vector2.zero;

        // Рисуем гизмо прямо на префабе, чтобы удобно настраивать
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.5f); // Желтый полупрозрачный

            // Центр с учетом оффсета
            Vector3 center = transform.position + (Vector3)offset;

            // Размер (предполагаем, что 1 тайл = 1 юнит в мире, если нет - умножь на размер тайла)
            Vector3 size = new Vector3(width, height, 1);

            Gizmos.DrawCube(center, size);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(center, size);
        }
    }
}