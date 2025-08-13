using UnityEngine;
using System.Collections.Generic;

public class Chest : MonoBehaviour
{
    public List<Item> items;

    private void Awake()
    {
        items = new List<Item>();
        for (int i = 0; i < 9; i++)
        {
            items.Add(null);
        }
    }
}
