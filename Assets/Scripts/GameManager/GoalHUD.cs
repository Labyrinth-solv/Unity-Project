using UnityEngine;
using TMPro;
using System.Text;
using System.Collections.Generic;

public class GoalHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text goalText;
    [SerializeField] private float refreshInterval = 0.5f;

    private float nextRefreshTime;
    private StringBuilder sb = new StringBuilder();

    private void Update()
    {
        if (Time.time < nextRefreshTime || LevelManager.Instance == null || goalText == null) return;

        nextRefreshTime = Time.time + refreshInterval;
        RefreshUI();
    }

    private void RefreshUI()
    {
        sb.Clear();
        sb.AppendLine("<color=yellow>LEVEL GOALS:</color>");

        List<LevelGoalSO> goals = LevelManager.Instance.GetActiveGoals();
        foreach (var goal in goals)
        {
            if (goal == null) continue;

            if (!string.IsNullOrWhiteSpace(goal.goalName))
            {
                sb.AppendLine($"<color=yellow>{goal.goalName}</color>");
            }

            foreach (var itemGoal in goal.itemGoals)
            {
                if (itemGoal == null || itemGoal.targetItem == null) continue;

                float currentRate = ProductionRateTracker.GetDeliveryRatePerSecond(itemGoal.targetItem, itemGoal.sampleWindow);
                bool isMet = currentRate >= itemGoal.requiredRatePerSecond;
                string itemName = string.IsNullOrWhiteSpace(itemGoal.targetItem.displayName)
                    ? itemGoal.targetItem.name
                    : itemGoal.targetItem.displayName;

                string color = isMet ? "green" : "white";
                sb.AppendLine($"<color={color}>- {itemName}: {currentRate:F2} / {itemGoal.requiredRatePerSecond:F2} /s</color>");
            }
        }

        goalText.text = sb.ToString();
    }
}
