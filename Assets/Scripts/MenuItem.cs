using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MenuItem : MonoBehaviour
{
    [SerializeField] private string actionName;
    [SerializeField] private TMP_Text remainingCountText;
    [SerializeField] private PlacedObjectTypeSO machineTypeOverride;
    [SerializeField] private bool hideWhenUnlimited = true;

    private PlacedObjectTypeSO machineType;
    private bool isSubscribedToGrid;

    private void OnEnable()
    {
        TrySubscribeToGrid();
        ResolveMachineType();
        RefreshRemainingCount();
    }

    private void OnDisable()
    {
        if (GridBuildingSystem.Instance != null)
        {
            GridBuildingSystem.Instance.OnPlacementLimitsChanged -= RefreshRemainingCount;
        }

        isSubscribedToGrid = false;
    }

    private void Start()
    {
        TrySubscribeToGrid();
        ResolveMachineType();
        RefreshRemainingCount();
    }

    void Update()
    {
        TrySubscribeToGrid();

        if (Input.GetKeyDown(KeyCode.X))
        {
            GridBuildingSystem.Instance?.SetSelectedObject(-1);
        }
    }

    private void TrySubscribeToGrid()
    {
        if (isSubscribedToGrid || GridBuildingSystem.Instance == null)
        {
            return;
        }

        GridBuildingSystem.Instance.OnPlacementLimitsChanged += RefreshRemainingCount;
        isSubscribedToGrid = true;
    }
    public void onClick()
    {
        if (TryGetActionIndex(out int index))
        {
            GridBuildingSystem.Instance?.SetSelectedObject(index);
        }
    }

    public void onButton1() => SetActionAndClick("button1");
    public void onButton2() => SetActionAndClick("button2");
    public void onButton3() => SetActionAndClick("button3");
    public void onButton4() => SetActionAndClick("button4");

    private void SetActionAndClick(string newActionName)
    {
        actionName = newActionName;
        ResolveMachineType();
        onClick();
    }

    private void ResolveMachineType()
    {
        if (machineTypeOverride != null)
        {
            machineType = machineTypeOverride;
            return;
        }

        if (GridBuildingSystem.Instance == null || !TryGetActionIndex(out int index))
        {
            machineType = null;
            return;
        }

        machineType = GridBuildingSystem.Instance.GetPlacedObjectTypeSO(index);
    }

    private bool TryGetActionIndex(out int index)
    {
        index = -1;
        if (string.IsNullOrWhiteSpace(actionName))
        {
            return false;
        }

        if (!actionName.StartsWith("button", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string numberPart = actionName.Substring("button".Length);

        if (!int.TryParse(numberPart, out int parsedIndex))
        {
            return false;
        }

        index = parsedIndex - 1;
        return index >= 0;
    }

    private void RefreshRemainingCount()
    {
        if (remainingCountText == null)
        {
            return;
        }

        if (GridBuildingSystem.Instance == null)
        {
            remainingCountText.gameObject.SetActive(false);
            return;
        }

        if (machineType == null)
        {
            ResolveMachineType();
        }

        if (GridBuildingSystem.Instance.TryGetRemainingPlacementCount(machineType, out int remainingCount))
        {
            remainingCountText.gameObject.SetActive(true);
            remainingCountText.text = remainingCount.ToString();
            return;
        }

        remainingCountText.gameObject.SetActive(!hideWhenUnlimited);
        if (!hideWhenUnlimited)
        {
            remainingCountText.text = "";
        }
    }
}
