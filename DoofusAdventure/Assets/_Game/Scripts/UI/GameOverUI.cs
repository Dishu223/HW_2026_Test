using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Self-contained Game Over screen with robust Restart triggers and score tallying.
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
    private bool isGameOverActive = false;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        HideGameOverImmediate();
    }

    private void OnEnable()
    {
        GameEvents.OnGameOver += HandleGameOver;
        GameEvents.OnGameStart += HandleGameStart;
        GameEvents.OnGameRestart += HandleGameStart;
    }

    private void OnDisable()
    {
        GameEvents.OnGameOver -= HandleGameOver;
        GameEvents.OnGameStart -= HandleGameStart;
        GameEvents.OnGameRestart -= HandleGameStart;
    }

    private void HandleGameOver()
    {
        ShowGameOver();
    }

    private void HandleGameStart()
    {
        HideGameOverImmediate();
    }

    public void ShowGameOver()
    {
        isGameOverActive = true;
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

    public void HideGameOverImmediate()
    {
        isGameOverActive = false;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    private void Update()
    {
        if (!isGameOverActive) return;

        timer += Time.unscaledDeltaTime;
        if (timer < activationCooldown) return;

        if (restartPromptText != null)
        {
            restartPromptText.alpha = 0.5f + Mathf.PingPong(Time.unscaledTime * 2f, 0.5f);
        }

        bool rPressed = Keyboard.current != null && (Keyboard.current.rKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame);
        bool mouseClicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

        if (rPressed || mouseClicked)
        {
            RestartGame();
        }
    }

    public void DisplayGameOverResults()
    {
        int score = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0;
        int best = ScoreManager.Instance != null ? ScoreManager.Instance.HighScore : score;
        int goal = ScoreManager.Instance != null ? ScoreManager.Instance.TargetGoal : 50;

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
