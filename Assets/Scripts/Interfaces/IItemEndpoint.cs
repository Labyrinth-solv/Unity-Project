public interface IItemEndpoint
{
    //Phía nhận Receive
    bool CanReceive();
    bool TryReceive(Item item); //Nhận item

    //Phía cung cấp Provide
    bool CanProvide();
    Item Peek(); //Xem item
    void Consume(); //Xóa item sau khi object đã nhận
}