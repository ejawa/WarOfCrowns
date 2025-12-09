using UnityEngine;

namespace WarOfCrowns.World
{
    public class EnvironmentBender : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private MaterialPropertyBlock _propBlock;

        [Header("Настройки")]
        [SerializeField] private float speedMin = 1.0f;
        [SerializeField] private float speedMax = 2.5f;
        [SerializeField] private float swayStrength = 0.1f;

        private void Start()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _propBlock = new MaterialPropertyBlock();

            // Получаем текущие свойства (чтобы не стереть цвет и прочее)
            _renderer.GetPropertyBlock(_propBlock);

            // 1. Случайная скорость
            float randomSpeed = Random.Range(speedMin, speedMax);
            _propBlock.SetFloat("_WindSpeed", randomSpeed);

            // 2. Сила наклона
            _propBlock.SetFloat("_SwayStrength", swayStrength);

            // 3. Смещение времени (Рассинхрон)
            // Используем координату X, чтобы создать эффект "волны" ветра
            float timeOffset = transform.position.x + transform.position.y + Random.Range(0f, 5f);
            _propBlock.SetFloat("_TimeOffset", timeOffset);

            // Применяем свойства к этому конкретному дереву
            _renderer.SetPropertyBlock(_propBlock);
        }

        // Update не нужен, так как всё делает шейдер!
        // Это супер-оптимизировано.
    }
}