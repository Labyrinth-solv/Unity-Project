using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Generator : MonoBehaviour, ITickable, IProducer
{
    [SerializeField] private float produceInterval=1.5f;
    [SerializeField] private Item outputItem;
    
    private float timer;
    private IItemHolder output;
    private void Awake()
    {
        output=GetComponent<IItemHolder>();
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
        return output != null && output.CanAcceptItem();
    }

    public void Produce()
    {
        Item newItem=new Item()
        {
            id=outputItem.id,
            amount=1
        };
        output.TryAddItem(newItem);
    }
}
