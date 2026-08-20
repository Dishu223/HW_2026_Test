using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls in-game HUD displays:
/// live score counter with punch bounce animation and platform countdown bar.
/// (Panel visibility is strictly managed by UIManager).
/// </summary>
public class HUDController : MonoBehaviour
{
    [Header("Score Display")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI highScoreText;

    [Header("Platform Timer Display")]
    [SerializeField] private Image timerFillBar;

    [Header("Timer Colors")]
    [SerializeField] private Color timerFullColor = new Color(0.18f, 0.8f, 0.44f);
    [SerializeField] private Color timerWarnColor = new Color(0.95f, 0.77f, 0.06f);
    [SerializeField] private Color timerCritColor = new Color(0.91f, 0.3f, 0.24f);

    private Coroutine scorePunchRoutine;

    private void OnEnable()
    {
        GameEvents.OnScoreChanged += UpdateScoreDisplay;
        GameEvents.OnPulpitTimerTick += UpdateTimerDisplay;
    }

    private void OnDisable()
    {
        GameEvents.OnScoreChanged -= UpdateScoreDisplay;
        GameEvents.OnPulpitTimerTick -= UpdateTimerDisplay;
    }

    private void Start()
    {
        UpdateScoreDisplay(0);
        if (ScoreManager.Instance != null && highScoreText != null)
        {
            highScoreText.text = $"BEST: {ScoreManager.Instance.HighScore}";
        }
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

    private void UpdateTimerDisplay(float normalizedTime)
    {
        if (timerFillBar != null)
        {
            timerFillBar.fillAmount = normalizedTime;

            if (normalizedTime > 0.5f)
            {
                float t = (1f - normalizedTime) * 2f;
                timerFillBar.color = Color.Lerp(timerFullColor, timerWarnColor, t);
            }
            else
            {
                float t = (0.5f - normalizedTime) * 2f;
                timerFillBar.color = Color.Lerp(timerWarnColor, timerCritColor, t);
            }
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
