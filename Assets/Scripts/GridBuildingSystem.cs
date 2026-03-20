using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class GridBuildingSystem : MonoBehaviour
{
    [SerializeField] private PlacedObjectTypeSO placedObjectTypeSO;
    private PlacedObjectTypeSO.Dir dir=PlacedObjectTypeSO.Dir.Down;
    private GridXZ<GridObject> grid;
    
    private void Awake()
    {
        int initialWidth=10;
        int initialHeight=10;
        float cellSize=4f;
        this.grid=new GridXZ<GridObject>(initialWidth,initialHeight,cellSize,new Vector3(0,0), 
                (GridXZ<GridObject> g, int x, int z)=>new GridObject(g,x,z));
    }


    public class GridObject
    {
        private GridXZ<GridObject> grid;
        private int x,z;
        public Transform objectTransform;
        public GridObject(GridXZ<GridObject> grid, int x, int z)
        {
            this.grid=grid;
            this.x=x;
            this.z=z;
        }

        public void SetTransform(Transform transform)
        {
            this.objectTransform=transform;
            grid.TriggerGridObjectChanged(x,z);
        }

        public void ClearTransform()
        {
            objectTransform=null;
            grid.TriggerGridObjectChanged(x,z);
        }

        public bool CanBuild()
        {
            return objectTransform==null;
        }
        public override String ToString()
        {
            return this.x+", "+this.z+"\n"+objectTransform;
        }
    }



    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            this.grid.GetXZ(Mouse3D.GetMouseWorldPosition(), out int x, out int z);
            GridObject gridObject=grid.GetGridObject(x,z);
            List<Vector2Int> gridPositionList=placedObjectTypeSO.GetGridPositionList(new Vector2Int(x,z),dir);
            bool canBuild=true;
            Vector2Int offSet=placedObjectTypeSO.GetRotaionOffset(dir);
            Vector3 ObjectPosition=grid.GetWorldPosition(x,z)+new Vector3(offSet.x,0,offSet.y)*grid.GetCellSize();
            foreach(Vector2Int gridPosition in gridPositionList)
            {
                if(!grid.GetGridObject(gridPosition.x, gridPosition.y).CanBuild())
                {
                    canBuild=false;
                }
            }
            
            if (canBuild)
            {
                Transform tempTransform=Instantiate(placedObjectTypeSO.prefab, ObjectPosition, Quaternion.Euler(0,PlacedObjectTypeSO.GetRotationAngle(dir),0));
                foreach(Vector2Int gridPosition in gridPositionList)
                {
                    grid.GetGridObject(gridPosition.x, gridPosition.y).SetTransform(tempTransform);
                }
            }
            else
            {
                UICLass.CreateWorldTextPopup("cannot build here", grid.GetWorldPosition(x,z)+new Vector3(grid.GetCellSize()/2,0,0));
            }
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            dir=PlacedObjectTypeSO.GetNextDir(dir);
            UICLass.CreateWorldTextPopup(""+dir,Mouse3D.GetMouseWorldPosition());
        }
    }

}
