using UnityEngine;

public class Generator : MonoBehaviour, ITickable, IProducer, IProductionMachine
{
    [SerializeField] private string machineName = "Generator";
    public string GetMachineName() => machineName;
    [SerializeField] private float produceInterval = 1.5f;
    [SerializeField] private ItemSO outputItem;

    private float timer;
    private IItemHolder output;

    private void Awake()
    {
        output = GetComponent<IItemHolder>();
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
        if (outputItem == null) return;

        timer += deltaTime;
        if (timer >= produceInterval)
        {
            if (CanProduce())
            {
                Produce();
                timer = 0f;
            }
            else
            {
                // Hold at max progress until buffer clears
                timer = produceInterval;
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

    public float GetProductionProgressNormalized()
    {
        if (outputItem == null || produceInterval <= 0f)
        {
            return 0f;
        }

        return Mathf.Clamp01(timer / produceInterval);
    }

    public float GetProductionElapsedTime()
    {
        return outputItem == null ? 0f : timer;
    }

    public float GetProductionDuration()
    {
        return Mathf.Max(0f, produceInterval);
    }
}
