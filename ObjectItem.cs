using UnityEngine;

public abstract class ObjectItem : MonoBehaviour
{
    public abstract Item GetItemData();

    public void PickUp()
    {
        Inventory inventory = GameObject.FindFirstObjectByType<Inventory>();
        inventory.AddItemToInventory(GetItemData(), 1);
        Destroy(gameObject);
    }
}
