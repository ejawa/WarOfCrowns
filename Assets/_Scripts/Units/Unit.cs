using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using WarOfCrowns.Core;
using WarOfCrowns.Data;
using WarOfCrowns.Buildings;
using WarOfCrowns.World;
using System.Collections;

namespace WarOfCrowns.Units
{
    public enum UnitStance { Aggressive, Defensive, Hold }

    [RequireComponent(typeof(UnitAI), typeof(UnitVisuals))]
    public class Unit : NetworkBehaviour
    {
        [Header("Настройки")]
        [SerializeField] private GameObject selectionIndicator;

        [Header("Личность (Сетевая)")]
        public NetworkVariable<FixedString64Bytes> unitNameNet = new NetworkVariable<FixedString64Bytes>("");
        public NetworkVariable<int> genderNet = new NetworkVariable<int>(0);
        public NetworkVariable<int> professionNet = new NetworkVariable<int>((int)ProfessionType.Unemployed);
        public NetworkVariable<UnitStance> stanceNet = new NetworkVariable<UnitStance>(UnitStance.Defensive);

        [Header("Внешность")]
        public NetworkVariable<int> bodyIndex = new NetworkVariable<int>(-1);
        public NetworkVariable<int> headIndex = new NetworkVariable<int>(-1);
        public NetworkVariable<int> clothesIndex = new NetworkVariable<int>(-1);
        public NetworkVariable<int> plumeIndex = new NetworkVariable<int>(-1);
        public NetworkVariable<float> visualTint = new NetworkVariable<float>(1f);

        [Header("Характеристики")]
        public float satiety = 100f;
        private const float HUNGER_RATE = 0.25f;
        public bool IsEating { get; set; } = false;
        private float starvationTimer = 1f;

        // Состояния среды
        public bool IsInWater { get; private set; }
        private bool _isDrowning = false;

        [Header("Экипировка (Сетевая)")]
        public NetworkVariable<int> currentToolType = new NetworkVariable<int>((int)ResourceType.Wood);
        public NetworkVariable<int> currentWeaponType = new NetworkVariable<int>((int)ResourceType.Wood);
        public NetworkVariable<int> currentArmorType = new NetworkVariable<int>((int)ResourceType.Wood);

        public NetworkVariable<int> toolDurability = new NetworkVariable<int>(100);
        public NetworkVariable<int> weaponDurability = new NetworkVariable<int>(100);
        public NetworkVariable<int> armorDurability = new NetworkVariable<int>(100);

        [Header("Владелец")]
        public NetworkVariable<int> ownerKingdomID = new NetworkVariable<int>(-1);
        [HideInInspector] public Kingdom OwningKingdom;
        public string uniqueID;

        [Header("Жилье")]
        // ID NetworkObject Дома, где прописан юнит
        public NetworkVariable<ulong> residenceNetID = new NetworkVariable<ulong>(0);
        // ID NetworkObject Здания, где юнит сейчас НАХОДИТСЯ (спрятан)
        public NetworkVariable<ulong> currentBuildingNetID = new NetworkVariable<ulong>(0);

        // Свойства
        public string UnitName => unitNameNet.Value.ToString();
        public Gender UnitGender => (Gender)genderNet.Value;
        public ProfessionType Profession => (ProfessionType)professionNet.Value;
        public ResourceType Tool => (ResourceType)currentToolType.Value;
        public ResourceType Weapon => (ResourceType)currentWeaponType.Value;
        public ResourceType Armor => (ResourceType)currentArmorType.Value;
        public UnitStance Stance => stanceNet.Value;

        // Визуал
        public string BodyName => "Body_" + bodyIndex.Value;
        public string HeadName => "Head_" + headIndex.Value;
        public string ClothesName => "Clothes_" + clothesIndex.Value;

        private UnitAI _ai;
        private Health _health;
        private UnitVisuals _visuals;
        private UnitMotor _motor;
        private float _manualOverrideTimer = 0f;
        private bool _visualsLoaded = false;

        // Заглушки сохранения
        [HideInInspector] public string savedWorkplaceID;
        [HideInInspector] public UnitState savedState;
        [HideInInspector] public string savedTargetResourceID;
        [HideInInspector] public bool savedIsMoving;
        [HideInInspector] public Vector3 savedMoveTarget;

        private void Awake()
        {
            _ai = GetComponent<UnitAI>();
            _health = GetComponent<Health>();
            _visuals = GetComponent<UnitVisuals>();
            _motor = GetComponent<UnitMotor>();
            if (string.IsNullOrEmpty(uniqueID)) uniqueID = System.Guid.NewGuid().ToString();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // 1. Инициализация владельца
            UpdateKingdomReference();
            ownerKingdomID.OnValueChanged += (o, n) =>
            {
                UpdateKingdomReference();
                CheckPopulationRegistration();
            };

            // 2. Инициализация визуала
            bodyIndex.OnValueChanged += (o, n) => TryUpdateVisuals();
            headIndex.OnValueChanged += (o, n) => TryUpdateVisuals();
            clothesIndex.OnValueChanged += (o, n) => TryUpdateVisuals();
            plumeIndex.OnValueChanged += (o, n) => TryUpdateVisuals();
            visualTint.OnValueChanged += (o, n) => TryUpdateVisuals();

            // 3. Экипировка и статус
            currentToolType.OnValueChanged += (o, n) => TryUpdateVisuals();
            currentWeaponType.OnValueChanged += (o, n) => TryUpdateVisuals();
            currentArmorType.OnValueChanged += (o, n) => TryUpdateVisuals();
            professionNet.OnValueChanged += (o, n) => TryUpdateVisuals();
            stanceNet.OnValueChanged += (o, n) => _visuals.UpdateStanceVisual(n);

            // 4. Проверка нахождения в здании
            if (currentBuildingNetID.Value != 0) SetVisibility(false);
            currentBuildingNetID.OnValueChanged += OnBuildingStateChanged;

            if (IsServer && bodyIndex.Value == -1) InitializeNewUnitOnServer();

            StartCoroutine(WaitForDatabaseRoutine());
            CheckPopulationRegistration();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            if (PopulationManager.Instance) PopulationManager.Instance.RemoveUnit(this);
            currentBuildingNetID.OnValueChanged -= OnBuildingStateChanged;
        }

        private void Update()
        {
            if (_isDrowning) return;

            // Повторная попытка загрузки визуала
            if (!_visualsLoaded && WorldState.Instance != null && WorldState.Instance.AppearanceDB != null)
                TryUpdateVisuals();

            // Логика Сервера
            if (IsServer)
            {
                if (!IsEating) satiety -= HUNGER_RATE * Time.deltaTime;
                if (satiety <= 0)
                {
                    satiety = 0;
                    starvationTimer -= Time.deltaTime;
                    if (starvationTimer <= 0)
                    {
                        if (_health) _health.TakeDamage(1);
                        else GetComponent<NetworkObject>().Despawn();
                        starvationTimer = 1f;
                    }
                }
                HandleDrowning();
            }

            // Логика Владельца
            if (IsOwner)
            {
                if (satiety < 30f && _ai.CurrentState != UnitState.SeekingFood && _ai.CurrentState != UnitState.Fighting)
                    _ai.SeekFood();
            }

            // Проверка воды (Визуал)
            CheckWaterBiome();

            if (_manualOverrideTimer > 0) _manualOverrideTimer -= Time.deltaTime;
        }

        // --- УПРАВЛЕНИЕ ВИДИМОСТЬЮ ---
        private void OnBuildingStateChanged(ulong oldId, ulong newId)
        {
            // 0 = вышел, >0 = зашел
            SetVisibility(newId == 0);
        }

        public void SetVisibility(bool visible)
        {
            var renderers = GetComponentsInChildren<SpriteRenderer>();
            foreach (var r in renderers) r.enabled = visible;

            var col = GetComponent<Collider2D>();
            if (col) col.enabled = visible;

            var canvas = GetComponentInChildren<Canvas>();
            if (canvas) canvas.enabled = visible;

            if (!visible) Deselect();
        }

        // --- ЛОГИКА ВОДЫ ---
        private void CheckWaterBiome()
        {
            if (WorldGenerator.Instance != null)
            {
                string biome = WorldGenerator.Instance.GetBiomeAt(transform.position);
                bool water = biome.Contains("Water") || biome.Contains("Ocean") || biome.Contains("Sea");

                if (water != IsInWater)
                {
                    IsInWater = water;
                    _visuals.ForceUpdateState();
                }
            }
        }

        private void HandleDrowning()
        {
            if (!IsInWater) return;
            if (_isDrowning) return;

            bool hasHeavyArmor = Armor != ResourceType.Wood;
            string biome = WorldGenerator.Instance.GetBiomeAt(transform.position);
            bool deepWater = biome.Contains("Deep") || biome.Contains("Sea");

            if (hasHeavyArmor && deepWater)
            {
                _isDrowning = true;
                _ai.CancelAction();
                _motor.StopMoving();
                StartCoroutine(DrowningRoutine());
                DrowningClientRpc();
            }
        }

        [ClientRpc] private void DrowningClientRpc() { if (!IsServer) StartCoroutine(DrowningRoutine()); }

        private IEnumerator DrowningRoutine()
        {
            _isDrowning = true;
            float animDuration = 1.0f;
            if (_visuals)
            {
                _visuals.TriggerDrowningEffect();
                animDuration = _visuals.DrownAnimationLength;
            }
            yield return new WaitForSeconds(animDuration);

            if (IsServer)
            {
                if (_health) _health.TakeDamage(9999);
                else GetComponent<NetworkObject>().Despawn();
            }
        }

        // --- СЕТЕВЫЕ ССЫЛКИ ---
        public void ForceUpdateKingdomReferenceServer() { UpdateKingdomReference(); }

        private void UpdateKingdomReference()
        {
            if (ownerKingdomID.Value == -1) return;
            OwningKingdom = Kingdom.GetKingdomByID(ownerKingdomID.Value);
        }

        private void CheckPopulationRegistration()
        {
            if (PopulationManager.Instance == null || Kingdom.PlayerKingdom == null) return;
            if (gameObject.CompareTag("Enemy")) return;
            if (ownerKingdomID.Value == Kingdom.PlayerKingdom.kingdomID.Value)
                PopulationManager.Instance.AddUnit(this);
            else
                PopulationManager.Instance.RemoveUnit(this);
        }

        // --- ИНИЦИАЛИЗАЦИЯ И ВИЗУАЛ ---
        private IEnumerator WaitForDatabaseRoutine()
        {
            while (WorldState.Instance == null || WorldState.Instance.AppearanceDB == null) yield return null;
            TryUpdateVisuals();
            _visuals.UpdateStanceVisual(Stance);
        }

        private void InitializeNewUnitOnServer()
        {
            Gender g = (Random.Range(0, 2) == 0) ? Gender.Male : Gender.Female;
            genderNet.Value = (int)g;
            visualTint.Value = Random.Range(0.6f, 1.0f);

            if (WorldState.Instance) unitNameNet.Value = WorldState.Instance.GetRandomFullName(g);

            if (WorldState.Instance && WorldState.Instance.AppearanceDB)
            {
                var db = WorldState.Instance.AppearanceDB;
                if (db.bodies != null && db.bodies.Count > 0) bodyIndex.Value = Random.Range(0, db.bodies.Count);
                if (g == Gender.Male && db.maleHeads != null && db.maleHeads.Count > 0)
                    headIndex.Value = Random.Range(0, db.maleHeads.Count);
                else if (g == Gender.Female && db.femaleHeads != null && db.femaleHeads.Count > 0)
                    headIndex.Value = Random.Range(0, db.femaleHeads.Count);
                if (db.peasantClothes != null && db.peasantClothes.Count > 0)
                    clothesIndex.Value = Random.Range(0, db.peasantClothes.Count);
                if (db.soldierPlumes != null && db.soldierPlumes.Count > 0)
                    plumeIndex.Value = Random.Range(0, db.soldierPlumes.Count);
            }
        }

        private void TryUpdateVisuals()
        {
            if (WorldState.Instance == null || WorldState.Instance.AppearanceDB == null) return;
            var db = WorldState.Instance.AppearanceDB;

            SpriteSet bodySet = null;
            if (db.bodies != null && bodyIndex.Value >= 0 && bodyIndex.Value < db.bodies.Count)
                bodySet = db.bodies[bodyIndex.Value];

            SpriteSet headSet = null;
            var headsList = (UnitGender == Gender.Male) ? db.maleHeads : db.femaleHeads;
            if (headsList != null && headIndex.Value >= 0 && headIndex.Value < headsList.Count)
                headSet = headsList[headIndex.Value];

            SpriteSet clothesSet = null;
            var clothesList = (Profession == ProfessionType.Soldier) ? db.soldierClothes : db.peasantClothes;
            if (clothesList != null && clothesList.Count > 0)
            {
                int safeIndex = Mathf.Abs(clothesIndex.Value) % clothesList.Count;
                clothesSet = clothesList[safeIndex];
            }

            SpriteSet plumeSet = null;
            if (db.soldierPlumes != null && db.soldierPlumes.Count > 0 && plumeIndex.Value >= 0)
            {
                int safeIndex = Mathf.Abs(plumeIndex.Value) % db.soldierPlumes.Count;
                plumeSet = db.soldierPlumes[safeIndex];
            }

            _visuals.LoadAppearance(bodySet, headSet, clothesSet, plumeSet);
            _visuals.UpdateEquipment(Tool, Weapon, Armor, db);

            gameObject.name = $"Unit_{UnitName}";
            if (bodyIndex.Value != -1) _visualsLoaded = true;
        }

        // --- RPC И ИНСТРУМЕНТЫ ---
        public void SetStance(UnitStance newStance) { if (IsServer) stanceNet.Value = newStance; else SetStanceServerRpc(newStance); }
        [ServerRpc(RequireOwnership = false)] private void SetStanceServerRpc(UnitStance newStance) { stanceNet.Value = newStance; }
        public void Eat(int amount) { if (IsServer) { satiety += amount; if (satiety > 100f) satiety = 100f; } else EatServerRpc(amount); }
        [ServerRpc(RequireOwnership = false)] private void EatServerRpc(int amount) { Eat(amount); }
        public void SetManualCommandOverride() { _manualOverrideTimer = 20f; IsEating = false; }
        public void SetProfession(ProfessionType p) { if (IsServer) professionNet.Value = (int)p; }

        public float GetToolSpeedMultiplier(string targetResourceName = "")
        {
            if (WorldState.Instance == null || WorldState.Instance.ToolDB == null) return 1.0f;
            if (!string.IsNullOrEmpty(targetResourceName))
            {
                string toolName = Tool.ToString();
                bool isMatch = false;
                if (targetResourceName.Contains("Wood") && toolName.Contains("Axe")) isMatch = true;
                else if ((targetResourceName.Contains("Stone") || targetResourceName.Contains("Ore") || targetResourceName.Contains("Gold") || targetResourceName.Contains("Coal")) && toolName.Contains("Pickaxe")) isMatch = true;
                else if (targetResourceName == "Construction" && toolName.Contains("Hammer")) isMatch = true;
                if (!isMatch) return 1.0f;
            }
            return WorldState.Instance.ToolDB.GetMultiplier(Tool);
        }

        public bool HasBetterToolInStock(ResourceType requiredCategory)
        {
            if (OwningKingdom == null) return false;
            string reqName = requiredCategory.ToString();
            bool wrongType = true;
            if (Tool.ToString().Contains("Axe") && reqName.Contains("Axe")) wrongType = false;
            if (Tool.ToString().Contains("Pickaxe") && reqName.Contains("Pickaxe")) wrongType = false;
            if (Tool.ToString().Contains("Hammer") && reqName.Contains("Hammer")) wrongType = false;
            if (Tool == ResourceType.Wood || wrongType)
            {
                ResourceType[] priority = GetToolsByPriority(reqName);
                foreach (var tool in priority) { if (tool == ResourceType.Wood) continue; if (OwningKingdom.GetResourceAmount(tool) > 0) return true; }
            }
            return false;
        }

        public void EquipBestTool(ResourceType requiredCategory)
        {
            if (!IsServer || OwningKingdom == null) return;
            string reqName = requiredCategory.ToString();
            ResourceType[] priority = GetToolsByPriority(reqName);
            foreach (var tool in priority)
            {
                if (tool == ResourceType.Wood) continue;
                if (OwningKingdom.GetResourceAmount(tool) > 0)
                {
                    if (Tool != ResourceType.Wood) OwningKingdom.AddResource(Tool, 1);
                    OwningKingdom.AddResource(tool, -1);
                    currentToolType.Value = (int)tool;
                    toolDurability.Value = 100;
                    return;
                }
            }
        }

        public void EquipItemServer(ResourceType item)
        {
            if (!IsServer) return;
            string n = item.ToString();
            if (n.Contains("Pickaxe") || n.Contains("Axe") || n.Contains("Hammer")) { currentToolType.Value = (int)item; toolDurability.Value = 100; }
            else if (n.Contains("Sword") || n.Contains("Spear") || n.Contains("Bow")) { currentWeaponType.Value = (int)item; weaponDurability.Value = 100; }
            else if (n.Contains("Armor")) { currentArmorType.Value = (int)item; armorDurability.Value = 100; }
        }

        public void ReduceDurability(bool isTool, int amount = 1)
        {
            if (!IsServer) return;
            if (isTool)
            {
                if (Tool == ResourceType.Wood) return;
                toolDurability.Value -= amount;
                if (toolDurability.Value <= 0) currentToolType.Value = (int)ResourceType.Wood;
            }
            else
            {
                if (Weapon == ResourceType.Wood) return;
                weaponDurability.Value -= amount;
                if (weaponDurability.Value <= 0) currentWeaponType.Value = (int)ResourceType.Wood;
            }
        }

        private ResourceType[] GetToolsByPriority(string n)
        {
            if (n.Contains("Pickaxe")) return new[] { ResourceType.ObsidianPickaxe, ResourceType.MithrilPickaxe, ResourceType.SteelPickaxe, ResourceType.GoldPickaxe, ResourceType.IronPickaxe, ResourceType.StonePickaxe, ResourceType.WoodenPickaxe };
            if (n.Contains("Axe")) return new[] { ResourceType.ObsidianAxe, ResourceType.MithrilAxe, ResourceType.SteelAxe, ResourceType.GoldAxe, ResourceType.IronAxe, ResourceType.StoneAxe, ResourceType.WoodenAxe };
            if (n.Contains("Hammer")) return new[] { ResourceType.ObsidianHammer, ResourceType.MithrilHammer, ResourceType.SteelHammer, ResourceType.GoldHammer, ResourceType.IronHammer, ResourceType.StoneHammer, ResourceType.WoodenHammer };
            return new ResourceType[0];
        }

        public void Select() { if (selectionIndicator) selectionIndicator.SetActive(true); }
        public void Deselect() { if (selectionIndicator) selectionIndicator.SetActive(false); }
        public void SetFacingDirection(Vector3 t) { _visuals.FaceTarget(t); }
        [ClientRpc] public void PlayAttackVisualsClientRpc(Vector3 t) { _visuals.FaceTarget(t); _visuals.TriggerAttackAnimation(); }

        public UnitSaveData GetSaveData() { return new UnitSaveData(); }
        public void LoadFromData(UnitSaveData d) { }
        public void RestoreActions() { }
    }
}