using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Self-contained Game Over screen:
/// - Fades in cleanly when GameOver occurs
/// - Protects against accidental immediate restart from held space/mouse keys
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
    private float activationCooldown = 0.6f;
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
    }

    private void OnDisable()
    {
        GameEvents.OnGameOver -= HandleGameOver;
        GameEvents.OnGameStart -= HandleGameStart;
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
        if (timer < activationCooldown) return; // Strict cooldown so held keys don't restart immediately!

        if (restartPromptText != null)
        {
            restartPromptText.alpha = 0.4f + Mathf.PingPong(Time.unscaledTime * 2.5f, 0.6f);
        }

        // Restart only when key was pressed THIS frame after cooldown
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

        if (bestScoreText != null)
            bestScoreText.text = $"BEST: {best}";

        if (victoryBannerText != null)
        {
            victoryBannerText.gameObject.SetActive(score >= 50);
            if (score >= 50)
                victoryBannerText.text = "*** 50 PULPITS REACHED! CHALLENGE COMPLETE! ***";
        }

        if (scoreTickerRoutine != null) StopCoroutine(scoreTickerRoutine);
        scoreTickerRoutine = StartCoroutine(ScoreTickerCoroutine(score));
    }

    private IEnumerator ScoreTickerCoroutine(int targetScore)
    {
        float duration = 1.0f;
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
        HideGameOverImmediate();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetState(GameManager.GameState.Playing);
        }
        else
        {
            GameEvents.TriggerGameStart();
        }
    }
}
