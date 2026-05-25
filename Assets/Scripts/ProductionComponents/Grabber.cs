using UnityEngine;

public class Grabber : MonoBehaviour, IItemEndpoint, ITickable
{
    [Min(0f)]
    [SerializeField] private float grabSpeed = 2f; // tốc độ grab item
    [SerializeField] private PlacedObjectTypeSO.Dir defaultDirection = PlacedObjectTypeSO.Dir.Right;
    [SerializeField] private Transform itemVisualParent;
    [SerializeField] private Vector3 itemVisualStartOffset = new Vector3(-0.45f, 0.3f, 0f);
    [SerializeField] private Vector3 itemVisualEndOffset = new Vector3(0.45f, 0.3f, 0f);
    [SerializeField] private Vector3 fallbackVisualScale = new Vector3(0.25f, 0.25f, 0.25f);
    [SerializeField] private Color fallbackVisualColor = new Color(0.2f, 0.8f, 1f, 1f);

    private ItemSO item;
    private GameObject itemVisual;
    private float grabProgress = 0f; // 0 = chưa grab, 1 = grab xong
    private bool isGrabbing = false;
    private IItemEndpoint sourceEndpoint; // endpoint cung cấp item (machine/processor)
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
        if (item == null)
        {
            if (!isGrabbing)
            {
                // Start moving towards source
                isGrabbing = true;
            }

            // Progress towards 0 (Source)
            grabProgress = Mathf.Max(0f, grabProgress - grabSpeed * deltaTime);
            UpdateItemVisualPosition();

            if (grabProgress <= 0f)
            {
                TryPickUpItem();
            }
        }
        else
        {
            // Have item, move towards 1 (Destination)
            grabProgress = Mathf.Min(1f, grabProgress + grabSpeed * deltaTime);
            UpdateItemVisualPosition();

            if (grabProgress >= 1f)
            {
                TryTransferItemToNextEndpoint();
            }
        }
    }

    private void TryPickUpItem()
    {
        if (!TryGetSourceEndpoint(out IItemEndpoint source)) return;
        if (!source.CanProvide()) return;

        item = source.Peek();
        source.Consume();
        CreateItemVisual();
    }

    #region Grabbing Logic
    private void TryStartGrabbing()
    {
        // Tìm source endpoint ở phía input
        if (!TryGetSourceEndpoint(out IItemEndpoint source)) return;
        if (!source.CanProvide()) return;

        sourceEndpoint = source;
        isGrabbing = true;
        grabProgress = 0f;
    }

    private void GrabItemProgress(float deltaTime)
    {
        if (grabSpeed <= 0f) return;

        grabProgress = Mathf.Min(1f, grabProgress + grabSpeed * deltaTime);

        // Khi grab xong, lấy item từ source
        if (grabProgress >= 1f)
        {
            if (sourceEndpoint != null && sourceEndpoint.CanProvide())
            {
                item = sourceEndpoint.Peek();
                sourceEndpoint.Consume();
                isGrabbing = false;
                grabProgress = 0f;
                CreateItemVisual();
                UpdateItemVisualPosition();
            }
        }
    }

    private bool TryGetSourceEndpoint(out IItemEndpoint endpoint)
    {
        endpoint = null;
        
        // Lấy input grid position của grabber
        if (!placedObject.TryGetInputGridPosition(out Vector2Int inputGridPos))
            return false;

        // Tìm building ở vị trí input
        if (!GridBuildingSystem.Instance.TryGetPlacedObjectAtGridPosition(inputGridPos, out PlacedObject sourceObject))
            return false;

        // Kiểm tra xem source có implement IItemEndpoint không
        endpoint = sourceObject.GetComponent<IItemEndpoint>();
        return endpoint != null;
    }

    private bool TryGetNextEndpoint(out IItemEndpoint endpoint)
    {
        endpoint = null;

        // Lấy output grid position của grabber
        if (!placedObject.TryGetOutputGridPosition(out Vector2Int outputGridPos))
            return false;

        // Tìm building ở vị trí output
        if (!GridBuildingSystem.Instance.TryGetPlacedObjectAtGridPosition(outputGridPos, out PlacedObject nextObject))
            return false;

        // Kiểm tra xem destination có implement IItemEndpoint không
        endpoint = nextObject.GetComponent<IItemEndpoint>();
        return endpoint != null;
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
    #endregion

    #region Helper Methods
    private void RemoveItem()
    {
        item = null;
        grabProgress = 0f;
        isGrabbing = false;
        DestroyItemVisual();
    }
    #endregion

    #region IItemEndpoint Implementation
    // Phía nhận (input từ machines)
    public bool CanReceive() => item == null && !isGrabbing;
    public bool TryReceive(ItemSO receivedItem)
    {
        if (receivedItem == null || item != null || isGrabbing) return false;

        item = receivedItem;
        grabProgress = 0f;
        CreateItemVisual();
        UpdateItemVisualPosition();
        return true;
    }

    // Phía cung cấp (output đến conveyor)
    public bool CanProvide() => item != null && grabProgress >= 1f;
    public ItemSO Peek() => item;
    public void Consume()
    {
        item = null;
        grabProgress = 0f;
        isGrabbing = false;
        DestroyItemVisual();
    }
    #endregion

    #region Visual Updates
    private void CreateItemVisual()
    {
        if (itemVisualParent == null) return;
        
        if (itemVisual != null)
            Destroy(itemVisual);

        if (item == null || item.visualPrefab == null)
        {
            CreateFallbackVisual();
        }
        else
        {
            itemVisual = Instantiate(item.visualPrefab, itemVisualParent);
            itemVisual.transform.localPosition = itemVisualStartOffset;
            itemVisual.transform.localRotation = Quaternion.identity;
        }
    }

    private void CreateFallbackVisual()
    {
        itemVisual = new GameObject("ItemVisualFallback");
        itemVisual.transform.SetParent(itemVisualParent);
        itemVisual.transform.localPosition = itemVisualStartOffset;
        itemVisual.transform.localScale = fallbackVisualScale;

        MeshFilter meshFilter = itemVisual.AddComponent<MeshFilter>();
        MeshCollider meshCollider = itemVisual.AddComponent<MeshCollider>();
        MeshRenderer meshRenderer = itemVisual.AddComponent<MeshRenderer>();

        meshFilter.mesh = new Mesh();
        meshFilter.mesh.vertices = new Vector3[] {
            Vector3.zero, Vector3.right, Vector3.up, Vector3.one
        };
        meshFilter.mesh.triangles = new int[] { 0, 1, 2, 1, 3, 2 };
        meshCollider.convex = true;

        Material fallbackMaterial = new Material(Shader.Find("Standard"));
        fallbackMaterial.color = fallbackVisualColor;
        meshRenderer.material = fallbackMaterial;
    }

    private void DestroyItemVisual()
    {
        if (itemVisual != null)
            Destroy(itemVisual);
    }

    private void UpdateItemVisualPosition()
    {
        if (itemVisual == null) return;

        Vector3 newPosition = Vector3.Lerp(itemVisualStartOffset, itemVisualEndOffset, grabProgress);
        itemVisual.transform.localPosition = newPosition;
    }
    #endregion

    private PlacedObjectTypeSO.Dir GetCurrentDirection()
    {
        // Nếu PlacedObject có direction info thì dùng, không thì dùng default
        PlacedObject placedObj = GetComponent<PlacedObject>();
        if (placedObj != null && placedObj.IsInitialized())
        {
            return placedObj.GetDir();
        }
        return defaultDirection;
    }
}
