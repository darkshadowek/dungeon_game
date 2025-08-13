using UnityEngine;

[CreateAssetMenu(fileName = "NewSword", menuName = "Items/Sword")]
public class SwordItem : Item
{
    [Header("Sword")]
    public int damage;
    public override void Use()
    {
        Debug.Log($"Swinging sword {itemName} for {damage} dmg!");
    }
}
