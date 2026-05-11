public interface IItemHolder
{
    bool CanAcceptItem();
    bool TryAddItem(ItemSO item);
    ItemSO TakeItem();
}
