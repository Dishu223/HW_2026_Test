using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles Game Over Screen: counting-up score ticker, challenge complete banner (score >= 50),
/// best score display, and instant restart on 'R' key.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("Score Displays")]
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private TextMeshProUGUI bestScoreText;
    [SerializeField] private TextMeshProUGUI victoryBannerText;
    [SerializeField] private TextMeshProUGUI restartPromptText;

    private Coroutine scoreTickerRoutine;

    private void OnEnable()
    {
        GameEvents.OnGameOver += DisplayGameOverResults;
    }

    private void OnDisable()
    {
        GameEvents.OnGameOver -= DisplayGameOverResults;
    }

    private void Update()
    {
        // Animate restart prompt
        if (restartPromptText != null)
        {
            restartPromptText.alpha = 0.4f + Mathf.PingPong(Time.unscaledTime * 2.5f, 0.6f);
        }

        // Detect R key to restart or Space to return to lobby
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
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

        // Show special win banner if hit 50 pulpits!
        if (victoryBannerText != null)
        {
            victoryBannerText.gameObject.SetActive(score >= 50);
            if (score >= 50)
                victoryBannerText.text = "🎉 50 PULPITS REACHED! CHALLENGE COMPLETE! 🎉";
        }

        // Start score ticker
        if (gameObject.activeInHierarchy)
        {
            if (scoreTickerRoutine != null) StopCoroutine(scoreTickerRoutine);
            scoreTickerRoutine = StartCoroutine(ScoreTickerCoroutine(score));
        }
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
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetState(GameManager.GameState.Playing);
        }
    }

    public void ReturnToLobby()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetState(GameManager.GameState.Lobby);
        }
    }
}
