using UnityEngine;

public class ConveyorBelt : MonoBehaviour, IItemTransporter, IItemEndpoint, ITickable
{
    [Min(0f)]
    [SerializeField] private float speed = 2f;
    [SerializeField] private PlacedObjectTypeSO.Dir defaultDirection = PlacedObjectTypeSO.Dir.Right;
    [SerializeField] private Transform itemVisualParent;
    [SerializeField] private Vector3 itemVisualStartOffset = new Vector3(-0.45f, 0.3f, 0f);
    [SerializeField] private Vector3 itemVisualEndOffset = new Vector3(0.45f, 0.3f, 0f);
    [SerializeField] private Vector3 fallbackVisualScale = new Vector3(0.25f, 0.25f, 0.25f);
    [SerializeField] private Color fallbackVisualColor = new Color(1f, 0.8f, 0.2f, 1f);

    private ItemSO item;
    private GameObject itemVisual;
    private float progress = 0f;
    private PlacedObject placedObject;

    private void Awake()
    {
        placedObject = GetComponent<PlacedObject>();
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
        if (item == null) return;

        MoveItemInsideConveyor(deltaTime);

        if (progress >= 1f)
        {
            TryTransferItemToNextEndpoint();
        }
    }

    // Transport state used by other components that need to inspect the belt.
    public bool HasItem() => item != null;
    public ItemSO PeekItem() => item;
    public void RemoveItem()
    {
        item = null;
        progress = 0f;
        DestroyItemVisual();
    }
    public float GetProgress() => progress;
    public void SetProgress(float value)
    {
        progress = Mathf.Clamp01(value);
        UpdateItemVisualPosition();
    }
    public Vector3 GetDirection()
    {
        Vector2Int direction = PlacedObjectTypeSO.GetDirectionVector(GetCurrentDirection());
        return new Vector3(direction.x, 0f, direction.y);
    }

    // Input side. A belt can receive only one item at a time.
    public bool CanReceive() => item == null;
    public bool TryReceive(ItemSO item)
    {
        if (item == null || this.item != null) return false;

        this.item = item;
        progress = 0f;
        CreateItemVisual();
        UpdateItemVisualPosition();
        return true;
    }

    // Output side. The item is considered available only at the end of the belt.
    public bool CanProvide() => item != null && progress >= 1f;
    public ItemSO Peek() => item;
    public void Consume() => RemoveItem();

    private void MoveItemInsideConveyor(float deltaTime)
    {
        if (speed <= 0f) return;

        progress = Mathf.Min(1f, progress + speed * deltaTime);
        UpdateItemVisualPosition();
    }

    private void TryTransferItemToNextEndpoint()
    {
        if (!TryGetNextEndpoint(out IItemEndpoint nextEndpoint)) return;
        if (!nextEndpoint.CanReceive()) return;

        ItemSO itemToTransfer = item;
        if (nextEndpoint.TryReceive(itemToTransfer))
        {
            RemoveItem();
        }
    }

    private bool TryGetNextEndpoint(out IItemEndpoint nextEndpoint)
    {
        nextEndpoint = null;

        if (GridBuildingSystem.Instance == null || placedObject == null) return false;
        if (!placedObject.TryGetOutputGridPosition(out Vector2Int outputGridPosition))
        {
            // A belt can exist as a prefab/scene object before the grid building
            // system initializes its PlacedObject data. In that state it can
            // still animate a demo item, but it cannot know the next grid cell.
            return false;
        }

        if (!GridBuildingSystem.Instance.TryGetPlacedObjectAtGridPosition(outputGridPosition, out PlacedObject nextPlacedObject))
        {
            return false;
        }

        if (nextPlacedObject == placedObject) return false;

        // Components are discovered through their interface, so the conveyor
        // can connect to belts, buffers, storages, or future machines.
        MonoBehaviour[] behaviours = nextPlacedObject.GetComponentsInChildren<MonoBehaviour>();
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (ReferenceEquals(behaviour, this)) continue;

            if (behaviour is IItemEndpoint endpoint)
            {
                nextEndpoint = endpoint;
                return true;
            }
        }

        return false;
    }

    private PlacedObjectTypeSO.Dir GetCurrentDirection()
    {
        if (placedObject != null && placedObject.IsInitialized())
        {
            return placedObject.GetDir();
        }

        return defaultDirection;
    }

    private void CreateItemVisual()
    {
        DestroyItemVisual();
        if (item == null) return;

        Transform parent = itemVisualParent != null ? itemVisualParent : transform;
        if (item.visualPrefab != null)
        {
            itemVisual = Instantiate(item.visualPrefab, parent);
            return;
        }

        // Fallback for early testing: ItemSO can move on the belt even before
        // the item has its final art prefab assigned in the Inspector.
        itemVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        itemVisual.name = !string.IsNullOrEmpty(item.displayName) ? item.displayName : "Demo Item";
        itemVisual.transform.SetParent(parent, false);
        itemVisual.transform.localScale = fallbackVisualScale;

        Collider itemCollider = itemVisual.GetComponent<Collider>();
        if (itemCollider != null)
        {
            Destroy(itemCollider);
        }

        Renderer renderer = itemVisual.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = fallbackVisualColor;
        }
    }

    private void DestroyItemVisual()
    {
        if (itemVisual == null) return;

        Destroy(itemVisual);
        itemVisual = null;
    }

    private void UpdateItemVisualPosition()
    {
        if (itemVisual == null) return;

        // The prefab's zero-rotation direction is Right, so the default local
        // movement path is left-to-right on local X. The placed object's
        // rotation turns this same local path into Down/Left/Up as needed.
        itemVisual.transform.localPosition = Vector3.Lerp(itemVisualStartOffset, itemVisualEndOffset, progress);
        itemVisual.transform.localRotation = Quaternion.identity;
    }
}
