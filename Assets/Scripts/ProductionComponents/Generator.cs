using UnityEngine;

public class Generator : MonoBehaviour, ITickable, IProducer, IProductionMachine
{
    [SerializeField] private float produceInterval=1.5f;
    [SerializeField] private ItemSO outputItem;
    
    private float timer;
    private IItemHolder output;
    private void Awake()
    {
        output=GetComponent<IItemHolder>();
    }

    private void OnEnable()
    {
        TickManager.Register(this);
    }

    private void OnDisable()
    {
        TickManager.Unregister(this);
    }

    public void Tick(float deltaTime)
    {
        timer+=deltaTime;
        if (timer >= produceInterval)
        {
            timer=0f;
            if (CanProduce())
            {
                Produce();
            }
        }
    }

    public bool CanProduce()
    {
        return outputItem != null && output != null && output.CanAcceptItem();
    }

    public void Produce()
    {
        if (output != null && output.TryAddItem(outputItem))
        {
            ProductionRateTracker.RecordProduced(outputItem);
        }
    }

    public void SetOutputItem(ItemSO newItem)
    {
        outputItem = newItem;
    }

    public ItemSO GetOutputItem()
    {
        return outputItem;
    }
}
