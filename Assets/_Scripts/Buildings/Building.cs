using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;
using WarOfCrowns.Core;
using WarOfCrowns.Data;

namespace WarOfCrowns.Buildings
{
    [System.Serializable]
    public class BuildingCost
    {
        public ResourceType resourceType;
        public int amount;
    }

    public class Building : NetworkBehaviour
    {
        [Header("Информация")]
        public string buildingName;
        public Sprite buildingIcon;
        [TextArea] public string description;

        [Header("Стоимость")]
        public List<BuildingCost> costs;

        [Header("Экономика")]
        public int populationBonus = 0;

        // СЕТЕВАЯ ПЕРЕМЕННАЯ ВЛАДЕЛЬЦА
        // По умолчанию -1 (Ничей), чтобы избежать ложного срабатывания при старте
        public NetworkVariable<int> ownerKingdomID = new NetworkVariable<int>(-1);

        [HideInInspector] public Kingdom OwningKingdom;
        public string uniqueID;

        private bool _popApplied = false; // Флаг: применен ли бонус к населению

        private void Awake()
        {
            if (string.IsNullOrEmpty(uniqueID))
                uniqueID = System.Guid.NewGuid().ToString();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // 1. Пытаемся зарегистрироваться сразу при появлении
            CheckPopulationRegistration();

            // 2. Подписываемся на изменение владельца
            ownerKingdomID.OnValueChanged += (o, n) => CheckPopulationRegistration();
        }

        // --- ГЛАВНЫЙ МЕТОД ПРОВЕРКИ (ВЫЗЫВАЕТСЯ ИЗ Kingdom.cs) ---
        public void CheckPopulationRegistration()
        {
            // Обновляем локальную ссылку на объект Королевства (для удобства)
            UpdateKingdomReference();

            // Если игра еще не загрузилась или менеджеры не готовы - выходим
            if (Kingdom.PlayerKingdom == null || PopulationManager.Instance == null) return;

            // Если ID все еще -1 (данные не пришли), ничего не делаем
            if (ownerKingdomID.Value == -1) return;

            // Стройплощадки не дают бонусов к населению
            if (GetComponent<ConstructionSite>() != null) return;

            // ЛОГИКА:
            // Если это МОЕ здание (ID совпадает с моим локальным KingdomID)
            if (ownerKingdomID.Value == Kingdom.PlayerKingdom.kingdomID)
            {
                // Если бонус еще не применен -> Применяем
                if (!_popApplied && populationBonus > 0)
                {
                    PopulationManager.Instance.AddPopulationCap(populationBonus);
                    _popApplied = true;
                }
            }
            else
            {
                // Если это ЧУЖОЕ здание (или стало чужим), но бонус был применен -> Убираем
                if (_popApplied && populationBonus > 0)
                {
                    PopulationManager.Instance.AddPopulationCap(-populationBonus);
                    _popApplied = false;
                }
            }
        }

        private void UpdateKingdomReference()
        {
            foreach (var k in FindObjectsOfType<Kingdom>())
            {
                if (k.kingdomID == ownerKingdomID.Value)
                {
                    OwningKingdom = k;
                    break;
                }
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            // Если объект исчезает из сети, обязательно убираем бонус, если он был
            if (_popApplied && PopulationManager.Instance != null)
            {
                PopulationManager.Instance.AddPopulationCap(-populationBonus);
                _popApplied = false;
            }
        }

        // Вызывается сервером при создании здания
        public void SetOwnerID(int id)
        {
            if (IsServer) ownerKingdomID.Value = id;
        }

        // --- СИСТЕМА СОХРАНЕНИЙ ---
        public BuildingSaveData GetSaveData()
        {
            BuildingSaveData data = new BuildingSaveData();
            data.uniqueID = this.uniqueID;
            data.prefabName = gameObject.name.Replace("(Clone)", "").Trim();
            data.ownerID = ownerKingdomID.Value; // Сохраняем владельца
            data.posX = transform.position.x;
            data.posY = transform.position.y;
            data.posZ = transform.position.z;

            // Сохранение специфики (стройка, работа, кузница)
            if (TryGetComponent<ConstructionSite>(out var site))
            {
                data.isConstructionSite = true;
                data.constructionProgress = site.GetProgress();
            }
            else if (TryGetComponent<JobBuilding>(out var job))
            {
                data.isConstructionSite = false;
                data.productionProgress = job.GetProgress();
            }
            else if (TryGetComponent<Smithy>(out var smithy))
            {
                data.isConstructionSite = false;
                data.activeRecipeIndex = smithy.GetCurrentRecipeIndex();
                data.craftingTimer = smithy.GetCurrentTimer();
            }

            return data;
        }

        public void LoadFromData(BuildingSaveData data)
        {
            this.uniqueID = data.uniqueID;

            // Восстановление специфики
            if (data.isConstructionSite)
            {
                if (TryGetComponent<ConstructionSite>(out var site))
                    site.SetProgress(data.constructionProgress);
            }
            else
            {
                if (TryGetComponent<JobBuilding>(out var job))
                    job.SetProgress(data.productionProgress);
            }

            if (TryGetComponent<Smithy>(out var smithy))
            {
                smithy.LoadState(data.activeRecipeIndex, data.craftingTimer);
            }
        }
    }
}