using UnityEngine;
using WarOfCrowns.Core;
using WarOfCrowns.Data; // Для UnitSaveData

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

        // Для сохранения здоровья нам нужна ссылка на компонент Health
        private Health _health;

        public Kingdom OwningKingdom { get; set; }
        private UnitAI _ai;
        private float _manualOverrideTimer = 0f;

        private void Awake()
        {
            _ai = GetComponent<UnitAI>();
            _health = GetComponent<Health>(); // Убедись, что компонент Health есть!
        }

        private void Start()
        {
            if (PopulationManager.Instance != null && !gameObject.CompareTag("Enemy"))
                PopulationManager.Instance.AddUnit(this); // Передаем себя (this)
        }

        private void Update()
        {
            hunger += HUNGER_RATE * Time.deltaTime;
            if (_manualOverrideTimer > 0) _manualOverrideTimer -= Time.deltaTime;

            if (hunger > 70f && _ai.CurrentState != UnitState.SeekingFood && _manualOverrideTimer <= 0)
            {
                _ai.SeekFood();
            }
            if (hunger >= 100f)
            {
                Debug.Log($"{gameObject.name} died of starvation!");
                if (_health != null) _health.TakeDamage(9999); // Убиваем через Health
                else Destroy(gameObject);
            }
        }

        public void SetManualCommandOverride()
        {
            _manualOverrideTimer = 20f;
            if (_ai.CurrentState == UnitState.SeekingFood) _ai.CancelAction();
        }

        public void Eat(int satiety)
        {
            hunger -= satiety;
            if (hunger < 0) hunger = 0;
        }

        private void OnDestroy()
        {
            if (PopulationManager.Instance != null && !gameObject.CompareTag("Enemy"))
                PopulationManager.Instance.RemoveUnit(this); // Передаем себя
        }

        public void Select() { selectionIndicator.SetActive(true); }
        public void Deselect() { selectionIndicator.SetActive(false); }

        // --- СИСТЕМА СОХРАНЕНИЯ ---

        // 1. Упаковать себя в данные
        public UnitSaveData GetSaveData()
        {
            UnitSaveData data = new UnitSaveData();
            data.unitName = gameObject.name; // Пока просто имя объекта
            data.prefabName = "Peasant_Prototype"; // Имя префаба в Resources (важно!)

            data.posX = transform.position.x;
            data.posY = transform.position.y;
            data.posZ = transform.position.z;

            data.currentHunger = this.hunger;

            if (_health != null) data.currentHealth = _health.CurrentHealth; // Нужно добавить свойство в Health

            return data;
        }

        // 2. Распаковать данные в себя
        public void LoadFromData(UnitSaveData data)
        {
            transform.position = new Vector3(data.posX, data.posY, data.posZ);
            this.hunger = data.currentHunger;
            gameObject.name = data.unitName;

            // Навигация может сбить позицию, поэтому телепортируем агента, если он есть
            // (В нашей текущей версии без NavMeshAgent это не критично, но на будущее)

            if (_health != null) _health.SetHealth(data.currentHealth); // Нужно добавить метод в Health
        }
    }
}