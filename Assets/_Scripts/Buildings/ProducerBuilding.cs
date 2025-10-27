using UnityEngine;
using WarOfCrowns.Core;
using WarOfCrowns.Core.Items;

namespace WarOfCrowns.Buildings
{
    public class ProducerBuilding : MonoBehaviour
    {
        [Header("Production Settings")]
        [Tooltip("Какой предмет производит это здание.")]
        [SerializeField] private ItemType _producedItem;

        [Tooltip("Сколько производит за один раз.")]
        [SerializeField][Range(1, 10)] private int _producedAmount = 1;

        [Tooltip("Время в секундах между производством.")]
        [SerializeField] private float _productionTime = 10f;

        private float _timer;

        private void Start()
        {
            _timer = _productionTime;
        }

        private void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer <= 0)
            {
                _timer = _productionTime;
                ResourceManager.Instance.AddItem(_producedItem, _producedAmount);
                Debug.Log($"'{gameObject.name}' produced {_producedAmount} {_producedItem}");
            }
        }
    }
}