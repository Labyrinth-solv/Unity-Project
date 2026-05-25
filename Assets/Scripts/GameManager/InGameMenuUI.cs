using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class InGameMenuUI : MonoBehaviour
{
    private class LevelButtonView
    {
        public Button Button;
        public LevelGoalSO LevelGoal;
    }

    [Header("Panels")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject levelSelectedPanel;

    [Header("Buttons")]
    [SerializeField] private Button settingButton;
    [SerializeField] private Button levelsButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private Button closeLevelSelectedButton;

    [Header("Level Buttons")]
    [SerializeField] private Transform levelButtonContainer;
    [SerializeField] private Button levelButtonPrefab;
    [SerializeField] private List<LevelGoalSO> availableLevels = new List<LevelGoalSO>();
    [SerializeField] private Color normalLevelButtonColor = Color.white;
    [SerializeField] private Color completedLevelButtonColor = new Color(0.45f, 0.9f, 0.55f, 1f);

    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "StartMenu";
    [SerializeField] private string gameSceneName = "Main";

    private readonly List<Button> spawnedLevelButtons = new List<Button>();
    private readonly List<LevelButtonView> levelButtonViews = new List<LevelButtonView>();
    private readonly HashSet<string> completedLevelKeys = new HashSet<string>();

    private void Start()
    {
        HideMenus();
        GenerateLevelButtons();
    }

    private void OnEnable()
    {
        if (settingButton != null) settingButton.onClick.AddListener(OpenSettingsPanel);
        if (levelsButton != null) levelsButton.onClick.AddListener(OpenLevelSelectedPanel);
        if (restartButton != null) restartButton.onClick.AddListener(RestartLevel);
        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (exitButton != null) exitButton.onClick.AddListener(ExitToMainMenu);
        if (closeLevelSelectedButton != null) closeLevelSelectedButton.onClick.AddListener(CloseLevelSelectedPanel);
        if (LevelManager.Instance != null) LevelManager.Instance.OnLevelCompleted += RefreshLevelButtonColors;
    }

    private void OnDisable()
    {
        if (settingButton != null) settingButton.onClick.RemoveListener(OpenSettingsPanel);
        if (levelsButton != null) levelsButton.onClick.RemoveListener(OpenLevelSelectedPanel);
        if (restartButton != null) restartButton.onClick.RemoveListener(RestartLevel);
        if (resumeButton != null) resumeButton.onClick.RemoveListener(Resume);
        if (exitButton != null) exitButton.onClick.RemoveListener(ExitToMainMenu);
        if (closeLevelSelectedButton != null) closeLevelSelectedButton.onClick.RemoveListener(CloseLevelSelectedPanel);
        if (LevelManager.Instance != null) LevelManager.Instance.OnLevelCompleted -= RefreshLevelButtonColors;
    }

    public void OpenSettingsPanel()
    {
        SetSettingsPanelActive(true);
        SetLevelSelectedPanelActive(false);
        Time.timeScale = 0f;
    }

    public void OpenLevelSelectedPanel()
    {
        if (spawnedLevelButtons.Count == 0)
        {
            GenerateLevelButtons();
        }

        RefreshLevelButtonColors();
        SetSettingsPanelActive(false);
        SetLevelSelectedPanelActive(true);
        Time.timeScale = 0f;
    }

    public void BackToSettingsPanel()
    {
        SetLevelSelectedPanelActive(false);
        SetSettingsPanelActive(true);
        Time.timeScale = 0f;
    }

    public void CloseLevelSelectedPanel()
    {
        BackToSettingsPanel();
    }

    public void Resume()
    {
        SetSettingsPanelActive(false);
        SetLevelSelectedPanelActive(false);
        Time.timeScale = 1f;
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void SelectLevel(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("Level scene name is empty.");
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    public void SelectLevel(int sceneBuildIndex)
    {
        if (sceneBuildIndex < 0 || sceneBuildIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogWarning("Level scene build index is invalid: " + sceneBuildIndex);
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneBuildIndex);
    }

    public void SelectLevel(LevelGoalSO levelGoal)
    {
        if (levelGoal == null)
        {
            Debug.LogWarning("Level goal is missing.");
            return;
        }

        LevelSelectionState.SelectLevel(levelGoal);
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    public void ExitToMainMenu()
    {
        LevelSelectionState.ClearSelection();
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void ExitGame()
    {
        Time.timeScale = 1f;
        Debug.Log("Quit Game");
        Application.Quit();
    }

    public void GenerateLevelButtons()
    {
        if (levelButtonPrefab == null)
        {
            Debug.LogWarning("Level button prefab is missing.");
            return;
        }

        Transform container = levelButtonContainer;

        if (container == null && levelSelectedPanel != null)
        {
            container = levelSelectedPanel.transform;
        }

        if (container == null)
        {
            Debug.LogWarning("Level button container is missing.");
            return;
        }

        ClearGeneratedLevelButtons();

        if (availableLevels.Count == 0)
        {
            Debug.LogWarning("Available levels list is empty.");
            return;
        }

        for (int i = 0; i < availableLevels.Count; i++)
        {
            LevelGoalSO levelGoal = availableLevels[i];

            if (levelGoal == null)
            {
                continue;
            }

            Button button = Instantiate(levelButtonPrefab, container);
            button.gameObject.SetActive(true);

            SetLevelButtonLabel(button.gameObject, GetLevelLabel(levelGoal, i));

            LevelGoalSO targetLevelGoal = levelGoal;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SelectLevel(targetLevelGoal));

            spawnedLevelButtons.Add(button);
            levelButtonViews.Add(new LevelButtonView
            {
                Button = button,
                LevelGoal = levelGoal
            });
        }

        RefreshLevelButtonColors();
    }

    private string GetLevelLabel(LevelGoalSO levelGoal, int levelIndex)
    {
        if (!string.IsNullOrWhiteSpace(levelGoal.name))
        {
            return levelGoal.name;
        }

        if (!string.IsNullOrWhiteSpace(levelGoal.goalName))
        {
            return levelGoal.goalName;
        }

        return "Level " + (levelIndex + 1);
    }

    private void SetLevelButtonLabel(GameObject buttonObject, string label)
    {
        TMP_Text tmpText = buttonObject.GetComponentInChildren<TMP_Text>();

        if (tmpText != null)
        {
            tmpText.text = label;
            return;
        }

        Text uiText = buttonObject.GetComponentInChildren<Text>();

        if (uiText != null)
        {
            uiText.text = label;
        }
    }

    public void RefreshLevelButtonColors()
    {
        MarkCurrentLevelCompletedIfNeeded();
        ApplyLevelButtonColors();
        LoadCompletedLevelsFromPlayFab();
    }

    private void LoadCompletedLevelsFromPlayFab()
    {
        if (!PlayFabClientAPI.IsClientLoggedIn() || levelButtonViews.Count == 0)
        {
            return;
        }

        List<string> keys = new List<string>();
        HashSet<string> requestedKeys = new HashSet<string>();

        foreach (LevelButtonView view in levelButtonViews)
        {
            string key = LevelManager.GetCompletionDataKey(view.LevelGoal);
            if (!string.IsNullOrEmpty(key) && requestedKeys.Add(key))
            {
                keys.Add(key);
            }
        }

        if (keys.Count == 0)
        {
            return;
        }

        PlayFabClientAPI.GetUserData(
            new GetUserDataRequest { Keys = keys },
            result =>
            {
                completedLevelKeys.Clear();
                MarkCurrentLevelCompletedIfNeeded();

                if (result?.Data != null)
                {
                    foreach (KeyValuePair<string, UserDataRecord> entry in result.Data)
                    {
                        if (entry.Value != null && string.Equals(entry.Value.Value, "true", System.StringComparison.OrdinalIgnoreCase))
                        {
                            completedLevelKeys.Add(entry.Key);
                        }
                    }
                }

                ApplyLevelButtonColors();
            },
            error => Debug.LogError("Load completed level colors failed: " + error.GenerateErrorReport())
        );
    }

    private void MarkCurrentLevelCompletedIfNeeded()
    {
        if (LevelManager.Instance == null || !LevelManager.Instance.IsLevelCompleted)
        {
            return;
        }

        List<LevelGoalSO> activeGoals = LevelManager.Instance.GetActiveGoals();
        if (activeGoals == null)
        {
            return;
        }

        foreach (LevelGoalSO activeGoal in activeGoals)
        {
            string key = LevelManager.GetCompletionDataKey(activeGoal);
            if (!string.IsNullOrEmpty(key))
            {
                completedLevelKeys.Add(key);
            }
        }
    }

    private void ApplyLevelButtonColors()
    {
        foreach (LevelButtonView view in levelButtonViews)
        {
            if (view == null || view.Button == null)
            {
                continue;
            }

            Image image = view.Button.GetComponent<Image>();
            if (image == null)
            {
                continue;
            }

            string key = LevelManager.GetCompletionDataKey(view.LevelGoal);
            bool isCompleted = !string.IsNullOrEmpty(key) && completedLevelKeys.Contains(key);
            image.color = isCompleted ? completedLevelButtonColor : normalLevelButtonColor;
        }
    }

    private void ClearGeneratedLevelButtons()
    {
        foreach (Button button in spawnedLevelButtons)
        {
            if (button != null)
            {
                Destroy(button.gameObject);
            }
        }

        spawnedLevelButtons.Clear();
        levelButtonViews.Clear();
    }

    private void HideMenus()
    {
        SetSettingsPanelActive(false);
        SetLevelSelectedPanelActive(false);
        Time.timeScale = 1f;
    }

    private void SetSettingsPanelActive(bool isActive)
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(isActive);
        }
    }

    private void SetLevelSelectedPanelActive(bool isActive)
    {
        if (levelSelectedPanel != null)
        {
            levelSelectedPanel.SetActive(isActive);
        }
    }
}
