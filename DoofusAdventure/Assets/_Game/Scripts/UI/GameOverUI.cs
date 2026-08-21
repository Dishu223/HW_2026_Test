using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Self-contained Game Over & Victory Celebration Screen with Endless Mode continuation and restart triggers.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class GameOverUI : MonoBehaviour
{
    [Header("Score Displays")]
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private TextMeshProUGUI bestScoreText;
    [SerializeField] private TextMeshProUGUI victoryBannerText;
    [SerializeField] private TextMeshProUGUI restartPromptText;

    private CanvasGroup canvasGroup;
    private Coroutine scoreTickerRoutine;
    private float activationCooldown = 0.5f;
    private float timer = 0f;
    private bool isPanelActive = false;
    private bool isVictoryMode = false;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        HidePanelImmediate();
    }

    private void OnEnable()
    {
        GameEvents.OnGameOver += HandleGameOver;
        GameEvents.OnGameVictory += HandleGameVictory;
        GameEvents.OnGameStart += HandleGameStart;
        GameEvents.OnGameRestart += HandleGameStart;
    }

    private void OnDisable()
    {
        GameEvents.OnGameOver -= HandleGameOver;
        GameEvents.OnGameVictory -= HandleGameVictory;
        GameEvents.OnGameStart -= HandleGameStart;
        GameEvents.OnGameRestart -= HandleGameStart;
    }

    private void HandleGameOver()
    {
        isVictoryMode = false;
        ShowGameOver();
    }

    private void HandleGameVictory()
    {
        isVictoryMode = true;
        ShowVictory();
    }

    private void HandleGameStart()
    {
        HidePanelImmediate();
    }

    public void ShowVictory()
    {
        isPanelActive = true;
        timer = 0f;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        int score = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 5;
        int goal = ScoreManager.Instance != null ? ScoreManager.Instance.TargetGoal : 5;

        if (victoryBannerText != null)
        {
            victoryBannerText.gameObject.SetActive(true);
            victoryBannerText.text = $"*** VICTORY! {goal} PULPITS CONQUERED! ***";
        }

        if (finalScoreText != null)
        {
            finalScoreText.text = $"GOAL COMPLETE!\n<size=80>{score}</size>";
        }

        if (bestScoreText != null)
        {
            int best = ScoreManager.Instance != null ? ScoreManager.Instance.HighScore : score;
            bestScoreText.text = $"BEST: {best}";
        }

        if (restartPromptText != null)
        {
            restartPromptText.text = ">> PRESS SPACE TO CONTINUE (ENDLESS MODE) <<\n<size=20>[R] RESTART RUN</size>";
        }
    }

    public void ShowGameOver()
    {
        isPanelActive = true;
        timer = 0f;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        if (restartPromptText != null)
        {
            restartPromptText.text = ">> PRESS R OR SPACE TO RETRY <<";
        }

        DisplayGameOverResults();
    }

    public void HidePanelImmediate()
    {
        isPanelActive = false;
        isVictoryMode = false;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    private void Update()
    {
        if (!isPanelActive) return;

        timer += Time.unscaledDeltaTime;
        if (timer < activationCooldown) return;

        if (restartPromptText != null)
        {
            restartPromptText.alpha = 0.5f + Mathf.PingPong(Time.unscaledTime * 2f, 0.5f);
        }

        bool spacePressed = Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame);
        bool rPressed = Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
        bool mouseClicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

        if (isVictoryMode)
        {
            // In victory screen: Space or click continues into Endless Mode!
            if (spacePressed || mouseClicked)
            {
                ContinueEndlessMode();
            }
            else if (rPressed)
            {
                RestartGame();
            }
        }
        else
        {
            // In Game Over: Space, R, or click restarts
            if (rPressed || spacePressed || mouseClicked)
            {
                RestartGame();
            }
        }
    }

    private void ContinueEndlessMode()
    {
        // Smoothly hide victory panel so player continues endless run!
        HidePanelImmediate();
    }

    public void DisplayGameOverResults()
    {
        int score = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0;
        int best = ScoreManager.Instance != null ? ScoreManager.Instance.HighScore : score;
        int goal = ScoreManager.Instance != null ? ScoreManager.Instance.TargetGoal : 5;

        if (bestScoreText != null)
            bestScoreText.text = $"BEST: {best}";

        if (victoryBannerText != null)
        {
            bool isWon = score >= goal;
            victoryBannerText.gameObject.SetActive(isWon);
            if (isWon)
                victoryBannerText.text = $"*** {goal} PULPITS REACHED! CHALLENGE COMPLETE! ***";
        }

        if (scoreTickerRoutine != null) StopCoroutine(scoreTickerRoutine);
        scoreTickerRoutine = StartCoroutine(ScoreTickerCoroutine(score));
    }

    private IEnumerator ScoreTickerCoroutine(int targetScore)
    {
        float duration = 0.8f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            int displayScore = Mathf.RoundToInt(Mathf.Lerp(0, targetScore, elapsed / duration));
            if (finalScoreText != null)
                finalScoreText.text = $"PULPITS WALKED\n<size=80>{displayScore}</size>";
            yield return null;
        }

        if (finalScoreText != null)
            finalScoreText.text = $"PULPITS WALKED\n<size=80>{targetScore}</size>";
    }

    public void RestartGame()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartGame();
        }
        else
        {
            GameEvents.TriggerGameRestart();
        }
    }
}
