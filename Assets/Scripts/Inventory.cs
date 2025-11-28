using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public Dictionary<BlockType, int> items = new();
    InventoryUI invenUI;

    void Start()
    {
        invenUI = FindObjectOfType<InventoryUI>();
    }

    public void Add(BlockType type, int count = 1)
    {
        if (!items.ContainsKey(type)) items[type] = 0;
        items[type] += count;
        Debug.Log($"[Inventory] +{count} {type} (รั{items[type]}");
        invenUI.UpdateInventory(this);
    }

    public bool Consume(BlockType type, int count = 1)
    { 
        if (!items.TryGetValue(type, out var have) || have < count) return false;
        items[type] = have - count;
        Debug.Log($"[Inventory] -{count} {type} (รั{items[type]}");
        if (items[type] == 0)
        {
            items.Remove(type);
            invenUI.selectedIndex = -1;
            invenUI.ResetSelection();
        }

        invenUI.UpdateInventory(this);
        return true;
    }
}
