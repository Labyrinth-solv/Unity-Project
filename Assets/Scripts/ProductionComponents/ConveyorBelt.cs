using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

public class ConveyorBelt : MonoBehaviour, IItemTransporter, IItemEndpoint
{
    [SerializeField] private float speed = 2f;
    private Item item;
    private float progress = 0f;
    private Vector3 direction;

    // Transport logic riêng
    public bool HasItem() => item != null;
    public Item PeekItem() => item;
    public void RemoveItem()
    {
        item = null;
        progress = 0f;
    }
    public float GetProgress() => progress;
    public void SetProgress(float value)
    {
        progress = Mathf.Clamp01(value);
    }
    public Vector3 GetDirection() => direction;

    // Endpoint
    public bool CanReceive() => item == null;
    public bool TryReceive(Item item)
    {
        if (this.item != null) return false;
        this.item = item;
        return true;
    }

    public bool CanProvide() => item != null;
    public Item Peek() => item;
    public void Consume() => item = null;
}