using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using PlayFab;
using PlayFab.ClientModels;

public class PlayFabLoginManager : MonoBehaviour
{
    private const float LeaderboardRowHeight = 50f;
    private const float LeaderboardRowSpacing = 8f;

    [Header("PlayFab")]
    [SerializeField] private string titleId = "248E6";
    [SerializeField] private string statisticName = "Score";

    [Header("Panels")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject mainMenuPanel;

    [Header("Input")]
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField passwordInput;

    [Header("UI")]
    [SerializeField] private TMP_Text messageText;

    [Header("Leaderboard UI")]
    [SerializeField] private Transform leaderboardContent;
    [SerializeField] private GameObject leaderboardRowPrefab;
    [SerializeField] private TMP_Text leaderboardMessageText;

    [Header("Scene")]
    [SerializeField] private string gameSceneName = "Main";

    private bool isLoggedIn;

    private void Awake()
    {
        PlayFabSettings.staticSettings.TitleId = titleId;
    }

    private void Start()
    {
        if (IsPlayerLoggedIn())
        {
            isLoggedIn = true;
            ShowMainMenuPanel();
            LoadLeaderboard();
        }
        else
        {
            ShowLoginPanel();
        }
    }

    private void OnApplicationQuit()
    {
        isLoggedIn = false;
        PlayFabClientAPI.ForgetAllCredentials();
    }

    public void Register()
    {
        string username = usernameInput.text.Trim();
        string password = passwordInput.text.Trim();

        if (!ValidateInput(username, password)) return;

        var request = new RegisterPlayFabUserRequest
        {
            Username = username,
            Password = password,
            RequireBothUsernameAndEmail = false,
            DisplayName = username
        };

        PlayFabClientAPI.RegisterPlayFabUser(
            request,
            result =>
            {
                isLoggedIn = true;
                ShowMessage("Đăng ký thành công: " + username);
                ShowMainMenuPanel();

                LoadLeaderboard();
            },
            error =>
            {
                isLoggedIn = false;
                ShowMessage("Đăng ký thất bại: " + error.ErrorMessage);
            }
        );
    }

    public void Login()
    {
        string username = usernameInput.text.Trim();
        string password = passwordInput.text.Trim();

        if (!ValidateInput(username, password)) return;

        var request = new LoginWithPlayFabRequest
        {
            Username = username,
            Password = password
        };

        PlayFabClientAPI.LoginWithPlayFab(
            request,
            result =>
            {
                isLoggedIn = true;
                ShowMessage("Đăng nhập thành công: " + username);
                ShowMainMenuPanel();

                LoadLeaderboard();
            },
            error =>
            {
                isLoggedIn = false;
                ShowMessage("Đăng nhập thất bại: " + error.ErrorMessage);
            }
        );
    }

    public void StartGame()
    {
        if (!IsPlayerLoggedIn())
        {
            ShowMessage("Bạn cần đăng nhập trước khi bắt đầu.");
            ShowLoginPanel();
            return;
        }

        SceneManager.LoadScene(gameSceneName);
    }

    public void BackToLogin()
    {
        Logout();
    }

    public void Logout()
    {
        isLoggedIn = false;
        PlayFabClientAPI.ForgetAllCredentials();

        if (usernameInput != null)
        {
            usernameInput.text = "";
        }

        if (passwordInput != null)
        {
            passwordInput.text = "";
        }

        ShowMessage("Logged out.");
        ShowLoginPanel();
    }

    public void LoadLeaderboard()
    {
        ClearLeaderboardRows();

        if (leaderboardMessageText != null)
        {
            leaderboardMessageText.text = "Đang tải bảng xếp hạng...";
        }

        var request = new GetLeaderboardRequest
        {
            StatisticName = statisticName,
            StartPosition = 0,
            MaxResultsCount = 10,
            ProfileConstraints = new PlayerProfileViewConstraints
            {
                ShowDisplayName = true
            }
        };

        PlayFabClientAPI.GetLeaderboard(
            request,
            result =>
            {
                if (leaderboardMessageText != null)
                {
                    leaderboardMessageText.text = "";
                }

                if (result.Leaderboard == null || result.Leaderboard.Count == 0)
                {
                    if (leaderboardMessageText != null)
                    {
                        leaderboardMessageText.text = "Chưa có dữ liệu bảng xếp hạng.";
                    }

                    return;
                }

                for (int i = 0; i < result.Leaderboard.Count; i++)
                {
                    var item = result.Leaderboard[i];
                    string playerName = item.DisplayName;

                    if (string.IsNullOrEmpty(playerName))
                    {
                        playerName = item.PlayFabId;
                    }

                    CreateLeaderboardRow(
                        item.Position + 1,
                        playerName,
                        item.StatValue,
                        i
                    );
                }
            },
            error =>
            {
                if (leaderboardMessageText != null)
                {
                    leaderboardMessageText.text = "Không tải được bảng xếp hạng.";
                }

                Debug.LogError("Load leaderboard failed: " + error.GenerateErrorReport());
            }
        );
    }

    public void SubmitScore(int score)
    {
        if (!IsPlayerLoggedIn())
        {
            Debug.LogWarning("Chưa đăng nhập nên không thể gửi điểm.");
            return;
        }

        var request = new UpdatePlayerStatisticsRequest
        {
            Statistics = new List<StatisticUpdate>
            {
                new StatisticUpdate
                {
                    StatisticName = statisticName,
                    Value = score
                }
            }
        };

        PlayFabClientAPI.UpdatePlayerStatistics(
            request,
            result =>
            {
                Debug.Log("Gửi điểm thành công: " + score);
                LoadLeaderboard();
            },
            error =>
            {
                Debug.LogError("Gửi điểm thất bại: " + error.GenerateErrorReport());
            }
        );
    }

    private void CreateLeaderboardRow(int rank, string playerName, int score, int rowIndex)
    {
        if (leaderboardContent == null || leaderboardRowPrefab == null)
        {
            Debug.LogWarning("Thiếu leaderboardContent hoặc leaderboardRowPrefab.");
            return;
        }

        GameObject rowObject = Instantiate(leaderboardRowPrefab, leaderboardContent);
        RectTransform rowRectTransform = rowObject.GetComponent<RectTransform>();

        if (rowRectTransform != null)
        {
            float rowStep = LeaderboardRowHeight + LeaderboardRowSpacing;
            Vector2 anchoredPosition = rowRectTransform.anchoredPosition;
            Vector2 sizeDelta = rowRectTransform.sizeDelta;

            rowRectTransform.anchoredPosition = new Vector2(
                anchoredPosition.x,
                anchoredPosition.y - rowIndex * rowStep
            );
            rowRectTransform.sizeDelta = new Vector2(sizeDelta.x, LeaderboardRowHeight);

            RectTransform contentRectTransform = leaderboardContent as RectTransform;

            if (contentRectTransform != null)
            {
                contentRectTransform.sizeDelta = new Vector2(
                    contentRectTransform.sizeDelta.x,
                    Mathf.Abs(anchoredPosition.y) + LeaderboardRowHeight + rowIndex * rowStep
                );
            }
        }

        TMP_Text[] texts = rowObject.GetComponentsInChildren<TMP_Text>();

        foreach (TMP_Text text in texts)
        {
            if (text.name == "RankText")
            {
                text.text = rank.ToString();
            }
            else if (text.name == "NameText")
            {
                text.text = playerName;
            }
            else if (text.name == "ScoreText")
            {
                text.text = score.ToString();
            }
        }
    }

    private void ClearLeaderboardRows()
    {
        if (leaderboardContent == null) return;

        foreach (Transform child in leaderboardContent)
        {
            Destroy(child.gameObject);
        }

        RectTransform contentRectTransform = leaderboardContent as RectTransform;

        if (contentRectTransform != null)
        {
            contentRectTransform.sizeDelta = new Vector2(contentRectTransform.sizeDelta.x, 0f);
        }
    }

    private void ShowLoginPanel()
    {
        if (loginPanel != null)
            loginPanel.SetActive(true);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);
    }

    private void ShowMainMenuPanel()
    {
        if (loginPanel != null)
            loginPanel.SetActive(false);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
    }

    private bool ValidateInput(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            ShowMessage("Vui lòng nhập username.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            ShowMessage("Vui lòng nhập password.");
            return false;
        }

        if (password.Length < 6)
        {
            ShowMessage("Password cần ít nhất 6 ký tự.");
            return false;
        }

        return true;
    }

    private void ShowMessage(string message)
    {
        Debug.Log(message);

        if (messageText != null)
        {
            messageText.text = message;
        }
    }

    private bool IsPlayerLoggedIn()
    {
        return isLoggedIn || PlayFabClientAPI.IsClientLoggedIn();
    }
}
