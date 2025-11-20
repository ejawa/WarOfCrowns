using UnityEngine;

// Этот атрибут позволяет создавать файлы этого типа через меню Assets -> Create
[CreateAssetMenu(fileName = "NewResource", menuName = "War of Crowns/Resource Data")]
public class ResourceData : ScriptableObject
{
    [Header("Display Info")]
    public string displayName;
    public Sprite icon;

    [Header("Gameplay Stats")]
    [Tooltip("Является ли этот ресурс едой, которую можно конвертировать в сытость?")]
    public bool isSatietyProvider = false;
    [Tooltip("Сколько сытости дает 1 единица этого ресурса.")]
    public int satietyValue = 0;

    // Позже сюда можно будет добавить больше всего:
    // public bool isWeapon;
    // public int damage;
    // public bool isArmor;
    // public int defense;
}