using UnityEngine;

public class ItemBuffer : MonoBehaviour, IItemHolder, IItemEndpoint
{
    [SerializeField] private ItemSO item;

    // IItemHolder (internal logic)
    public bool CanAcceptItem() => item == null;
    public bool TryAddItem(ItemSO newItem)
    {
        if (item != null) return false;
        item = newItem;
        return true;
    }
    public ItemSO TakeItem()
    {
        var temp = item;
        item = null;
        return temp;
    }

    // IItemEndpoint (external communication)
    public bool CanReceive() => CanAcceptItem();
    public bool TryReceive(ItemSO item) => TryAddItem(item);

    public bool CanProvide() => this.item != null;
    public ItemSO Peek() => item;
    public void Consume() => item = null;
}
