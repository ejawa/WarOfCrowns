using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using WarOfCrowns.Core;
using WarOfCrowns.Data;
using WarOfCrowns.Buildings;
using WarOfCrowns.World;
using System.Linq;

namespace WarOfCrowns.Units
{
    [RequireComponent(typeof(UnitAI), typeof(UnitVisuals))]
    public class Unit : NetworkBehaviour
    {
        [Header("Настройки")]
        [SerializeField] private GameObject selectionIndicator;

        [Header("Личность")]
        public NetworkVariable<FixedString64Bytes> unitNameNet = new NetworkVariable<FixedString64Bytes>("");
        public NetworkVariable<int> genderNet = new NetworkVariable<int>(0);

        public NetworkVariable<FixedString64Bytes> bodySpriteName = new NetworkVariable<FixedString64Bytes>("");
        public NetworkVariable<FixedString64Bytes> headSpriteName = new NetworkVariable<FixedString64Bytes>("");
        public NetworkVariable<FixedString64Bytes> clothesSpriteName = new NetworkVariable<FixedString64Bytes>("");
        public NetworkVariable<int> professionNet = new NetworkVariable<int>((int)ProfessionType.Unemployed);

        [Header("Потребности")]
        public float satiety = 100f;
        private const float HUNGER_RATE = 0.25f;
        public bool IsEating { get; set; } = false;
        private float starvationTimer = 1f;

        [Header("Экипировка")]
        public ResourceType currentTool = ResourceType.Wood;
        public ResourceType currentWeapon = ResourceType.Wood;
        public ResourceType currentArmor = ResourceType.Wood;

        // ВАЖНО: Дефолтное значение -1. Это предотвращает ложное срабатывание при спавне.
        public NetworkVariable<int> ownerKingdomID = new NetworkVariable<int>(-1);

        [HideInInspector] public Kingdom OwningKingdom;
        public string uniqueID;

        private UnitAI _ai;
        private Health _health;
        private UnitVisuals _visuals;
        private float _manualOverrideTimer = 0f;

        // Переменные сохранения
        [HideInInspector] public string savedWorkplaceID;
        [HideInInspector] public UnitState savedState;
        private string _savedResourceID;
        private bool _savedIsMoving;
        private Vector3 _savedMoveTarget;

        public string unitName => unitNameNet.Value.ToString();
        public Gender gender => (Gender)genderNet.Value;
        public ProfessionType profession => (ProfessionType)professionNet.Value;

        private void Awake()
        {
            _ai = GetComponent<UnitAI>();
            _health = GetComponent<Health>();
            _visuals = GetComponent<UnitVisuals>();
            if (string.IsNullOrEmpty(uniqueID)) uniqueID = System.Guid.NewGuid().ToString();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // 1. Подписываемся на изменение владельца
            // Это сработает, когда сервер пришлет настоящий ID (например, сменит -1 на 1)
            ownerKingdomID.OnValueChanged += (oldVal, newVal) =>
            {
                UpdateKingdomReference();
                CheckPopulationRegistration();
            };

            // 2. Обновляем ссылку сразу (вдруг ID уже пришел)
            UpdateKingdomReference();

            if (IsServer && string.IsNullOrEmpty(unitNameNet.Value.ToString()))
            {
                InitializeNewUnitOnServer();
            }

            bodySpriteName.OnValueChanged += (o, n) => UpdateVisuals();
            headSpriteName.OnValueChanged += (o, n) => UpdateVisuals();
            clothesSpriteName.OnValueChanged += (o, n) => UpdateVisuals();

            UpdateVisuals();

            // 3. Пробуем зарегистрироваться (если ID уже валидный)
            CheckPopulationRegistration();
        }

        // --- УМНАЯ РЕГИСТРАЦИЯ ---
        public void CheckPopulationRegistration()
        {
            // Если ID все еще -1 (данные не пришли), ждем
            if (ownerKingdomID.Value == -1) return;

            if (Kingdom.PlayerKingdom == null || PopulationManager.Instance == null) return;

            int myLocalID = Kingdom.PlayerKingdom.kingdomID;
            int unitOwnerID = ownerKingdomID.Value;

            // Если этот юнит принадлежит МНЕ (Локальному игроку)
            if (unitOwnerID == myLocalID)
            {
                PopulationManager.Instance.AddUnit(this);
            }
            else
            {
                // Если это чужой юнит - убираем из моего списка (чтобы не считать чужих)
                PopulationManager.Instance.RemoveUnit(this);
            }
        }

        private void UpdateKingdomReference()
        {
            Kingdom[] kingdoms = FindObjectsOfType<Kingdom>();
            foreach (var k in kingdoms)
            {
                if (k.kingdomID == ownerKingdomID.Value) { OwningKingdom = k; break; }
            }
        }

        private void InitializeNewUnitOnServer()
        {
            Gender g = (Random.Range(0, 2) == 0) ? Gender.Male : Gender.Female;
            genderNet.Value = (int)g;

            if (GameManager.Instance != null)
            {
                unitNameNet.Value = GameManager.Instance.GetRandomFullName(g);
                if (GameManager.Instance.AppearanceDB != null)
                {
                    var db = GameManager.Instance.AppearanceDB;
                    var body = db.GetRandomBody();
                    var head = db.GetRandomHead(g);
                    var cloth = db.GetRandomPeasantClothes();

                    if (body != null && body.idle != null) bodySpriteName.Value = body.idle.name;
                    if (head != null && head.idle != null) headSpriteName.Value = head.idle.name;
                    if (cloth != null && cloth.idle != null) clothesSpriteName.Value = cloth.idle.name;
                }
            }
            else unitNameNet.Value = "Peasant";
        }

        private void UpdateVisuals()
        {
            if (GameManager.Instance == null || GameManager.Instance.AppearanceDB == null) return;
            var db = GameManager.Instance.AppearanceDB;
            _visuals.LoadAppearance(
                db.GetSpriteSetByName(bodySpriteName.Value.ToString()),
                db.GetSpriteSetByName(headSpriteName.Value.ToString()),
                db.GetSpriteSetByName(clothesSpriteName.Value.ToString())
            );
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            // Обязательно убираем из списка при уничтожении
            if (PopulationManager.Instance != null)
            {
                PopulationManager.Instance.RemoveUnit(this);
            }
        }

        private void Update()
        {
            if (!IsEating) satiety -= HUNGER_RATE * Time.deltaTime;
            if (_manualOverrideTimer > 0) _manualOverrideTimer -= Time.deltaTime;

            if (IsOwner)
            {
                if (satiety < 30f && _ai.CurrentState != UnitState.SeekingFood) _ai.SeekFood();
            }

            if (IsServer && satiety <= 0f)
            {
                satiety = 0;
                starvationTimer -= Time.deltaTime;
                if (starvationTimer <= 0)
                {
                    if (_health != null) _health.TakeDamage(1);
                    else GetComponent<NetworkObject>().Despawn();
                    starvationTimer = 1f;
                }
            }
        }

        public void EquipItem(ResourceType item)
        {
            string n = item.ToString();
            if (n.Contains("Pickaxe") || n.Contains("Axe") || n.Contains("Hammer")) currentTool = item;
            else if (n.Contains("Sword") || n.Contains("Spear") || n.Contains("Bow")) currentWeapon = item;
            else if (n.Contains("Armor")) currentArmor = item;

            if (GameManager.Instance)
                _visuals.UpdateEquipment(currentTool, currentWeapon, currentArmor, GameManager.Instance.AppearanceDB);
        }

        public void SetProfession(ProfessionType newProf)
        {
            if (IsServer) professionNet.Value = (int)newProf;
            if (GameManager.Instance)
                _visuals.UpdateProfession(newProf, GameManager.Instance.AppearanceDB);
        }

        public void SetManualCommandOverride() { _manualOverrideTimer = 20f; IsEating = false; }
        public void Eat(int amount) { satiety += amount; if (satiety > 100f) satiety = 100f; }

        public void Select() { if (selectionIndicator) selectionIndicator.SetActive(true); }
        public void Deselect() { if (selectionIndicator) selectionIndicator.SetActive(false); }

        public UnitSaveData GetSaveData() { return new UnitSaveData(); }
        public void LoadFromData(UnitSaveData data) { }
        public void RestoreActions() { }
    }
}