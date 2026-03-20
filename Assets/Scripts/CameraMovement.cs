using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{
    public float cameraSpeed=10f;

    // Update is called once per frame
    void Update()
    {
        float h=Input.GetAxis("Horizontal");
        float v=Input.GetAxis("Vertical");

        Vector3 direction=new Vector3(h,0,v);
        transform.Translate(direction*cameraSpeed*Time.deltaTime, Space.World);
    }
}
