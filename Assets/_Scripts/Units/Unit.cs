using UnityEngine;
using WarOfCrowns.Core;
using WarOfCrowns.Data;
using WarOfCrowns.World; // Для ResourceNode
using System.Linq;       // Для поиска по ID

namespace WarOfCrowns.Units
{
    [RequireComponent(typeof(UnitAI))]
    public class Unit : MonoBehaviour
    {
        [Header("Настройки")]
        [SerializeField] private GameObject selectionIndicator;

        [Header("Личность")]
        public string unitName;
        public Gender gender;
        public Sprite unitPortrait;
        public ProfessionType profession = ProfessionType.Unemployed;

        [Header("Потребности")]
        [Tooltip("100 = сыт, 0 = умирает")]
        public float satiety = 100f;
        private const float HUNGER_RATE = 0.5f;
        public bool IsEating { get; set; } = false;

        // Ссылки и ID
        public Kingdom OwningKingdom { get; set; }
        public string uniqueID; // Уникальный ID для сохранений

        // Компоненты
        private UnitAI _ai;
        private Health _health;
        private float _manualOverrideTimer = 0f;

        // --- ВРЕМЕННЫЕ ДАННЫЕ ДЛЯ ЗАГРУЗКИ ---
        // Мы храним их здесь, пока SaveManager не вызовет RestoreActions
        [HideInInspector] public string savedWorkplaceID;
        [HideInInspector] public UnitState savedState;
        private string _savedResourceID;
        private bool _savedIsMoving;
        private Vector3 _savedMoveTarget;

        private void Awake()
        {
            _ai = GetComponent<UnitAI>();
            _health = GetComponent<Health>();

            // Если это новый юнит (не загруженный), генерируем ID
            if (string.IsNullOrEmpty(uniqueID)) uniqueID = System.Guid.NewGuid().ToString();
        }

        private void Start()
        {
            // Генерация личности
            if (string.IsNullOrEmpty(unitName))
            {
                gender = (Random.Range(0, 2) == 0) ? Gender.Male : Gender.Female;

                if (GameManager.Instance != null)
                {
                    unitName = GameManager.Instance.GetRandomFullName(gender);
                    unitPortrait = GameManager.Instance.GetRandomPortrait(gender);
                }
                else
                {
                    unitName = "Peasant";
                }
                gameObject.name = $"Unit_{unitName}";
                satiety = 100f;
            }

            // Регистрация в населении
            if (PopulationManager.Instance != null && !gameObject.CompareTag("Enemy"))
                PopulationManager.Instance.AddUnit(this);
        }

        private void Update()
        {
            if (!IsEating) satiety -= HUNGER_RATE * Time.deltaTime;
            if (_manualOverrideTimer > 0) _manualOverrideTimer -= Time.deltaTime;

            // Поиск еды (AI)
            if (satiety < 30f && _ai.CurrentState != UnitState.SeekingFood && _manualOverrideTimer <= 0)
            {
                _ai.SeekFood();
            }

            // Смерть
            if (satiety <= 0f)
            {
                satiety = 0;
                Debug.Log($"{unitName} died of starvation!");
                if (_health != null) _health.TakeDamage(9999);
                else Destroy(gameObject);
            }
        }

        public void SetProfession(ProfessionType newProfession)
        {
            this.profession = newProfession;
        }

        public void SetManualCommandOverride()
        {
            _manualOverrideTimer = 20f;
            IsEating = false;
            if (_ai.CurrentState == UnitState.SeekingFood) _ai.CancelAction();
        }

        public void Eat(int amount)
        {
            satiety += amount;
            if (satiety > 100f) satiety = 100f;
        }

        private void OnDestroy()
        {
            if (PopulationManager.Instance != null && !gameObject.CompareTag("Enemy"))
                PopulationManager.Instance.RemoveUnit(this);
        }

        public void Select() { if (selectionIndicator) selectionIndicator.SetActive(true); }
        public void Deselect() { if (selectionIndicator) selectionIndicator.SetActive(false); }

        // ==========================================
        // СИСТЕМА СОХРАНЕНИЯ И ЗАГРУЗКИ
        // ==========================================

        public UnitSaveData GetSaveData()
        {
            UnitSaveData data = new UnitSaveData();

            // Основные данные
            data.uniqueID = this.uniqueID;
            data.unitName = this.unitName;
            data.gender = (int)this.gender;
            data.prefabName = "Peasant_Prototype";
            data.posX = transform.position.x;
            data.posY = transform.position.y;
            data.posZ = transform.position.z;

            // Состояние
            data.currentHunger = this.satiety;
            data.profession = this.profession.ToString();
            data.aiState = (int)_ai.CurrentState;
            if (_health != null) data.currentHealth = _health.CurrentHealth;

            // 1. Сохраняем РАБОТУ (ID здания)
            if (TryGetComponent<UnitWorker>(out var worker) && worker.CurrentJob != null)
            {
                var jobData = worker.CurrentJob.GetComponent<WarOfCrowns.Buildings.Building>();
                if (jobData != null) data.workplaceID = jobData.uniqueID;
            }

            // 2. Сохраняем СБОР РЕСУРСОВ (ID ресурса)
            // Нам нужно знать, какой именно куст мы рубим
            if (TryGetComponent<UnitGatherer>(out var gatherer) && gatherer.CurrentTarget != null)
            {
                data.targetResourceID = gatherer.CurrentTarget.uniqueID;
            }

            // 3. Сохраняем ДВИЖЕНИЕ (куда шли)
            if (TryGetComponent<UnitMotor>(out var motor))
            {
                data.isMoving = motor.IsMoving;
                data.moveTargetX = motor.TargetPosition.x;
                data.moveTargetY = motor.TargetPosition.y;
                data.moveTargetZ = motor.TargetPosition.z;
            }

            return data;
        }

        public void LoadFromData(UnitSaveData data)
        {
            this.uniqueID = data.uniqueID;
            this.unitName = data.unitName;
            this.gender = (Gender)data.gender;

            gameObject.name = $"Unit_{this.unitName}";
            transform.position = new Vector3(data.posX, data.posY, data.posZ);

            this.satiety = data.currentHunger;
            if (_health != null) _health.SetHealth(data.currentHealth);

            if (System.Enum.TryParse(data.profession, out ProfessionType prof))
                this.profession = prof;

            if (GameManager.Instance != null)
                unitPortrait = GameManager.Instance.GetRandomPortrait(gender);

            // Сохраняем "сложные" данные во временные переменные.
            // Мы не можем применить их прямо сейчас, потому что здания и ресурсы могут быть еще не загружены.
            this.savedWorkplaceID = data.workplaceID;
            this.savedState = (UnitState)data.aiState;

            this._savedResourceID = data.targetResourceID;
            this._savedIsMoving = data.isMoving;
            this._savedMoveTarget = new Vector3(data.moveTargetX, data.moveTargetY, data.moveTargetZ);
        }

        // Вызывается из SaveManager ПОСЛЕ загрузки всего мира
        public void RestoreActions()
        {
            // 1. Восстанавливаем РУБКУ/СБОР
            if (!string.IsNullOrEmpty(_savedResourceID))
            {
                // Ищем ресурс по ID среди всех загруженных ресурсов
                ResourceNode[] allResources = FindObjectsOfType<ResourceNode>();
                ResourceNode targetNode = allResources.FirstOrDefault(r => r.uniqueID == _savedResourceID);

                if (targetNode != null && TryGetComponent<UnitGatherer>(out var gatherer))
                {
                    gatherer.SetTarget(targetNode);
                    return; // Если начали собирать, движение произойдет само
                }
            }

            // 2. Восстанавливаем ДВИЖЕНИЕ
            if (_savedIsMoving)
            {
                // Если мы просто шли (и не работали, и не ели)
                if (_ai.CurrentState != UnitState.SeekingFood && _ai.CurrentState != UnitState.Working)
                {
                    GetComponent<UnitMotor>().MoveTo(_savedMoveTarget);
                }
            }

            // (Восстановление работы на здании происходит в SaveManager через JobBuilding.AddWorker)
        }
    }
}