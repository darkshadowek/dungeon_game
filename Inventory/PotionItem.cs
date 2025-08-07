using UnityEngine;

[CreateAssetMenu(fileName = "NewPotion", menuName = "Items/Potion")]
public class PotionItem : Item
{
    public int healAmount;
    public override void Use(GameObject user)
    {
        Debug.Log($"Drinking potion {itemName}, healing {healAmount} HP!");
    }
}
