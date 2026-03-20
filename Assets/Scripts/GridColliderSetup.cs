using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridColliderSetup : MonoBehaviour
{
    public void SetUp(int gridWidth, int gridHeight, float cellSize)
    {
        transform.localScale=new Vector3(gridWidth*cellSize/10f, 0, gridHeight*cellSize/10f);
    }
}
