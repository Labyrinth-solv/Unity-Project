public interface IItemHolder
{
    bool CanAcceptItem();
    bool TryAddItem(Item item);
    Item TakeItem();
}