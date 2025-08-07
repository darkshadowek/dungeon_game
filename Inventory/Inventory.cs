using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public List<Item> items;
    public GameObject slotPrefab;
    public Transform slotParent;
    public int maxSlots = 20;

    void Awake()
    {
        for (int i = 0; i < maxSlots; i++)
        {
            GameObject emptySlot = Instantiate(slotPrefab, slotParent);
            emptySlot.GetComponent<InventoryUI>().ClearSlot();
        }

        foreach (var item in items)
        {
            AddItemToInventory(item);
        }
    }

    public void AddItemToInventory(Item newItem, int count = 1)
    {
        if (newItem.isStackable)
        {
            foreach (Transform child in slotParent)
            {
                InventoryUI slot = child.GetComponent<InventoryUI>();
                if (slot.item == newItem && slot.quantity < slot.maxStack)
                {
                    slot.AddItem(count);
                    return;
                }
            }
        }

        foreach (Transform child in slotParent)
        {
            InventoryUI slot = child.GetComponent<InventoryUI>();
            if (slot.item == null)
            {
                slot.SetItem(newItem, count);
                return;
            }
        }

        Debug.Log("Inventory full!");
    }

}