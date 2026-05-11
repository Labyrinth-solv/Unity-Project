using UnityEngine;

public class ConveyorBeltDemoSpawner : MonoBehaviour
{
    [SerializeField] private ConveyorBelt conveyorBelt;
    [SerializeField] private ItemSO demoItem;
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private float spawnDelay = 0.2f;
    [SerializeField] private bool spawnOnlyWithoutInput = true;
    [SerializeField] private bool allowKeyboardSpawn = true;
    [SerializeField] private KeyCode spawnKey = KeyCode.T;

    private PlacedObject placedObject;
    private ItemSO runtimeFallbackItem;

    private void Awake()
    {
        if (conveyorBelt == null)
        {
            conveyorBelt = GetComponent<ConveyorBelt>();
        }

        placedObject = GetComponent<PlacedObject>();
    }

    private void Start()
    {
        if (!spawnOnStart) return;

        // Delay one frame slice so every component on the placed object has
        // finished Awake/OnEnable before the demo item is inserted.
        Invoke(nameof(SpawnDemoItem), Mathf.Max(0f, spawnDelay));
    }

    private void Update()
    {
        if (!allowKeyboardSpawn || !Input.GetKeyDown(spawnKey)) return;

        SpawnDemoItem();
    }

    public void SpawnDemoItem()
    {
        if (conveyorBelt == null || !conveyorBelt.CanReceive()) return;
        if (!ShouldSpawnOnThisBelt()) return;

        conveyorBelt.TryReceive(GetDemoItem());
    }

    private bool ShouldSpawnOnThisBelt()
    {
        if (!spawnOnlyWithoutInput) return true;
        if (GridBuildingSystem.Instance == null || placedObject == null || !placedObject.IsInitialized()) return true;
        if (!placedObject.TryGetInputGridPosition(out Vector2Int inputGridPosition)) return true;

        // Only the first belt in a chain should create the demo item. Belts
        // with another placed object behind them wait to receive from upstream.
        return !GridBuildingSystem.Instance.TryGetPlacedObjectAtGridPosition(inputGridPosition, out _);
    }

    private ItemSO GetDemoItem()
    {
        if (demoItem != null) return demoItem;

        if (runtimeFallbackItem == null)
        {
            runtimeFallbackItem = ScriptableObject.CreateInstance<ItemSO>();
            runtimeFallbackItem.id = "demo_item";
            runtimeFallbackItem.displayName = "Demo Item";
        }

        return runtimeFallbackItem;
    }
}
