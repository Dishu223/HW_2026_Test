using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Handles the Game Over screen with animated score counter,
// high score records, and retry / lobby navigation.
public class GameOverUI : MonoBehaviour
{
    [Header("UI Text Displays")]
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private TextMeshProUGUI highScoreText;
    [SerializeField] private TextMeshProUGUI victoryBannerText;

    [Header("Navigation Buttons")]
    [SerializeField] private Button retryButton;
    [SerializeField] private Button lobbyButton;

    [Header("Victory Celebration")]
    [SerializeField] private GameObject confettiVFX;

    private void Awake()
    {
        if (retryButton != null) retryButton.onClick.AddListener(OnRetryClicked);
        if (lobbyButton != null) lobbyButton.onClick.AddListener(OnLobbyClicked);
    }

    private void OnEnable()
    {
        StartCoroutine(AnimateResults());
    }

    private IEnumerator AnimateResults()
    {
        int targetScore = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0;
        int targetHighScore = ScoreManager.Instance != null ? ScoreManager.Instance.HighScore : 0;

        if (highScoreText != null)
        {
            highScoreText.text = $"Best Score: {targetHighScore}";
        }

        // Check if player completed the 50-Pulpit challenge
        if (victoryBannerText != null)
        {
            bool isVictory = targetScore >= 50;
            victoryBannerText.gameObject.SetActive(isVictory);
            if (isVictory && confettiVFX != null)
            {
                confettiVFX.SetActive(true);
            }
        }

        // Ticker animation: count up from 0 to targetScore
        if (finalScoreText != null)
        {
            float elapsed = 0f;
            float duration = 1.0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                int currentDisplay = (int)Mathf.Lerp(0, targetScore, elapsed / duration);
                finalScoreText.text = $"Pulpits Walked: {currentDisplay}";
                yield return null;
            }

            finalScoreText.text = $"Pulpits Walked: {targetScore}";
        }
    }

    private void OnRetryClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartGame();
        }
    }

    private void OnLobbyClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OpenLobby();
        }
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowLobby();
        }
    }
}
