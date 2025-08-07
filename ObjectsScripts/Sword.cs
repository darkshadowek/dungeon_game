using UnityEngine;

public class Sword : ObjectItem
{
    public SwordItem swordData;
    public bool holdingWeapon;
    public override Item GetItemData()
    {
        return swordData;
    }
    public void changeLayer()
    {
        if (holdingWeapon)
        {
            gameObject.layer = 7;
        }
        else
        {
            gameObject.layer = 0;
        }
    }
}

