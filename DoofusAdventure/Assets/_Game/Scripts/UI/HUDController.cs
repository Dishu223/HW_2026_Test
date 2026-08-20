using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls in-game HUD displays (Score counter & High score).
/// Uses CanvasGroup visibility so event listeners are always active.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class HUDController : MonoBehaviour
{
    [Header("Score Display")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI highScoreText;

    private CanvasGroup canvasGroup;
    private Coroutine scorePunchRoutine;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        HideHUD();
    }

    private void OnEnable()
    {
        GameEvents.OnGameStart += ShowHUD;
        GameEvents.OnGameOver += HideHUD;
        GameEvents.OnReturnToLobby += HideHUD;
        GameEvents.OnScoreChanged += UpdateScoreDisplay;
    }

    private void OnDisable()
    {
        GameEvents.OnGameStart -= ShowHUD;
        GameEvents.OnGameOver -= HideHUD;
        GameEvents.OnReturnToLobby -= HideHUD;
        GameEvents.OnScoreChanged -= UpdateScoreDisplay;
    }

    public void ShowHUD()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        UpdateScoreDisplay(0);
    }

    public void HideHUD()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    private void UpdateScoreDisplay(int newScore)
    {
        if (scoreText != null)
        {
            scoreText.text = $"PULPITS: {newScore}";

            if (gameObject.activeInHierarchy && canvasGroup != null && canvasGroup.alpha > 0.5f)
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
