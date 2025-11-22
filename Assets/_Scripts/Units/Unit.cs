using UnityEngine;
using WarOfCrowns.Core;
using WarOfCrowns.Data;
using WarOfCrowns.World;
using System.Linq;

namespace WarOfCrowns.Units
{
    [RequireComponent(typeof(UnitAI), typeof(UnitVisuals))]
    public class Unit : MonoBehaviour
    {
        [Header("Настройки")]
        [SerializeField] private GameObject selectionIndicator;

        [Header("Личность")]
        public string unitName;
        public Gender gender;
        public ProfessionType profession = ProfessionType.Unemployed;

        // --- ВЕРНУЛИ ЭТО ПОЛЕ ---
        public Sprite unitPortrait; // Аватарка для UI списков
        // ------------------------

        [Header("Потребности")]
        [Tooltip("100 = сыт, 0 = умирает")]
        public float satiety = 100f;
        private const float HUNGER_RATE = 0.5f;
        public bool IsEating { get; set; } = false;

        [Header("Экипировка")]
        public ResourceType currentTool = ResourceType.Wood;
        public ResourceType currentWeapon = ResourceType.Wood;
        public ResourceType currentArmor = ResourceType.Wood;

        // Ссылки
        public Kingdom OwningKingdom { get; set; }
        public string uniqueID;

        // Компоненты
        private UnitAI _ai;
        private Health _health;
        private UnitVisuals _visuals;
        private float _manualOverrideTimer = 0f;

        // Временные данные для загрузки
        [HideInInspector] public string savedWorkplaceID;
        [HideInInspector] public UnitState savedState;
        private string _savedResourceID;
        private bool _savedIsMoving;
        private Vector3 _savedMoveTarget;

        private void Awake()
        {
            _ai = GetComponent<UnitAI>();
            _health = GetComponent<Health>();
            _visuals = GetComponent<UnitVisuals>();

            if (string.IsNullOrEmpty(uniqueID)) uniqueID = System.Guid.NewGuid().ToString();
        }

        private void Start()
        {
            if (string.IsNullOrEmpty(unitName))
            {
                // 1. Генерируем пол
                gender = (Random.Range(0, 2) == 0) ? Gender.Male : Gender.Female;

                if (GameManager.Instance != null)
                {
                    // 2. Генерируем имя
                    unitName = GameManager.Instance.GetRandomFullName(gender);

                    // 3. Генерируем ПОРТРЕТ (Аватарку)
                    unitPortrait = GameManager.Instance.GetRandomPortrait(gender);

                    // 4. Генерируем ВНЕШНОСТЬ на карте (Тело, Голова, Одежда)
                    if (GameManager.Instance.AppearanceDB != null)
                    {
                        _visuals.InitAppearance(gender, GameManager.Instance.AppearanceDB);
                    }
                }
                else
                {
                    unitName = "Peasant";
                }
                gameObject.name = $"Unit_{unitName}";
                satiety = 100f;
            }

            if (PopulationManager.Instance != null && !gameObject.CompareTag("Enemy"))
                PopulationManager.Instance.AddUnit(this);
        }

        private void Update()
        {
            if (!IsEating) satiety -= HUNGER_RATE * Time.deltaTime;
            if (_manualOverrideTimer > 0) _manualOverrideTimer -= Time.deltaTime;

            if (satiety < 30f && _ai.CurrentState != UnitState.SeekingFood && _manualOverrideTimer <= 0)
            {
                _ai.SeekFood();
            }

            if (satiety <= 0f)
            {
                satiety = 0;
                if (_health != null) _health.TakeDamage(9999);
                else Destroy(gameObject);
            }
        }

        public void EquipItem(ResourceType item)
        {
            string itemName = item.ToString();

            if (itemName.Contains("Pickaxe") || itemName.Contains("Axe") || itemName.Contains("Hammer"))
                currentTool = item;
            else if (itemName.Contains("Sword") || itemName.Contains("Spear") || itemName.Contains("Bow"))
                currentWeapon = item;
            else if (itemName.Contains("Armor"))
                currentArmor = item;

            if (GameManager.Instance != null && GameManager.Instance.AppearanceDB != null)
            {
                _visuals.UpdateEquipment(currentTool, currentWeapon, currentArmor, GameManager.Instance.AppearanceDB);
            }
        }

        public void SetProfession(ProfessionType newProfession)
        {
            this.profession = newProfession;
            if (GameManager.Instance != null && GameManager.Instance.AppearanceDB != null)
                _visuals.UpdateProfession(newProfession, GameManager.Instance.AppearanceDB);
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

        public UnitSaveData GetSaveData()
        {
            UnitSaveData data = new UnitSaveData();
            data.uniqueID = this.uniqueID;
            data.unitName = this.unitName;
            data.gender = (int)this.gender;
            data.prefabName = "Peasant_Prototype";
            data.posX = transform.position.x;
            data.posY = transform.position.y;
            data.posZ = transform.position.z;
            data.currentHunger = this.satiety;
            data.profession = this.profession.ToString();
            data.aiState = (int)_ai.CurrentState;
            if (_health != null) data.currentHealth = _health.CurrentHealth;

            if (TryGetComponent<UnitWorker>(out var worker) && worker.CurrentJob != null)
            {
                var jobData = worker.CurrentJob.GetComponent<WarOfCrowns.Buildings.Building>();
                if (jobData != null) data.workplaceID = jobData.uniqueID;
            }
            if (TryGetComponent<UnitGatherer>(out var gatherer) && gatherer.CurrentTarget != null)
            {
                data.targetResourceID = gatherer.CurrentTarget.uniqueID;
            }
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
            this.unitName = data.unitName;
            this.gender = (Gender)data.gender;
            gameObject.name = $"Unit_{this.unitName}";
            transform.position = new Vector3(data.posX, data.posY, data.posZ);
            this.satiety = data.currentHunger;
            if (_health != null) _health.SetHealth(data.currentHealth);
            if (System.Enum.TryParse(data.profession, out ProfessionType prof)) this.profession = prof;

            if (GameManager.Instance != null)
            {
                // Генерируем внешность и портрет заново при загрузке (упрощение)
                unitPortrait = GameManager.Instance.GetRandomPortrait(gender);

                if (GameManager.Instance.AppearanceDB != null)
                    _visuals.InitAppearance(gender, GameManager.Instance.AppearanceDB);
            }

            _savedResourceID = data.targetResourceID;
            _savedIsMoving = data.isMoving;
            _savedMoveTarget = new Vector3(data.moveTargetX, data.moveTargetY, data.moveTargetZ);
            this.savedWorkplaceID = data.workplaceID;
            this.savedState = (UnitState)data.aiState;
        }

        public void RestoreActions()
        {
            if (!string.IsNullOrEmpty(_savedResourceID))
            {
                ResourceNode[] allResources = FindObjectsOfType<ResourceNode>();
                ResourceNode targetNode = allResources.FirstOrDefault(r => r.uniqueID == _savedResourceID);
                if (targetNode != null && TryGetComponent<UnitGatherer>(out var gatherer)) gatherer.SetTarget(targetNode);
            }
            if (_savedIsMoving)
            {
                if (_ai.CurrentState != UnitState.SeekingFood && _ai.CurrentState != UnitState.Working)
                    GetComponent<UnitMotor>().MoveTo(_savedMoveTarget);
            }
        }
    }
}