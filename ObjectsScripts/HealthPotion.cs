using UnityEngine;

public class HealthPotion : ObjectItem
{
    public PotionItem healthPotionData;
    public override Item GetItemData()
    {
        return healthPotionData;
    }
    
}
