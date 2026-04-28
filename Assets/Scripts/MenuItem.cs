using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuItem : MonoBehaviour
{
    [SerializeField] private string actionName;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.X))
        {
            GridBuildingSystem.Instance.SetSelectedObject(-1);
        }
    }
    public void onClick()
    {
        int index=0;
        if(actionName=="button1") index=0;
        if(actionName=="button2") index=1;
        GridBuildingSystem.Instance.SetSelectedObject(index);
    }
    
}
