using System;
using UnityEngine;

public class UICLass : MonoBehaviour
{
    public static void CreateWorldTextPopup(String text, Vector3 position)
    {
        GameObject obj=new GameObject("WorldText");
        obj.transform.position=position;
        TextMesh textMesh=obj.AddComponent<TextMesh>();
        obj.AddComponent<PopupMove>();
        textMesh.fontSize=50;
        textMesh.characterSize=0.1f;
        textMesh.color=Color.white;
        textMesh.text=text;
        textMesh.alignment=TextAlignment.Center;
        textMesh.anchor=TextAnchor.MiddleCenter;

        Destroy(obj,1.5f);
    }
}

public class PopupMove : MonoBehaviour
{
    public float speed=1.5f;
    void Update()
    {
        transform.position+=Vector3.up*speed*Time.deltaTime;
    }
}