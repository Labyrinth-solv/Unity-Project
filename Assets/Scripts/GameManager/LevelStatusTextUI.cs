using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LevelStatusTextUI : MonoBehaviour
{
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private bool useGoalName;
    [SerializeField] private string completedSuffix = " Completed";

    private LevelGoalSO currentLevelGoal;
    private bool isSubscribedToLevelManager;

    private void OnEnable()
    {
        TrySubscribeToLevelManager();
    }

    private void Start()
    {
        RefreshText();
    }

    private void Update()
    {
        TrySubscribeToLevelManager();

        if (LevelManager.Instance == null)
        {
            return;
        }

        LevelGoalSO activeLevelGoal = GetCurrentLevelGoal();
        if (activeLevelGoal != currentLevelGoal)
        {
            RefreshText();
        }
    }

    private void OnDisable()
    {
        if (LevelManager.Instance != null && isSubscribedToLevelManager)
        {
            LevelManager.Instance.OnLevelCompleted -= RefreshText;
        }

        isSubscribedToLevelManager = false;
    }

    public void RefreshText()
    {
        if (levelText == null || LevelManager.Instance == null)
        {
            return;
        }

        LevelGoalSO levelGoal = GetCurrentLevelGoal();
        if (levelGoal != currentLevelGoal)
        {
            currentLevelGoal = levelGoal;
        }

        if (currentLevelGoal == null)
        {
            levelText.text = "";
            return;
        }

        levelText.text = GetLevelStatusText();
    }

    private LevelGoalSO GetCurrentLevelGoal()
    {
        List<LevelGoalSO> activeGoals = LevelManager.Instance.GetActiveGoals();
        if (activeGoals == null || activeGoals.Count == 0)
        {
            return LevelSelectionState.SelectedLevelGoal;
        }

        foreach (LevelGoalSO goal in activeGoals)
        {
            if (goal != null)
            {
                return goal;
            }
        }

        return LevelSelectionState.SelectedLevelGoal;
    }

    private string GetLevelStatusText()
    {
        string label = GetLevelLabel(currentLevelGoal);
        bool isCompleted = LevelManager.Instance.IsLevelCompleted;
        return isCompleted ? label + completedSuffix : label;
    }

    private string GetLevelLabel(LevelGoalSO levelGoal)
    {
        if (levelGoal == null)
        {
            return "";
        }

        if (useGoalName && !string.IsNullOrWhiteSpace(levelGoal.goalName))
        {
            return levelGoal.goalName;
        }

        if (!string.IsNullOrWhiteSpace(levelGoal.name))
        {
            return levelGoal.name;
        }

        if (!string.IsNullOrWhiteSpace(levelGoal.goalName))
        {
            return levelGoal.goalName;
        }

        return "Level";
    }
    private void TrySubscribeToLevelManager()
    {
        if (isSubscribedToLevelManager || LevelManager.Instance == null)
        {
            return;
        }

        LevelManager.Instance.OnLevelCompleted += RefreshText;
        isSubscribedToLevelManager = true;
    }
}
