using System.Collections.Generic;
using UnityEngine;
public class ChestInventory : MonoBehaviour
{
    public List<InventoryUI> SlotsInChest;
    [SerializeField] private GameObject slotInventory;

    private void Start()
    {
        SlotsInChest.Clear();
        for (int i = 0; i < 9; i++)
        {
            var slot = slotInventory.transform.GetChild(i).GetComponent<InventoryUI>();
            SlotsInChest.Add(slot);
        }
    }

    public void SynchronizeItemsWithChest(Chest chest)
    {
        for (int i = 0; i < 9; i++)
        {
            Item item = chest.items[i];
            if (item != null)
            {
                SlotsInChest[i].item = item;
                SlotsInChest[i].icon.sprite = item.icon;
                SlotsInChest[i].icon.color = Color.white;
                SlotsInChest[i].quantity = 1; // tu mo¿esz ustawiæ iloœæ jeœli masz w Chest
            }
            else
            {
                SlotsInChest[i].ClearSlot();
            }
        }
    }

    public void SynchronizeChestWithItems(Chest chest)
    {
        for (int i = 0; i < 9; i++)
        {
            chest.items[i] = SlotsInChest[i].item;
        }
    }
}

