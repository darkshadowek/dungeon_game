using UnityEngine;

[CreateAssetMenu(fileName = "NewPotion", menuName = "Items/Potion")]
public class PotionItem : Item
{
    [Header("Potion")]
    public int amount;
    public PotionsType potionType;
    public override void Use()
    {
        PlayerBody.PlayerInstance.PotionUse(amount, potionType);
    }
}
public enum PotionsType
{
    Healthpotion,
    Manapotion,
}
