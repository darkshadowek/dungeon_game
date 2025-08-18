using UnityEngine;

public class AlchemicObject : ObjectItem
{
    public AlchemicIngredientItem swordData;

    public override Item GetItemData()
    {
        return swordData;
    }
}
