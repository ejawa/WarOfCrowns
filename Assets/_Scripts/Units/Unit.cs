using UnityEngine;
using WarOfCrowns.Core;
using WarOfCrowns.Units;

namespace WarOfCrowns.Units
{
    [RequireComponent(typeof(UnitAI))]
    public class Unit : MonoBehaviour
    {
        [Header("Настройки")]
        [SerializeField] private GameObject selectionIndicator;

        [Header("Потребности")]
        public float hunger = 0f;
        private const float HUNGER_RATE = 0.5f;

        public Kingdom OwningKingdom { get; set; }
        private UnitAI _ai;

        // Таймер, чтобы юнит не бежал есть сразу после приказа игрока
        private float _manualOverrideTimer = 0f;

        private void Awake() { _ai = GetComponent<UnitAI>(); }

        private void Start()
        {
            if (PopulationManager.Instance != null && !gameObject.CompareTag("Enemy"))
                PopulationManager.Instance.AddUnit();
        }

        private void Update()
        {
            hunger += HUNGER_RATE * Time.deltaTime;
            if (_manualOverrideTimer > 0) _manualOverrideTimer -= Time.deltaTime;

            // Ищем еду, ТОЛЬКО если:
            // 1. Голод сильный (>70)
            // 2. Мы уже не ищем еду
            // 3. Игрок не приказал нам работать (таймер override истек)
            if (hunger > 70f && _ai.CurrentState != UnitState.SeekingFood && _manualOverrideTimer <= 0)
            {
                _ai.SeekFood();
            }

            if (hunger >= 100f)
            {
                Debug.Log($"{gameObject.name} died of starvation!");
                Destroy(gameObject);
            }
        }

        // Этот метод мы вызовем из контроллера при любом клике ПКМ
        public void SetManualCommandOverride()
        {
            _manualOverrideTimer = 20f; // Юнит будет терпеть голод 20 секунд ради работы
            if (_ai.CurrentState == UnitState.SeekingFood)
            {
                _ai.CancelAction(); // Прерываем текущий поиск еды
            }
        }

        public void Eat(int satiety)
        {
            hunger -= satiety;
            if (hunger < 0) hunger = 0;
        }

        // ... OnDestroy, Select, Deselect
        private void OnDestroy() { if (PopulationManager.Instance != null && !gameObject.CompareTag("Enemy")) PopulationManager.Instance.RemoveUnit(); }
        public void Select() { selectionIndicator.SetActive(true); }
        public void Deselect() { selectionIndicator.SetActive(false); }
    }
}