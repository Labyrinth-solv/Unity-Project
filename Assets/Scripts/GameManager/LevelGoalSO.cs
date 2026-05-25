using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class LevelGoalItem
{
    public ItemSO targetItem;
    public float requiredRatePerSecond = 1f;
    public float sampleWindow = 10f;
}

[System.Serializable]
public class MachinePlacementLimit
{
    public PlacedObjectTypeSO machineType;
    public int maxCount = 1;
}

[CreateAssetMenu(menuName = "Factory/Level Goal")]
public class LevelGoalSO : ScriptableObject
{
    public string goalName = "Production Target";
    public int score = 10;
    public List<LevelGoalItem> itemGoals = new List<LevelGoalItem>();
    public List<MachinePlacementLimit> machinePlacementLimits = new List<MachinePlacementLimit>();
}
