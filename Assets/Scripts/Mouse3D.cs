using UnityEngine;
public class Mouse3D:MonoBehaviour
{
    public static Mouse3D Instance;

    private void Awake()
    {
        Instance=this;
    }
    public static Vector3 GetMouseWorldPosition()=>Instance.GetMouseWorldPosition_Instance();

    public Vector3 GetMouseWorldPosition_Instance()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        Plane plane = new Plane(Vector3.up, Vector3.zero);

        if (plane.Raycast(ray, out float enter))
        {
            return ray.GetPoint(enter);
        }
        return Vector3.zero;
    }
}
