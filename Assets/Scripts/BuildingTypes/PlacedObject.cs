using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlacedObject : MonoBehaviour
{
    private PlacedObjectTypeSO placedObjectTypeSO;
    private Vector2Int origin;
    private PlacedObjectTypeSO.Dir dir;

    public static PlacedObject Create(Vector3 worldPostition, PlacedObjectTypeSO placedObjectTypeSO, PlacedObjectTypeSO.Dir dir, Vector2Int origin)
    {
        Transform placedObjectTypeTransform = Instantiate(placedObjectTypeSO.prefab, worldPostition, Quaternion.Euler(0,PlacedObjectTypeSO.GetRotationAngle(dir),0));
        PlacedObject placedObject = placedObjectTypeTransform.GetComponent<PlacedObject>();
        placedObject.placedObjectTypeSO=placedObjectTypeSO;
        placedObject.origin=origin;
        placedObject.dir=dir;
        return placedObject;
    }

    public void DestroySelf()
    {
        Destroy(gameObject);
    }
    
    public List<Vector2Int> GetGridPositionList()
    {
        return placedObjectTypeSO.GetGridPositionList(origin, dir);
    }
}
