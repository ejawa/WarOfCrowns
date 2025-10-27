using UnityEngine;
using WarOfCrowns.Core;
using WarOfCrowns.Core.Items;

namespace WarOfCrowns.Buildings
{
    public class ConverterBuilding : MonoBehaviour
    {
        [Header("Conversion Settings")]
        [Tooltip("Какой предмет требуется для производства.")]
        [SerializeField] private ItemType _requiredItem;

        [Tooltip("Сколько требуется.")]
        [SerializeField][Range(1, 10)] private int _requiredAmount = 1;

        [Tooltip("Какой предмет производится.")]
        [SerializeField] private ItemType _producedItem;

        [Tooltip("Сколько производится за раз.")]
        [SerializeField][Range(1, 10)] private int _producedAmount = 1;

        [Tooltip("Время в секундах на одну конвертацию.")]
        [SerializeField] private float _conversionTime = 5f;

        private float _timer;

        private void Start()
        {
            _timer = _conversionTime;
        }

        private void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer <= 0)
            {
                _timer = _conversionTime;
                TryConvert();
            }
        }

        private void TryConvert()
        {
            if (ResourceManager.Instance.GetItemAmount(_requiredItem) >= _requiredAmount)
            {
                // Забираем сырье
                ResourceManager.Instance.AddItem(_requiredItem, -_requiredAmount);
                // Добавляем готовый продукт
                ResourceManager.Instance.AddItem(_producedItem, _producedAmount);
                Debug.Log($"'{gameObject.name}' converted {_requiredAmount} {_requiredItem} into {_producedAmount} {_producedItem}");
            }
        }
    }
}