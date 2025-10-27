using UnityEngine;
using WarOfCrowns.Core;
using WarOfCrowns.Core.Items;

namespace WarOfCrowns.Buildings
{
    public class ConverterBuilding : MonoBehaviour
    {
        [Header("Conversion Settings")]
        [Tooltip("����� ������� ��������� ��� ������������.")]
        [SerializeField] private ItemType _requiredItem;

        [Tooltip("������� ���������.")]
        [SerializeField][Range(1, 10)] private int _requiredAmount = 1;

        [Tooltip("����� ������� ������������.")]
        [SerializeField] private ItemType _producedItem;

        [Tooltip("������� ������������ �� ���.")]
        [SerializeField][Range(1, 10)] private int _producedAmount = 1;

        [Tooltip("����� � �������� �� ���� �����������.")]
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
                // �������� �����
                ResourceManager.Instance.AddItem(_requiredItem, -_requiredAmount);
                // ��������� ������� �������
                ResourceManager.Instance.AddItem(_producedItem, _producedAmount);
                Debug.Log($"'{gameObject.name}' converted {_requiredAmount} {_requiredItem} into {_producedAmount} {_producedItem}");
            }
        }
    }
}