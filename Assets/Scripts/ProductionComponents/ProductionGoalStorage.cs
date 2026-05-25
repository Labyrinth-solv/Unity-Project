using UnityEngine;

public class ProductionGoalStorage : MonoBehaviour, IItemEndpoint
{
    [SerializeField] private GameObject deliveryEffectPrefab;

    public bool CanReceive() => true; // Always accept items

    public bool TryReceive(ItemSO item)
    {
        if (item == null) return false;

        // Record the delivery for win condition checking
        ProductionRateTracker.RecordDelivery(item);

        if (deliveryEffectPrefab != null)
        {
            Instantiate(deliveryEffectPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
        }

        return true;
    }

    // Goal storage is a sink, it doesn't provide anything
    public bool CanProvide() => false;
    public ItemSO Peek() => null;
    public void Consume() { }
}
