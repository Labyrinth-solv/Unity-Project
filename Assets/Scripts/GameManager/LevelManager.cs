using UnityEngine;
using System.Collections.Generic;
using System.Text;
using PlayFab;
using PlayFab.ClientModels;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Level Score")]
    [SerializeField] private bool submitScoreOnComplete = true;
    [SerializeField] private string statisticName = "Score";
    [SerializeField] private string levelId;

    [SerializeField] private List<LevelGoalSO> activeGoals = new List<LevelGoalSO>();
    
    public event System.Action OnLevelCompleted;
    private bool isLevelCompleted;
    private bool isSubmittingLevelScore;
    public bool IsLevelCompleted => isLevelCompleted;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        if (isLevelCompleted) return;

        if (CheckWinCondition())
        {
            CompleteLevel();
        }
    }

    private bool CheckWinCondition()
    {
        if (activeGoals.Count == 0) return false;

        bool hasItemGoal = false;
        foreach (var goal in activeGoals)
        {
            if (goal == null) continue;

            foreach (var itemGoal in goal.itemGoals)
            {
                if (itemGoal == null || itemGoal.targetItem == null) continue;

                hasItemGoal = true;
                float currentRate = ProductionRateTracker.GetDeliveryRatePerSecond(itemGoal.targetItem, itemGoal.sampleWindow);
                if (currentRate < itemGoal.requiredRatePerSecond)
                {
                    return false;
                }
            }
        }
        return hasItemGoal;
    }

    private void CompleteLevel()
    {
        isLevelCompleted = true;
        Debug.Log("<color=green>Level Completed!</color>");
        SubmitLevelScore();
        OnLevelCompleted?.Invoke();
    }

    private void SubmitLevelScore()
    {
        if (!submitScoreOnComplete)
        {
            return;
        }

        if (isSubmittingLevelScore)
        {
            return;
        }

        int levelScore = CalculateLevelScore();
        if (!PlayFabClientAPI.IsClientLoggedIn())
        {
            Debug.LogWarning("Cannot submit score because the player is not logged in to PlayFab. Level score: " + levelScore);
            return;
        }

        isSubmittingLevelScore = true;
        string completionDataKey = GetLevelCompletionDataKey();

        PlayFabClientAPI.GetUserData(
            new GetUserDataRequest
            {
                Keys = new List<string> { completionDataKey }
            },
            result =>
            {
                if (IsLevelAlreadyCompleted(result, completionDataKey))
                {
                    isSubmittingLevelScore = false;
                    Debug.Log("Level already completed for this player. Score will not be added again: " + completionDataKey);
                    return;
                }

                SubmitScoreForFirstCompletion(levelScore, completionDataKey);
            },
            error =>
            {
                isSubmittingLevelScore = false;
                Debug.LogError("Get level completion state failed: " + error.GenerateErrorReport());
            }
        );
    }

    private void SubmitScoreForFirstCompletion(int levelScore, string completionDataKey)
    {
        PlayFabClientAPI.GetPlayerStatistics(
            new GetPlayerStatisticsRequest(),
            result =>
            {
                int currentScore = GetCurrentScore(result);
                int newScore = currentScore + levelScore;
                SubmitTotalScore(newScore, levelScore, currentScore, completionDataKey);
            },
            error =>
            {
                isSubmittingLevelScore = false;
                Debug.LogError("Get current score failed: " + error.GenerateErrorReport());
            }
        );
    }

    private int GetCurrentScore(GetPlayerStatisticsResult result)
    {
        if (result?.Statistics == null)
        {
            return 0;
        }

        foreach (var statistic in result.Statistics)
        {
            if (statistic.StatisticName == statisticName)
            {
                return statistic.Value;
            }
        }

        return 0;
    }

    private void SubmitTotalScore(int totalScore, int levelScore, int previousScore, string completionDataKey)
    {
        var request = new UpdatePlayerStatisticsRequest
        {
            Statistics = new List<StatisticUpdate>
            {
                new StatisticUpdate
                {
                    StatisticName = statisticName,
                    Value = totalScore
                }
            }
        };

        PlayFabClientAPI.UpdatePlayerStatistics(
            request,
            result => MarkLevelCompleted(completionDataKey, totalScore, levelScore, previousScore),
            error =>
            {
                isSubmittingLevelScore = false;
                Debug.LogError("Submit level score failed: " + error.GenerateErrorReport());
            }
        );
    }

    private void MarkLevelCompleted(string completionDataKey, int totalScore, int levelScore, int previousScore)
    {
        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                { completionDataKey, "true" }
            }
        };

        PlayFabClientAPI.UpdateUserData(
            request,
            result =>
            {
                isSubmittingLevelScore = false;
                Debug.Log("Submitted total score: " + totalScore + " (" + previousScore + " + " + levelScore + ")");
            },
            error =>
            {
                isSubmittingLevelScore = false;
                Debug.LogError("Mark level completed failed: " + error.GenerateErrorReport());
            }
        );
    }

    private bool IsLevelAlreadyCompleted(GetUserDataResult result, string completionDataKey)
    {
        return result?.Data != null
            && result.Data.TryGetValue(completionDataKey, out UserDataRecord record)
            && record != null
            && string.Equals(record.Value, "true", System.StringComparison.OrdinalIgnoreCase);
    }

    private string GetLevelCompletionDataKey()
    {
        return "CompletedLevel_" + SanitizeDataKey(GetLevelCompletionId());
    }

    public static string GetCompletionDataKey(LevelGoalSO levelGoal)
    {
        if (levelGoal == null)
        {
            return string.Empty;
        }

        return "CompletedLevel_" + SanitizeDataKeyValue(levelGoal.name);
    }

    private string GetLevelCompletionId()
    {
        if (!string.IsNullOrWhiteSpace(levelId))
        {
            return levelId;
        }

        if (activeGoals.Count == 1 && activeGoals[0] != null)
        {
            return activeGoals[0].name;
        }

        StringBuilder builder = new StringBuilder();

        foreach (var goal in activeGoals)
        {
            if (goal == null) continue;

            if (builder.Length > 0)
            {
                builder.Append("_");
            }

            builder.Append(goal.name);
        }

        if (builder.Length == 0)
        {
            return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        }

        return builder.ToString();
    }

    private string SanitizeDataKey(string value)
    {
        return SanitizeDataKeyValue(value);
    }

    private static string SanitizeDataKeyValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Level";
        }

        StringBuilder builder = new StringBuilder();

        foreach (char character in value)
        {
            if (char.IsLetterOrDigit(character) || character == '_' || character == '-')
            {
                builder.Append(character);
            }
            else
            {
                builder.Append('_');
            }
        }

        return builder.ToString();
    }

    public int CalculateLevelScore()
    {
        int totalScore = 0;
        foreach (var goal in activeGoals)
        {
            if (goal == null) continue;
            totalScore += Mathf.Max(0, goal.score);
        }

        return totalScore;
    }

    public void AddGoal(LevelGoalSO goal)
    {
        if (goal != null && !activeGoals.Contains(goal))
            activeGoals.Add(goal);
    }

    public void ClearGoals()
    {
        activeGoals.Clear();
        isLevelCompleted = false;
        isSubmittingLevelScore = false;
    }

    public List<LevelGoalSO> GetActiveGoals() => activeGoals;
}
