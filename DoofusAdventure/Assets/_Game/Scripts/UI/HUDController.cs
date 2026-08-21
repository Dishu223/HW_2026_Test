using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Retro Arcade In-Game HUD Controller:
/// - Chunky High-Visibility Neon Score Display (Electric Neon Gold & Crisp White)
/// - Single-Line Segmented Sand Battery for Rewinds (100% font-safe, zero line wrapping)
/// - Pure text rendering with zero blocking overlays for maximum visibility!
/// - Fully respects manual Scene View positioning and styling.
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

        CleanupAndFormatTexts();
        HideHUD();
    }

    private void CleanupAndFormatTexts()
    {
        // 1. Ensure Score Text has zero blocking children and 100% white base color for rich-text tags
        if (scoreText != null)
        {
            Transform oldBacking = scoreText.transform.Find("Marquee_Backing");
            if (oldBacking != null) Destroy(oldBacking.gameObject);

            scoreText.color = Color.white;
            scoreText.enableWordWrapping = false;
            scoreText.overflowMode = TextOverflowModes.Overflow;
            scoreText.alignment = TextAlignmentOptions.Center;
        }

        // 2. Ensure Rewind Charges Text has zero blocking children and single-line layout
        if (rewindChargesText != null)
        {
            Transform oldRewBg = rewindChargesText.transform.Find("Rewind_Backing");
            if (oldRewBg != null) Destroy(oldRewBg.gameObject);

            rewindChargesText.color = Color.white;
            rewindChargesText.enableWordWrapping = false;
            rewindChargesText.overflowMode = TextOverflowModes.Overflow;
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
            // Chunky High-Contrast Neon Styling
            scoreText.text = $"<color=#FFE600>SCORE</color>  <color=#FFFFFF>{newScore}</color>";

            if (gameObject.activeInHierarchy && canvasGroup != null && canvasGroup.alpha > 0.5f)
            {
                if (scorePunchRoutine != null) StopCoroutine(scorePunchRoutine);
                scorePunchRoutine = StartCoroutine(PunchScale(scoreText.transform, 1.25f, 0.18f));
            }
        }

        if (highScoreText != null && ScoreManager.Instance != null)
        {
            highScoreText.text = $"<color=#94A3B8>BEST</color>  <color=#00E5FF>{ScoreManager.Instance.HighScore}</color>";
        }
    }

    private void UpdateChargesDisplay(int current, int max)
    {
        if (rewindChargesText == null) return;

        // Clean Single-Line Segmented Battery
        string batterySegments = "";
        for (int i = 0; i < max; i++)
        {
            if (i < current)
                batterySegments += "<color=#00E5FF>■</color> ";
            else
                batterySegments += "<color=#64748B>▪</color> ";
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
