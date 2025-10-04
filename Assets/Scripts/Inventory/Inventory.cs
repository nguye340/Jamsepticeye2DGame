// Create this in Assets/Scripts/Inventory/Inventory.cs
using UnityEngine;
using System.Collections.Generic;

public class Inventory : MonoBehaviour
{
    private Dictionary<string, int> items = new Dictionary<string, int>();

    public void AddItem(string itemId, int count = 1)
    {
        if (items.ContainsKey(itemId))
            items[itemId] += count;
        else
            items[itemId] = count;
    }

    public int TotalCount()
    {
        int total = 0;
        foreach (var item in items)
        {
            total += item.Value;
        }
        return total;
    }

    // Add other inventory methods as needed
}