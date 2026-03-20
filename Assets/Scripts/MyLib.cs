using Unity.Burst.Intrinsics;
using UnityEngine;
public class MyLib
{
    public static void CreateEmptyMeshArray(int quadCount, 
    out Vector3[] vertices, out Vector2[] uv, out int[] triangles)
    {
        int vCount=quadCount*4;
        int tCount=quadCount*6;
        vertices=new Vector3[vCount];
        uv=new Vector2[vCount];
        triangles=new int[tCount];
    }
    
	public static void AddToMeshArrays(
		Vector3[] vertices, Vector2[] uvs, int[] triangles,
		int index, Vector3 pos, float rotDeg, Vector3 baseSize, Vector2 uv00, Vector2 uv11)
	{
		// 1) half size quanh tâm
		Vector3 half = baseSize * 0.5f;

		// 2) ma trận quay quanh Z
		Quaternion R = Quaternion.Euler(0f, 0f, rotDeg);

		// 3) 4 góc (local offsets quanh (0,0)), rồi quay & tịnh tiến
		int v = index * 4;
		int v0 = v + 0;
		int v1 = v + 1;
		int v2 = v + 2;
		int v3 = v + 3;

		vertices[v0] = pos + R * new Vector3(-half.x, +half.y); // TL
		vertices[v1] = pos + R * new Vector3(-half.x, -half.y); // BL
		vertices[v2] = pos + R * new Vector3(+half.x, -half.y); // BR
		vertices[v3] = pos + R * new Vector3(+half.x, +half.y); // TR

		// 4) UVs (chuẩn)
		uvs[v0] = new Vector2(uv00.x, uv11.y);
		uvs[v1] = new Vector2(uv00.x, uv00.y);
		uvs[v2] = new Vector2(uv11.x, uv00.y);
		uvs[v3] = new Vector2(uv11.x, uv11.y);

		// 5) Triangles (winding order theo chiều nhìn Z+)
		int t = index * 6;
		triangles[t + 0] = v0;
		triangles[t + 1] = v3;
		triangles[t + 2] = v1;

		triangles[t + 3] = v1;
		triangles[t + 4] = v3;
		triangles[t + 5] = v2;
	}
}