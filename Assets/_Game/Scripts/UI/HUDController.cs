using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Manages real-time in-game HUD indicators:
// live score, platform countdown bar, and rewind charges.
public class HUDController : MonoBehaviour
{
    [Header("Score Display")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private float scorePunchScale = 1.3f;

    [Header("Platform Countdown Bar")]
    [SerializeField] private Image timerFillImage;
    [SerializeField] private Color timerNormalColor = new Color(0.2f, 0.9f, 0.3f);
    [SerializeField] private Color timerWarningColor = new Color(1.0f, 0.75f, 0.1f);
    [SerializeField] private Color timerDangerColor = new Color(0.95f, 0.2f, 0.2f);

    [Header("Rewind Counter")]
    [SerializeField] private TextMeshProUGUI rewindText;

    private Vector3 initialScoreScale = Vector3.one;

    private void Awake()
    {
        if (scoreText != null) initialScoreScale = scoreText.transform.localScale;
    }

    private void OnEnable()
    {
        GameEvents.OnScoreChanged += UpdateScoreDisplay;
        GameEvents.OnPulpitTimerTick += UpdateTimerBar;
        GameEvents.OnGameStart += RefreshHUD;
        GameEvents.OnRewindComplete += RefreshHUD;
    }

    private void OnDisable()
    {
        GameEvents.OnScoreChanged -= UpdateScoreDisplay;
        GameEvents.OnPulpitTimerTick -= UpdateTimerBar;
        GameEvents.OnGameStart -= RefreshHUD;
        GameEvents.OnRewindComplete -= RefreshHUD;
    }

    private void RefreshHUD()
    {
        int currentScore = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0;
        UpdateScoreDisplay(currentScore);
        UpdateTimerBar(1f);
        UpdateRewindDisplay();
    }

    private void UpdateScoreDisplay(int newScore)
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {newScore}";
            // Quick punch animation
            scoreText.transform.localScale = initialScoreScale * scorePunchScale;
        }
        UpdateRewindDisplay();
    }

    private void Update()
    {
        // Smoothly return score text to normal scale after punch
        if (scoreText != null && scoreText.transform.localScale != initialScoreScale)
        {
            scoreText.transform.localScale = Vector3.Lerp(scoreText.transform.localScale, initialScoreScale, Time.deltaTime * 10f);
        }
    }

    private void UpdateTimerBar(float normalizedTime)
    {
        if (timerFillImage == null) return;

        timerFillImage.fillAmount = Mathf.Clamp01(normalizedTime);

        // Dynamically shift bar color based on remaining urgency
        if (normalizedTime > 0.5f)
        {
            float t = (normalizedTime - 0.5f) / 0.5f;
            timerFillImage.color = Color.Lerp(timerWarningColor, timerNormalColor, t);
        }
        else
        {
            float t = normalizedTime / 0.5f;
            timerFillImage.color = Color.Lerp(timerDangerColor, timerWarningColor, t);
        }
    }

    private void UpdateRewindDisplay()
    {
        if (rewindText != null && GameManager.Instance != null)
        {
            rewindText.text = $"⏪ {GameManager.Instance.RewindsRemaining}";
        }
    }
}
