using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class GridBuildingSystem : MonoBehaviour
{
    [SerializeField] private List<PlacedObjectTypeSO> placedObjectTypeSOList;
    private PlacedObjectTypeSO placedObjectTypeSO;
    private PlacedObjectTypeSO.Dir dir = PlacedObjectTypeSO.Dir.Down;
    private GridXZ<GridObject> grid;
    public static GridBuildingSystem Instance { get; private set; }
    public event EventHandler OnSelectedChanged;
    private void Awake()
    {
        Instance = this;
        int initialWidth = 10;
        int initialHeight = 10;
        float cellSize = 1f;
        this.grid = new GridXZ<GridObject>(initialWidth, initialHeight, cellSize, new Vector3(0, 0),
                (GridXZ<GridObject> g, int x, int z) => new GridObject(g, x, z));
        placedObjectTypeSO = null;
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


    public PlacedObjectTypeSO GetPlacedObjectTypeSO()
    {
        return this.placedObjectTypeSO;
    }

    public void SetSelectedObject(int index)
    {
        if (index == -1)
        {
            placedObjectTypeSO = null;
            OnSelectedChanged?.Invoke(this, EventArgs.Empty);
        }
        if (index < 0 || index >= placedObjectTypeSOList.Count) return;

        placedObjectTypeSO = placedObjectTypeSOList[index];
        OnSelectedChanged?.Invoke(this, EventArgs.Empty);
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
        public override string ToString()
        {
            return this.x + ", " + this.z + "\n" + placedObject;
        }
    }



    private void Update()
    {
        Vector2Int gridSize = this.grid.GetSize();

        if (placedObjectTypeSO && Input.GetMouseButtonDown(0))
        {
            this.grid.GetXZ(Mouse3D.GetMouseWorldPosition(), out int x, out int z);
            GridObject gridObject = grid.GetGridObject(x, z);
            List<Vector2Int> gridPositionList = placedObjectTypeSO.GetGridPositionList(new Vector2Int(x, z), dir);
            bool canBuild = true;
            Vector2Int offSet = placedObjectTypeSO.GetRotaionOffset(dir);
            Vector3 ObjectPosition = grid.GetWorldPosition(x, z) + new Vector3(offSet.x, 0, offSet.y) * grid.GetCellSize();
            foreach (Vector2Int gridPosition in gridPositionList)
            {
                GridObject objectPosition = grid.GetGridObject(gridPosition.x, gridPosition.y);
                if (objectPosition == null)
                {
                    canBuild = false;
                    break;
                }
                else
                {
                    if (!objectPosition.CanBuild())
                    {
                        canBuild = false;
                        break;
                    }
                }
            }

            if (canBuild)
            {
                PlacedObject placedObject = PlacedObject.Create(ObjectPosition, placedObjectTypeSO, dir, new Vector2Int(x, z));
                foreach (Vector2Int gridPosition in gridPositionList)
                {
                    grid.GetGridObject(gridPosition.x, gridPosition.y).SetPlacedObject(placedObject);
                }
            }
            else
            {
                UICLass.CreateWorldTextPopup("cannot build here", grid.GetWorldPosition(x, z) + new Vector3(grid.GetCellSize() / 2, 0, 0));
            }
        }
        if (Input.GetMouseButtonDown(1))
        {
            this.grid.GetXZ(Mouse3D.GetMouseWorldPosition(), out int x, out int z);
            GridObject gridObject = grid.GetGridObject(x, z);
            PlacedObject placedObject = gridObject.GetPlacedObject();
            if (placedObject != null)
            {
                List<Vector2Int> gridPositionList = placedObject.GetGridPositionList();
                placedObject.DestroySelf();
                foreach (Vector2Int gridPosition in gridPositionList)
                {
                    grid.GetGridObject(gridPosition.x, gridPosition.y).ClearPlacedObject();
                }
            }
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            dir = PlacedObjectTypeSO.GetNextDir(dir);
            UICLass.CreateWorldTextPopup("" + dir, Mouse3D.GetMouseWorldPosition());
        }
    }

}
