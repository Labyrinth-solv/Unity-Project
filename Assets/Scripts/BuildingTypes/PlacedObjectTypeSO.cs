using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu()]
public class PlacedObjectTypeSO : ScriptableObject
{
    public enum Dir
    {
        Down, Left, Right, Up
    }
    
    public string nameString;
    public Transform prefab;
    public Transform visual;
    public int width;
    public int height;

    public static Dir GetNextDir(Dir dir)
    {
        switch (dir)
        {
            default:
            case Dir.Right: return Dir.Down;
            case Dir.Down:  return Dir.Left;
            case Dir.Left:  return Dir.Up;
            case Dir.Up:    return Dir.Right;
        }
    }
    public static int GetRotationAngle(Dir dir)
    {
        switch (dir)
        {
            default:
            // The conveyor prefab is modeled facing Right at rotation 0.
            // Rotations below turn that local +X direction onto the grid.
            case Dir.Right: return 0;
            case Dir.Down:  return 90;
            case Dir.Left:  return 180;
            case Dir.Up:    return 270;
        }
    }

    public static Vector2Int GetDirectionVector(Dir dir)
    {
        switch (dir)
        {
            default:
            case Dir.Down:  return new Vector2Int(0, -1);
            case Dir.Left:  return new Vector2Int(-1, 0);
            case Dir.Up:    return new Vector2Int(0, 1);
            case Dir.Right: return new Vector2Int(1, 0);
        }
    }

    public Vector2Int GetRotaionOffset(Dir dir)
    {
        switch (dir)
        {
            default:
            // Offset must follow the same rotation mapping as GetRotationAngle.
            // The prefab's unrotated state is Right, so Right keeps the object
            // on the clicked grid origin.
            case Dir.Right: return new Vector2Int(0,0);
            case Dir.Down: return new Vector2Int(0,width);
            case Dir.Left: return new Vector2Int(width,height);
            case Dir.Up: return new Vector2Int(height,0);

        }
    }

    public List<Vector2Int> GetGridPositionList(Vector2Int offset, Dir dir)
    {
        List<Vector2Int> gridPositionList = new List<Vector2Int>();
        switch(dir){
            default:
            // Right and Left are 0/180 degree rotations, so they keep the
            // original width x height footprint.
            case Dir.Right:
            case Dir.Left:
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        gridPositionList.Add(offset + new Vector2Int(x, y));
                    }
                }
                break;
            // Down and Up are 90/270 degree rotations, so width and height
            // are swapped on the grid.
            case Dir.Down:
            case Dir.Up:
                for (int x = 0; x < height; x++)
                {
                    for (int y = 0; y < width; y++)
                    {
                        gridPositionList.Add(offset + new Vector2Int(x, y));
                    }
                }
                break;
        }
        return gridPositionList;
    }
}
