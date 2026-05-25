using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class GridXZ<TGridObject>
{
    private Transform debugGridParent;
    private int width;
    private int height;
    private readonly float cellSize;
    private Transform[,] gridDebugObject;
    private readonly Vector3 originPosition;
    private TGridObject[,] gridArray;
    private Func<int, int, TGridObject, string> debugTextFormatter;
    public event EventHandler<OnGridValueChangedEventArgs> onGridValueChanged;
    public class OnGridValueChangedEventArgs : EventArgs
    {
        public int x;
        public int z;
    }
    public GridXZ(
        int width,
        int height,
        float cellSize,
        Vector3 originPosition,
        Func<GridXZ<TGridObject>, int, int, TGridObject> createGridObject,
        Func<int, int, TGridObject, string> debugTextFormatter = null)
    {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;
        this.originPosition = originPosition;
        this.debugTextFormatter = debugTextFormatter;
        gridArray = new TGridObject[width, height];
        gridDebugObject = new Transform[width, height];
        debugGridParent = new GameObject("GridDebugObjects").transform;
        debugGridParent.gameObject.SetActive(false);
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                gridArray[i, j] = createGridObject(this, i, j);
            }
        }
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                // Tao TextMesh Object
                Vector3 gridPosition = GetWorldPosition(i, j);
                GameObject obj = new GameObject("WT", typeof(TextMesh));
                obj.transform.SetParent(debugGridParent);
                obj.transform.position = gridPosition + new Vector3(1, 0, 1) * cellSize * 0.5f;
                TextMesh textMesh = obj.GetComponent<TextMesh>();
                //Hien thi tren man hinh
                textMesh.text = GetDebugText(i, j);
                textMesh.fontSize = 20;
                textMesh.characterSize = 0.1f;
                textMesh.color = Color.white;
                textMesh.alignment = TextAlignment.Center;
                textMesh.anchor = TextAnchor.MiddleCenter;
                gridDebugObject[i, j] = obj.transform;
            }
        }

        onGridValueChanged += (object sender, OnGridValueChangedEventArgs e) =>
        {
            gridDebugObject[e.x, e.z].GetComponent<TextMesh>().text = GetDebugText(e.x, e.z);
        };
    }

    public void SetDebugTextFormatter(Func<int, int, TGridObject, string> debugTextFormatter)
    {
        this.debugTextFormatter = debugTextFormatter;
        RefreshAllDebugText();
    }

    public void RefreshAllDebugText()
    {
        if (gridDebugObject == null)
        {
            return;
        }

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (gridDebugObject[i, j] == null)
                {
                    continue;
                }

                gridDebugObject[i, j].GetComponent<TextMesh>().text = GetDebugText(i, j);
            }
        }
    }

    private string GetDebugText(int x, int z)
    {
        if (debugTextFormatter != null)
        {
            return debugTextFormatter(x, z, gridArray[x, z]);
        }

        return Convert.ToString(gridArray[x, z]);
    }

    public float GetCellSize()
    {
        return this.cellSize;
    }

    public Vector2Int GetSize()
    {
        return new Vector2Int(width, height);
    }

    public void GetXZ(Vector3 mousePosition, out int x, out int z)
    {
        x = (int)(mousePosition.x / cellSize);
        z = (int)(mousePosition.z / cellSize);
    }
    public Vector3 GetWorldPosition(int x, int z)
    {
        return new Vector3(x, 0, z) * cellSize + originPosition;
    }

    public void SetDebugVisible(bool isVisible)
    {
        if (debugGridParent != null && debugGridParent.gameObject.activeSelf != isVisible)
        {
            debugGridParent.gameObject.SetActive(isVisible);
        }
    }

    public void DrawDebug()
    {
        if (debugGridParent == null || !debugGridParent.gameObject.activeSelf)
        {
            return;
        }

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                Debug.DrawLine(GetWorldPosition(i, j), GetWorldPosition(i + 1, j), Color.white);
                Debug.DrawLine(GetWorldPosition(i, j), GetWorldPosition(i, j + 1), Color.white);
            }
        }

        Debug.DrawLine(GetWorldPosition(0, height), GetWorldPosition(width, height), Color.white);
        Debug.DrawLine(GetWorldPosition(width, 0), GetWorldPosition(width, height), Color.white);
    }

    public void TriggerGridObjectChanged(int x, int z)
    {
        onGridValueChanged?.Invoke(this, new OnGridValueChangedEventArgs { x = x, z = z });
    }

    public TGridObject GetGridObject(int x, int z)
    {
        if (x >= 0 && z >= 0 && x < width && z < height) return gridArray[x, z];
        return default(TGridObject);
    }
    public TGridObject GetGridObject(Vector3 mousePosition)
    {
        int x, z;
        GetXZ(mousePosition, out x, out z);
        return GetGridObject(x, z);
    }
}
