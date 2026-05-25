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
            return false;
        }

        if (!GridBuildingSystem.Instance.TryGetPlacedObjectAtGridPosition(outputGridPosition, out PlacedObject nextPlacedObject))
        {
            return false;
        }

        if (nextPlacedObject == placedObject) return false;

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
        
        GameObject wrapper = new GameObject("ItemWrapper");
        wrapper.transform.SetParent(parent, false);
        itemVisual = wrapper;

        GameObject actualVisual;
        if (item.visualPrefab != null)
        {
            actualVisual = Instantiate(item.visualPrefab, wrapper.transform);
        }
        else
        {
            actualVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            actualVisual.transform.SetParent(wrapper.transform, false);
            actualVisual.transform.localScale = fallbackVisualScale;
            Renderer r = actualVisual.GetComponent<Renderer>();
            if (r != null) r.material.color = fallbackVisualColor;
            Destroy(actualVisual.GetComponent<Collider>());
        }

        CenterAndScaleVisual(actualVisual);
    }

    private void CenterAndScaleVisual(GameObject visual)
    {
        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        bool first = true;
        foreach (var r in renderers)
        {
            if (first) { bounds = r.bounds; first = false; }
            else bounds.Encapsulate(r.bounds);
        }

        Vector3 localCenter = visual.transform.InverseTransformPoint(bounds.center);
        visual.transform.localPosition = -localCenter;
        
        float maxDim = Mathf.Max(bounds.size.x, bounds.size.z);
        if (maxDim > 0.8f)
        {
            float scale = 0.6f / maxDim;
            visual.transform.localScale *= scale;
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

        itemVisual.transform.localPosition = Vector3.Lerp(itemVisualStartOffset, itemVisualEndOffset, progress);
        itemVisual.transform.localRotation = Quaternion.identity;
    }
}
