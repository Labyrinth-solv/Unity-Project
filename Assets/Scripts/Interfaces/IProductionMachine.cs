public interface IProductionMachine
{
    void SetOutputItem(ItemSO item);
    ItemSO GetOutputItem();
    float GetProductionProgressNormalized();
    float GetProductionElapsedTime();
    float GetProductionDuration();
}
