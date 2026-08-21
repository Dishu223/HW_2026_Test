using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Retro Arcade In-Game HUD Controller:
/// - Chunky Arcade Marquee Score Display (Neon Yellow / Cyan with deep dark drop shadow)
/// - Clean Segmented Sand Battery for Rewinds Left (No emojis, 100% font safe)
/// - Automatically initializes RewindGlitchFX screen overlay
/// - Fully respects custom Scene View manual positioning!
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

        // Ensure RewindGlitchFX is attached
        if (GetComponent<RewindGlitchFX>() == null)
        {
            gameObject.AddComponent<RewindGlitchFX>();
        }

        HideHUD();
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
            // Chunky Arcade Marquee Styling with Neon Gold and Shadow
            scoreText.text = $"<color=#FFE600>SCORE</color> <color=#FFFFFF>{newScore}</color>";

            if (gameObject.activeInHierarchy && canvasGroup != null && canvasGroup.alpha > 0.5f)
            {
                if (scorePunchRoutine != null) StopCoroutine(scorePunchRoutine);
                scorePunchRoutine = StartCoroutine(PunchScale(scoreText.transform, 1.25f, 0.18f));
            }
        }

        if (highScoreText != null && ScoreManager.Instance != null)
        {
            highScoreText.text = $"<color=#94A3B8>BEST</color> <color=#00E5FF>{ScoreManager.Instance.HighScore}</color>";
        }
    }

    private void UpdateChargesDisplay(int current, int max)
    {
        if (rewindChargesText == null) return;

        // Clean Segmented Battery Blocks (No emojis, crisp font-safe brackets & bars)
        string batterySegments = "";
        for (int i = 0; i < max; i++)
        {
            if (i < current)
                batterySegments += "<color=#00E5FF>■</color> "; // Charged neon cyan block
            else
                batterySegments += "<color=#334155>▪</color> "; // Spent dark slate block
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
