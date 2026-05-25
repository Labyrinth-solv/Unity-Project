using UnityEngine;
using System.Collections.Generic;

public class LevelInitializer : MonoBehaviour
{
    [System.Serializable]
    public struct InitialPlacement
    {
        public PlacedObjectTypeSO type;
        public Vector2Int origin;
        public PlacedObjectTypeSO.Dir direction;
    }

    [SerializeField] private List<InitialPlacement> startingBuildings;
    [SerializeField] private List<LevelGoalSO> levelGoals;

    private void Start()
    {
        ProductionRateTracker.ResetTracking();
        List<LevelGoalSO> goalsToLoad = GetGoalsToLoad();

        // Setup Goals
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.ClearGoals();

            foreach (var goal in goalsToLoad)
            {
                LevelManager.Instance.AddGoal(goal);
            }
        }

        if (GridBuildingSystem.Instance != null)
        {
            GridBuildingSystem.Instance.ClearPlacementLimits();
            foreach (var goal in goalsToLoad)
            {
                if (goal == null) continue;
                GridBuildingSystem.Instance.AddPlacementLimits(goal.machinePlacementLimits);
            }
        }

        // Spawn Buildings
        foreach (var placement in startingBuildings)
        {
            Vector2Int origin = GetPlacementOrigin(placement);
            GridBuildingSystem.Instance.TryPlaceObject(placement.type, origin, placement.direction);
        }
    }

    private List<LevelGoalSO> GetGoalsToLoad()
    {
        LevelGoalSO selectedLevelGoal = LevelSelectionState.SelectedLevelGoal;

        if (selectedLevelGoal != null)
        {
            return new List<LevelGoalSO> { selectedLevelGoal };
        }

        return levelGoals ?? new List<LevelGoalSO>();
    }

    private Vector2Int GetPlacementOrigin(InitialPlacement placement)
    {
        if (!IsGoalStorageType(placement.type) || GridBuildingSystem.Instance == null)
        {
            return placement.origin;
        }

        Vector2Int gridSize = GridBuildingSystem.Instance.GetGridSize();
        Vector2Int footprintSize = GetFootprintSize(placement.type, placement.direction);
        int x = Mathf.RoundToInt((gridSize.x - footprintSize.x) * 0.5f);
        int z = gridSize.y - footprintSize.y;

        return new Vector2Int(Mathf.Max(0, x), Mathf.Max(0, z));
    }

    private bool IsGoalStorageType(PlacedObjectTypeSO type)
    {
        return type != null
            && type.prefab != null
            && type.prefab.GetComponent<ProductionGoalStorage>() != null;
    }

    private Vector2Int GetFootprintSize(PlacedObjectTypeSO type, PlacedObjectTypeSO.Dir direction)
    {
        if (direction == PlacedObjectTypeSO.Dir.Down || direction == PlacedObjectTypeSO.Dir.Up)
        {
            return new Vector2Int(type.height, type.width);
        }

        return new Vector2Int(type.width, type.height);
    }
}
