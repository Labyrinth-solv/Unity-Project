using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GridBuildingSystem : MonoBehaviour
{
    [SerializeField] private List<PlacedObjectTypeSO> placedObjectTypeSOList;
    [SerializeField] private int initialWidth = 25;
    [SerializeField] private int initialHeight = 25;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private bool showGridCoordinates = true;
    [SerializeField] private bool showPlacedObjectString = true;
    private PlacedObjectTypeSO placedObjectTypeSO;
    private PlacedObjectTypeSO.Dir dir = PlacedObjectTypeSO.Dir.Right;
    private GridXZ<GridObject> grid;
    private bool cachedShowGridCoordinates;
    private bool cachedShowPlacedObjectString;
    private readonly Dictionary<PlacedObjectTypeSO, int> placementLimits = new Dictionary<PlacedObjectTypeSO, int>();
    private readonly Dictionary<PlacedObjectTypeSO, int> placedCounts = new Dictionary<PlacedObjectTypeSO, int>();
    private readonly HashSet<PlacedObject> limitCountedObjects = new HashSet<PlacedObject>();
    public static GridBuildingSystem Instance { get; private set; }
    public event EventHandler OnSelectedChanged;
    public event Action OnPlacementLimitsChanged;
    private void Awake()
    {
        Instance = this;
        this.grid = new GridXZ<GridObject>(initialWidth, initialHeight, cellSize, new Vector3(0, 0),
                (GridXZ<GridObject> g, int x, int z) => new GridObject(g, x, z),
                GetGridDebugText);
        cachedShowGridCoordinates = showGridCoordinates;
        cachedShowPlacedObjectString = showPlacedObjectString;
        placedObjectTypeSO = null;
        RefreshDebugVisibility();
    }

    public Vector3 GetMouseWorldSnappedPosition()
    {
        if (!placedObjectTypeSO) return Vector3.zero;
        Vector3 mouseWorldPosition = Mouse3D.GetMouseWorldPosition();

        grid.GetXZ(mouseWorldPosition, out int x, out int z);
        Vector2Int offset = placedObjectTypeSO.GetRotaionOffset(dir);
        return grid.GetWorldPosition(x, z)
            + new Vector3(offset.x, 0, offset.y) * grid.GetCellSize();
    }

    public Quaternion GetPlacedObjectRotation()
    {
        return Quaternion.Euler(0, PlacedObjectTypeSO.GetRotationAngle(dir), 0);
    }

    public Vector2Int GetGridSize()
    {
        return grid != null ? grid.GetSize() : Vector2Int.zero;
    }

    public float GetCellSize()
    {
        return grid != null ? grid.GetCellSize() : 0f;
    }

    public Vector3 GetGridWorldPosition(int x, int z)
    {
        return grid != null ? grid.GetWorldPosition(x, z) : Vector3.zero;
    }


    public PlacedObjectTypeSO GetPlacedObjectTypeSO()
    {
        return this.placedObjectTypeSO;
    }

    public PlacedObjectTypeSO GetPlacedObjectTypeSO(int index)
    {
        if (index < 0 || index >= placedObjectTypeSOList.Count)
        {
            return null;
        }

        return placedObjectTypeSOList[index];
    }

    public bool TryGetRemainingPlacementCount(PlacedObjectTypeSO type, out int remainingCount)
    {
        remainingCount = 0;
        if (type == null || !placementLimits.TryGetValue(type, out int maxCount))
        {
            return false;
        }

        placedCounts.TryGetValue(type, out int currentCount);
        remainingCount = Mathf.Max(0, maxCount - currentCount);
        return true;
    }

    public bool TryGetPlacedObjectAtGridPosition(Vector2Int gridPosition, out PlacedObject placedObject)
    {
        placedObject = null;
        GridObject gridObject = grid.GetGridObject(gridPosition.x, gridPosition.y);
        if (gridObject == null) return false;

        placedObject = gridObject.GetPlacedObject();
        return placedObject != null;
    }

    public void SetSelectedObject(int index)
    {
        if (index == -1)
        {
            placedObjectTypeSO = null;
            RefreshDebugVisibility();
            OnSelectedChanged?.Invoke(this, EventArgs.Empty);
            return;
        }
        if (index < 0 || index >= placedObjectTypeSOList.Count) return;

        placedObjectTypeSO = placedObjectTypeSOList[index];
        RefreshDebugVisibility();
        OnSelectedChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RefreshDebugVisibility()
    {
        grid?.SetDebugVisible(placedObjectTypeSO != null);
    }

    private string GetGridDebugText(int x, int z, GridObject gridObject)
    {
        return gridObject != null
            ? gridObject.GetDebugText(showGridCoordinates, showPlacedObjectString)
            : string.Empty;
    }

    private void RefreshDebugTextIfNeeded()
    {
        if (cachedShowGridCoordinates == showGridCoordinates
            && cachedShowPlacedObjectString == showPlacedObjectString)
        {
            return;
        }

        cachedShowGridCoordinates = showGridCoordinates;
        cachedShowPlacedObjectString = showPlacedObjectString;
        grid?.RefreshAllDebugText();
    }

    public void ClearPlacementLimits()
    {
        placementLimits.Clear();
        placedCounts.Clear();
        limitCountedObjects.Clear();
        OnPlacementLimitsChanged?.Invoke();
    }

    public void AddPlacementLimits(List<MachinePlacementLimit> limits)
    {
        if (limits == null)
        {
            return;
        }

        foreach (var limit in limits)
        {
            if (limit == null || limit.machineType == null)
            {
                continue;
            }

            int maxCount = Mathf.Max(0, limit.maxCount);
            if (placementLimits.TryGetValue(limit.machineType, out int existingLimit))
            {
                placementLimits[limit.machineType] = Mathf.Min(existingLimit, maxCount);
            }
            else
            {
                placementLimits.Add(limit.machineType, maxCount);
            }
        }

        OnPlacementLimitsChanged?.Invoke();
    }

    private bool IsPlacementLimitReached(PlacedObjectTypeSO type)
    {
        if (type == null || !placementLimits.TryGetValue(type, out int maxCount))
        {
            return false;
        }

        placedCounts.TryGetValue(type, out int currentCount);
        return currentCount >= maxCount;
    }

    private void IncrementPlacedCount(PlacedObject placedObject, PlacedObjectTypeSO type)
    {
        if (placedObject == null || type == null || !placementLimits.ContainsKey(type))
        {
            return;
        }

        placedCounts.TryGetValue(type, out int currentCount);
        placedCounts[type] = currentCount + 1;
        limitCountedObjects.Add(placedObject);
        OnPlacementLimitsChanged?.Invoke();
    }

    private void DecrementPlacedCount(PlacedObject placedObject, PlacedObjectTypeSO type)
    {
        if (placedObject == null || type == null || !placementLimits.ContainsKey(type))
        {
            return;
        }

        if (!limitCountedObjects.Remove(placedObject))
        {
            return;
        }

        placedCounts.TryGetValue(type, out int currentCount);
        placedCounts[type] = Mathf.Max(0, currentCount - 1);
        OnPlacementLimitsChanged?.Invoke();
    }

    public class GridObject
    {
        private GridXZ<GridObject> grid;
        private int x, z;
        public PlacedObject placedObject;
        public GridObject(GridXZ<GridObject> grid, int x, int z)
        {
            this.grid = grid;
            this.x = x;
            this.z = z;
        }

        public PlacedObject GetPlacedObject()
        {
            return placedObject;
        }

        public void SetPlacedObject(PlacedObject placedObject)
        {
            this.placedObject = placedObject;
            grid.TriggerGridObjectChanged(x, z);
        }

        public void ClearPlacedObject()
        {
            placedObject = null;
            grid.TriggerGridObjectChanged(x, z);
        }

        public bool CanBuild()
        {
            return placedObject == null;
        }

        public string GetDebugText(bool showCoordinates, bool showPlacedObjectString)
        {
            string debugText = string.Empty;

            if (showCoordinates)
            {
                debugText = x + ", " + z;
            }

            if (showPlacedObjectString && placedObject != null)
            {
                if (!string.IsNullOrEmpty(debugText))
                {
                    debugText += "\n";
                }

                debugText += placedObject;
            }

            return debugText;
        }

        public override string ToString()
        {
            return this.x + ", " + this.z + "\n" + placedObject;
        }
    }



    public bool TryPlaceObject(PlacedObjectTypeSO type, Vector2Int origin, PlacedObjectTypeSO.Dir dir, bool countTowardsLimit = false)
    {
        if (type == null || grid == null)
        {
            return false;
        }

        if (countTowardsLimit && IsPlacementLimitReached(type))
        {
            return false;
        }

        List<Vector2Int> gridPositionList = type.GetGridPositionList(origin, dir);
        foreach (Vector2Int gridPosition in gridPositionList)
        {
            GridObject gridObject = grid.GetGridObject(gridPosition.x, gridPosition.y);
            if (gridObject == null || !gridObject.CanBuild())
            {
                return false;
            }
        }

        Vector2Int offset = type.GetRotaionOffset(dir);
        Vector3 worldPosition = grid.GetWorldPosition(origin.x, origin.y) + new Vector3(offset.x, 0, offset.y) * grid.GetCellSize();

        PlacedObject placedObject = PlacedObject.Create(worldPosition, type, dir, origin);
        foreach (Vector2Int gridPosition in gridPositionList)
        {
            grid.GetGridObject(gridPosition.x, gridPosition.y).SetPlacedObject(placedObject);
        }

        if (countTowardsLimit)
        {
            IncrementPlacedCount(placedObject, type);
        }

        return true;
    }

    private void Update()
    {
        RefreshDebugTextIfNeeded();
        grid.DrawDebug();
        Vector2Int gridSize = this.grid.GetSize();

        if (placedObjectTypeSO && Input.GetMouseButtonDown(0))
        {
            if (IsPointerOverUI())
            {
                return;
            }

            this.grid.GetXZ(Mouse3D.GetMouseWorldPosition(), out int x, out int z);
            Vector2Int origin = new Vector2Int(x, z);
            if (IsPlacementLimitReached(placedObjectTypeSO))
            {
                UICLass.CreateWorldTextPopup("placement limit reached", grid.GetWorldPosition(x, z) + new Vector3(grid.GetCellSize() / 2, 0, 0));
            }
            else if (!TryPlaceObject(placedObjectTypeSO, origin, dir, true))
            {
                UICLass.CreateWorldTextPopup("cannot build here", grid.GetWorldPosition(x, z) + new Vector3(grid.GetCellSize() / 2, 0, 0));
            }
        }
        if (Input.GetMouseButtonDown(1))
        {
            if (IsPointerOverUI())
            {
                return;
            }

            this.grid.GetXZ(Mouse3D.GetMouseWorldPosition(), out int x, out int z);
            GridObject gridObject = grid.GetGridObject(x, z);
            PlacedObject placedObject = gridObject != null ? gridObject.GetPlacedObject() : null;
            if (placedObject != null && placedObject.CanBeDestroyed())
            {
                PlacedObjectTypeSO destroyedType = placedObject.GetPlacedObjectTypeSO();
                List<Vector2Int> gridPositionList = placedObject.GetGridPositionList();
                placedObject.DestroySelf();
                DecrementPlacedCount(placedObject, destroyedType);
                foreach (Vector2Int gridPosition in gridPositionList)
                {
                    grid.GetGridObject(gridPosition.x, gridPosition.y).ClearPlacedObject();
                }
            }
        }
        bool isCtrlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        if (!isCtrlHeld && Input.GetKeyDown(KeyCode.R))
        {
            dir = PlacedObjectTypeSO.GetNextDir(dir);
            UICLass.CreateWorldTextPopup("" + dir, Mouse3D.GetMouseWorldPosition());
        }
    }

    private bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

}
