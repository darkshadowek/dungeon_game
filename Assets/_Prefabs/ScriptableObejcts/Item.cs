using UnityEngine;

public abstract class Item : ScriptableObject
{
    [Header("Item")]
    public bool isStackable = true;
    public string itemName;
    public Sprite icon;
    public ItemType itemType;
    public GameObject worldPrefab;
    public abstract void Use();
}

public enum ItemType
{
    Sword,
    Potion,
    Armor,
    AlhemicIngredient,
    CraftIngredient,
    KeyItem
}
