using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Retro Arcade In-Game HUD Controller:
/// - Crisp, high-visibility Fredoka typography
/// - Guaranteed single-line layout (word-wrapping strictly disabled)
/// - Clean readable Score and Rewind charges display
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class HUDController : MonoBehaviour
{
    [Header("Score Display")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI highScoreText;

    [Header("Rewind Charges Display")]
    [SerializeField] private TextMeshProUGUI rewindChargesText;

    private CanvasGroup canvasGroup;
    private Coroutine scorePunchRoutine;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (GetComponent<RewindGlitchFX>() == null)
        {
            gameObject.AddComponent<RewindGlitchFX>();
        }

        ConfigureTextProperties();
        HideHUD();
    }

    private void Start()
    {
        ConfigureTextProperties();
    }

    private void ConfigureTextProperties()
    {
        if (scoreText != null)
        {
            scoreText.enableWordWrapping = false;
            scoreText.overflowMode = TextOverflowModes.Overflow;
            scoreText.alignment = TextAlignmentOptions.Center;
            scoreText.fontSize = 32f;
            scoreText.color = Color.white;

            RectTransform rt = scoreText.rectTransform;
            if (rt.sizeDelta.x < 350f) rt.sizeDelta = new Vector2(350f, 60f);
        }

        if (highScoreText != null)
        {
            highScoreText.enableWordWrapping = false;
            highScoreText.overflowMode = TextOverflowModes.Overflow;
            highScoreText.fontSize = 20f;
            highScoreText.color = Color.white;
        }

        if (rewindChargesText != null)
        {
            rewindChargesText.enableWordWrapping = false;
            rewindChargesText.overflowMode = TextOverflowModes.Overflow;
            rewindChargesText.alignment = TextAlignmentOptions.Right;
            rewindChargesText.fontSize = 24f;
            rewindChargesText.color = Color.white;

            RectTransform rt = rewindChargesText.rectTransform;
            if (rt.sizeDelta.x < 350f) rt.sizeDelta = new Vector2(350f, 60f);
        }
    }

    private void OnEnable()
    {
        GameEvents.OnGameStart += ShowHUD;
        GameEvents.OnGameOver += HideHUD;
        GameEvents.OnReturnToLobby += HideHUD;
        GameEvents.OnScoreChanged += UpdateScoreDisplay;
        GameEvents.OnRewindChargesChanged += UpdateChargesDisplay;
    }

    private void OnDisable()
    {
        GameEvents.OnGameStart -= ShowHUD;
        GameEvents.OnGameOver -= HideHUD;
        GameEvents.OnReturnToLobby -= HideHUD;
        GameEvents.OnScoreChanged -= UpdateScoreDisplay;
        GameEvents.OnRewindChargesChanged -= UpdateChargesDisplay;
    }

    public void ShowHUD()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        ConfigureTextProperties();
        UpdateScoreDisplay(0);
        UpdateChargesDisplay(3, 3);
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
            // Crisp, high-contrast neon yellow label + bright white score
            scoreText.text = $"<color=#FFE600>SCORE</color> : <color=#FFFFFF>{newScore}</color>";

            if (gameObject.activeInHierarchy && canvasGroup != null && canvasGroup.alpha > 0.5f)
            {
                if (scorePunchRoutine != null) StopCoroutine(scorePunchRoutine);
                scorePunchRoutine = StartCoroutine(PunchScale(scoreText.transform, 1.22f, 0.16f));
            }
        }

        if (highScoreText != null && ScoreManager.Instance != null)
        {
            highScoreText.text = $"<color=#94A3B8>BEST</color> : <color=#00E5FF>{ScoreManager.Instance.HighScore}</color>";
        }
    }

    private void UpdateChargesDisplay(int current, int max)
    {
        if (rewindChargesText == null) return;

        // Clean ASCII Segmented Charges (100% font-safe across all fonts)
        string batterySegments = "";
        for (int i = 0; i < max; i++)
        {
            if (i < current)
                batterySegments += "<color=#00E5FF>/</color> "; // Neon cyan slash
            else
                batterySegments += "<color=#475569>.</color> "; // Dim slate dot
        }

        rewindChargesText.text = $"<color=#00E5FF>REWIND</color>  [ {batterySegments.Trim()} ]";
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
