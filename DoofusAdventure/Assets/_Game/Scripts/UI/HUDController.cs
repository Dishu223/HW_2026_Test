using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls in-game HUD displays (Score counter & High score).
/// Activates strictly when gameplay starts, and hides when on menus or game over.
/// </summary>
public class HUDController : MonoBehaviour
{
    [Header("Score Display")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI highScoreText;

    private Coroutine scorePunchRoutine;

    private void Awake()
    {
        // Hide HUD until game starts
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        GameEvents.OnGameStart += HandleGameStart;
        GameEvents.OnGameOver += HandleGameOver;
        GameEvents.OnReturnToLobby += HandleReturnToLobby;
        GameEvents.OnScoreChanged += UpdateScoreDisplay;
    }

    private void OnDisable()
    {
        GameEvents.OnGameStart -= HandleGameStart;
        GameEvents.OnGameOver -= HandleGameOver;
        GameEvents.OnReturnToLobby -= HandleReturnToLobby;
        GameEvents.OnScoreChanged -= UpdateScoreDisplay;
    }

    private void HandleGameStart()
    {
        gameObject.SetActive(true);
        UpdateScoreDisplay(0);
    }

    private void HandleGameOver()
    {
        gameObject.SetActive(false);
    }

    private void HandleReturnToLobby()
    {
        gameObject.SetActive(false);
    }

    private void UpdateScoreDisplay(int newScore)
    {
        if (scoreText != null)
        {
            scoreText.text = $"PULPITS: {newScore}";

            if (gameObject.activeInHierarchy)
            {
                if (scorePunchRoutine != null) StopCoroutine(scorePunchRoutine);
                scorePunchRoutine = StartCoroutine(PunchScale(scoreText.transform, 1.35f, 0.2f));
            }
        }

        if (highScoreText != null && ScoreManager.Instance != null)
        {
            highScoreText.text = $"BEST: {ScoreManager.Instance.HighScore}";
        }
    }

    private IEnumerator PunchScale(Transform target, float punchScale, float duration)
    {
        Vector3 initialScale = Vector3.one;
        Vector3 targetScale = Vector3.one * punchScale;
        float halfDuration = duration / 2f;

        float elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            target.localScale = Vector3.Lerp(initialScale, targetScale, elapsed / halfDuration);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            target.localScale = Vector3.Lerp(targetScale, initialScale, elapsed / halfDuration);
            yield return null;
        }

        target.localScale = initialScale;
    }
}
