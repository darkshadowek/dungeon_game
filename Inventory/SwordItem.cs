using UnityEngine;

[CreateAssetMenu(fileName = "NewSword", menuName = "Items/Sword")]
public class SwordItem : Item
{
    public int damage;
    public override void Use(GameObject user)
    {
        Debug.Log($"Swinging sword {itemName} for {damage} dmg!");
    }
}
