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
        public Sprite unitPortrait; // Для UI

        [Header("Потребности")]
        public float satiety = 100f;
        private const float HUNGER_RATE = 0.5f;
        public bool IsEating { get; set; } = false;

        [Header("Экипировка")]
        public ResourceType currentTool = ResourceType.Wood;
        public ResourceType currentWeapon = ResourceType.Wood;
        public ResourceType currentArmor = ResourceType.Wood;

        public Kingdom OwningKingdom { get; set; }
        public string uniqueID;

        private UnitAI _ai;
        private Health _health;
        private UnitVisuals _visuals;
        private float _manualOverrideTimer = 0f;

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
                // Генерация НОВОГО юнита
                gender = (Random.Range(0, 2) == 0) ? Gender.Male : Gender.Female;
                if (GameManager.Instance != null)
                {
                    unitName = GameManager.Instance.GetRandomFullName(gender);
                    unitPortrait = GameManager.Instance.GetRandomPortrait(gender);
                    // Генерация рандомной внешности
                    if (GameManager.Instance.AppearanceDB != null)
                        _visuals.InitAppearance(gender, GameManager.Instance.AppearanceDB);
                }
                else unitName = "Peasant";

                gameObject.name = $"Unit_{unitName}";
            }
            else
            {
                // ЗАГРУЖЕННЫЙ юнит
                // Обновляем визуал экипировки (одежда уже загружена в LoadFromData)
                if (GameManager.Instance != null && GameManager.Instance.AppearanceDB != null)
                {
                    _visuals.UpdateEquipment(currentTool, currentWeapon, currentArmor, GameManager.Instance.AppearanceDB);
                }
            }

            if (PopulationManager.Instance != null && !gameObject.CompareTag("Enemy"))
                PopulationManager.Instance.AddUnit(this);
        }

        private void Update()
        {
            if (!IsEating) satiety -= HUNGER_RATE * Time.deltaTime;
            if (_manualOverrideTimer > 0) _manualOverrideTimer -= Time.deltaTime;

            if (satiety < 30f && _ai.CurrentState != UnitState.SeekingFood && _manualOverrideTimer <= 0) _ai.SeekFood();

            if (satiety <= 0f)
            {
                satiety = 0;
                if (_health != null) _health.TakeDamage(9999); else Destroy(gameObject);
            }
        }

        public void EquipItem(ResourceType item)
        {
            string n = item.ToString();
            if (n.Contains("Pickaxe") || n.Contains("Axe") || n.Contains("Hammer")) currentTool = item;
            else if (n.Contains("Sword") || n.Contains("Spear") || n.Contains("Bow")) currentWeapon = item;
            else if (n.Contains("Armor")) currentArmor = item;

            if (GameManager.Instance != null)
                _visuals.UpdateEquipment(currentTool, currentWeapon, currentArmor, GameManager.Instance.AppearanceDB);
        }

        public void SetProfession(ProfessionType newProf)
        {
            profession = newProf;
            // Обновляем одежду (если она зависит от профессии)
            if (GameManager.Instance != null)
                _visuals.UpdateProfession(newProf, GameManager.Instance.AppearanceDB);
        }

        public void SetManualCommandOverride() { _manualOverrideTimer = 20f; IsEating = false; if (_ai.CurrentState == UnitState.SeekingFood) _ai.CancelAction(); }
        public void Eat(int amount) { satiety += amount; if (satiety > 100f) satiety = 100f; }
        private void OnDestroy() { if (PopulationManager.Instance != null && !gameObject.CompareTag("Enemy")) PopulationManager.Instance.RemoveUnit(this); }
        public void Select() { if (selectionIndicator) selectionIndicator.SetActive(true); }
        public void Deselect() { if (selectionIndicator) selectionIndicator.SetActive(false); }

        // --- СОХРАНЕНИЕ ---
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

            // Сохраняем экипировку
            data.weaponType = (int)this.currentWeapon;
            data.armorType = (int)this.currentArmor;
            data.toolType = (int)this.currentTool;

            // Сохраняем внешность (Имена спрайтов)
            data.bodySpriteName = _visuals.BodySpriteName;
            data.headSpriteName = _visuals.HeadSpriteName;
            data.clothesSpriteName = _visuals.ClothesSpriteName;

            // Сохраняем связи
            if (TryGetComponent<UnitWorker>(out var worker) && worker.CurrentJob != null)
            {
                var jobData = worker.CurrentJob.GetComponent<WarOfCrowns.Buildings.Building>();
                if (jobData != null) data.workplaceID = jobData.uniqueID;
            }
            if (TryGetComponent<UnitGatherer>(out var gatherer) && gatherer.CurrentTarget != null)
                data.targetResourceID = gatherer.CurrentTarget.uniqueID;
            if (TryGetComponent<UnitMotor>(out var motor))
            {
                data.isMoving = motor.IsMoving;
                data.moveTargetX = motor.TargetPosition.x;
                data.moveTargetY = motor.TargetPosition.y;
                data.moveTargetZ = motor.TargetPosition.z;
            }
            return data;
        }

        // --- ЗАГРУЗКА ---
        public void LoadFromData(UnitSaveData data)
        {
            this.uniqueID = data.uniqueID;
            this.unitName = data.unitName;
            this.gender = (Gender)data.gender;
            gameObject.name = $"Unit_{this.unitName}";
            transform.position = new Vector3(data.posX, data.posY, data.posZ);
            this.satiety = data.currentHunger;
            if (_health != null) _health.SetHealth(data.currentHealth);
            if (System.Enum.TryParse(data.profession, out ProfessionType prof)) this.profession = prof;

            // Загружаем экипировку
            this.currentWeapon = (ResourceType)data.weaponType;
            this.currentArmor = (ResourceType)data.armorType;
            this.currentTool = (ResourceType)data.toolType;

            // Загружаем внешность
            if (GameManager.Instance != null && GameManager.Instance.AppearanceDB != null)
            {
                unitPortrait = GameManager.Instance.GetRandomPortrait(gender); // Портрет рандомный

                AppearanceDatabase db = GameManager.Instance.AppearanceDB;
                SpriteSet body = db.GetSpriteSetByName(data.bodySpriteName);
                SpriteSet head = db.GetSpriteSetByName(data.headSpriteName);
                SpriteSet clothes = db.GetSpriteSetByName(data.clothesSpriteName);

                _visuals.LoadAppearance(body, head, clothes);
            }

            _savedResourceID = data.targetResourceID;
            _savedIsMoving = data.isMoving;
            _savedMoveTarget = new Vector3(data.moveTargetX, data.moveTargetY, data.moveTargetZ);
            _visuals.UpdateEquipment(currentTool, currentWeapon, currentArmor, GameManager.Instance.AppearanceDB);
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