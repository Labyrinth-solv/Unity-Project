public interface IItemEndpoint
{
    //Phía nhận Receive
    bool CanReceive();
    bool TryReceive(ItemSO item); //Nhận item

    //Phía cung cấp Provide
    bool CanProvide();
    ItemSO Peek(); //Xem item
    void Consume(); //Xóa item sau khi object đã nhận
}
