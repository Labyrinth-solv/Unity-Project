using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemBuffer : MonoBehaviour, IItemHolder, IItemEndpoint
{
    private Item item;

    // IItemHolder (internal logic)
    public bool CanAcceptItem() => item == null;
    public bool TryAddItem(Item newItem)
    {
        if (item != null) return false;
        item = newItem;
        return true;
    }
    public Item TakeItem()
    {
        var temp = item;
        item = null;
        return temp;
    }

    // IItemEndpoint (external communication)
    public bool CanReceive() => CanAcceptItem();
    public bool TryReceive(Item item) => TryAddItem(item);

    public bool CanProvide() => this.item != null;
    public Item Peek() => item;
    public void Consume() => item = null;
}