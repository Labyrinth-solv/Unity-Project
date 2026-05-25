public static class LevelSelectionState
{
    public static LevelGoalSO SelectedLevelGoal { get; private set; }

    public static void SelectLevel(LevelGoalSO levelGoal)
    {
        SelectedLevelGoal = levelGoal;
    }

    public static void ClearSelection()
    {
        SelectedLevelGoal = null;
    }
}
