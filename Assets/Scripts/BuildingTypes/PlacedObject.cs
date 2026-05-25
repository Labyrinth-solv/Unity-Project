using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlacedObject : MonoBehaviour
{
    [SerializeField] private bool canBeDestroyed = true;
    private PlacedObjectTypeSO placedObjectTypeSO;
    private Vector2Int origin;
    private PlacedObjectTypeSO.Dir dir;
    private bool isInitialized;

    public static PlacedObject Create(Vector3 worldPostition, PlacedObjectTypeSO placedObjectTypeSO, PlacedObjectTypeSO.Dir dir, Vector2Int origin)
    {
        Transform placedObjectTypeTransform = Instantiate(placedObjectTypeSO.prefab, worldPostition, Quaternion.Euler(0,PlacedObjectTypeSO.GetRotationAngle(dir),0));
        PlacedObject placedObject = placedObjectTypeTransform.GetComponent<PlacedObject>();
        placedObject.Initialize(placedObjectTypeSO, origin, dir);
        return placedObject;
    }

    public void Initialize(PlacedObjectTypeSO placedObjectTypeSO, Vector2Int origin, PlacedObjectTypeSO.Dir dir)
    {
        this.placedObjectTypeSO=placedObjectTypeSO;
        this.origin=origin;
        this.dir=dir;
        isInitialized=placedObjectTypeSO != null;
    }

    public bool CanBeDestroyed()
    {
        return canBeDestroyed;
    }

    public void SetCanBeDestroyed(bool canBeDestroyed)
    {
        this.canBeDestroyed = canBeDestroyed;
    }

    public void DestroySelf()
{
        Destroy(gameObject);
    }

    public PlacedObjectTypeSO GetPlacedObjectTypeSO()
    {
        return placedObjectTypeSO;
    }

    public Vector2Int GetOrigin()
    {
        return origin;
    }

    public PlacedObjectTypeSO.Dir GetDir()
    {
        return dir;
    }

    public bool IsInitialized()
    {
        return isInitialized && placedObjectTypeSO != null;
    }

    public Vector2Int GetOutputGridPosition()
    {
        TryGetOutputGridPosition(out Vector2Int outputGridPosition);
        return outputGridPosition;
    }

    public bool TryGetOutputGridPosition(out Vector2Int outputGridPosition)
    {
        return TryGetEdgeGridPosition(PlacedObjectTypeSO.GetDirectionVector(dir), out outputGridPosition);
    }

    public bool TryGetInputGridPosition(out Vector2Int inputGridPosition)
    {
        return TryGetEdgeGridPosition(-PlacedObjectTypeSO.GetDirectionVector(dir), out inputGridPosition);
    }

    private bool TryGetEdgeGridPosition(Vector2Int direction, out Vector2Int edgeGridPosition)
    {
        List<Vector2Int> gridPositionList = GetGridPositionList();
        if (gridPositionList.Count == 0)
        {
            edgeGridPosition = default;
            return false;
        }

        // Pick the occupied cell furthest toward the requested direction,
        // then step one cell out. This keeps the method valid for both 1x1
        // conveyors and larger machines.
        edgeGridPosition = gridPositionList[0] + direction;
        int bestScore = gridPositionList[0].x * direction.x + gridPositionList[0].y * direction.y;
        for (int i = 1; i < gridPositionList.Count; i++)
        {
            Vector2Int gridPosition = gridPositionList[i];
            int score = gridPosition.x * direction.x + gridPosition.y * direction.y;
            if (score > bestScore)
            {
                bestScore = score;
                edgeGridPosition = gridPosition + direction;
            }
        }

        return true;
    }
    
    public List<Vector2Int> GetGridPositionList()
    {
        if (!IsInitialized())
        {
            return new List<Vector2Int>();
        }

        return placedObjectTypeSO.GetGridPositionList(origin, dir);
    }
}
