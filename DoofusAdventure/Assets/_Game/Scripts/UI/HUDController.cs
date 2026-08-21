using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Retro Arcade In-Game HUD Controller:
/// - Chunky Arcade Marquee Score Box (Dark Glass with Neon Border & Drop Shadow)
/// - Clean Single-Line Segmented Sand Battery for Rewinds (Zero line-wrapping, zero emojis)
/// - Automatically initializes RewindGlitchFX screen overlay
/// - Fully respects manual Scene View positioning while providing stylized backings!
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

    private Image scoreMarqueeBadge;
    private Image rewindBadge;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (GetComponent<RewindGlitchFX>() == null)
        {
            gameObject.AddComponent<RewindGlitchFX>();
        }

        StyleArcadeMarqueeBoxes();
        HideHUD();
    }

    private void StyleArcadeMarqueeBoxes()
    {
        // 1. Style Score Marquee Box
        if (scoreText != null)
        {
            scoreText.enableWordWrapping = false;
            scoreText.overflowMode = TextOverflowModes.Overflow;
            scoreText.alignment = TextAlignmentOptions.Center;

            // Ensure width is generous so text never clips
            RectTransform scoreRT = scoreText.rectTransform;
            if (scoreRT.sizeDelta.x < 240f) scoreRT.sizeDelta = new Vector2(260f, 50f);

            // Add or style dark-glass marquee backing behind Score
            Transform existingBadge = scoreText.transform.Find("Marquee_Backing");
            if (existingBadge == null)
            {
                GameObject bgObj = new GameObject("Marquee_Backing");
                bgObj.transform.SetParent(scoreText.transform, false);
                bgObj.transform.SetAsFirstSibling();

                RectTransform bgRT = bgObj.AddComponent<RectTransform>();
                bgRT.anchorMin = Vector2.zero;
                bgRT.anchorMax = Vector2.one;
                bgRT.sizeDelta = new Vector2(36f, 16f); // Generous padding

                scoreMarqueeBadge = bgObj.AddComponent<Image>();
                scoreMarqueeBadge.color = new Color(0.04f, 0.07f, 0.12f, 0.88f); // Dark glass
                scoreMarqueeBadge.raycastTarget = false;

                // Add subtle neon border outline
                Outline outline = bgObj.AddComponent<Outline>();
                outline.effectColor = new Color(1f, 0.85f, 0f, 0.65f); // Neon Gold outline
                outline.effectDistance = new Vector2(2f, -2f);
            }
        }

        // 2. Style Rewind Charges Box
        if (rewindChargesText != null)
        {
            rewindChargesText.enableWordWrapping = false;
            rewindChargesText.overflowMode = TextOverflowModes.Overflow;

            RectTransform rewRT = rewindChargesText.rectTransform;
            if (rewRT.sizeDelta.x < 280f) rewRT.sizeDelta = new Vector2(300f, 45f);

            Transform existingRewBg = rewindChargesText.transform.Find("Rewind_Backing");
            if (existingRewBg == null)
            {
                GameObject bgObj = new GameObject("Rewind_Backing");
                bgObj.transform.SetParent(rewindChargesText.transform, false);
                bgObj.transform.SetAsFirstSibling();

                RectTransform bgRT = bgObj.AddComponent<RectTransform>();
                bgRT.anchorMin = Vector2.zero;
                bgRT.anchorMax = Vector2.one;
                bgRT.sizeDelta = new Vector2(32f, 14f);

                rewindBadge = bgObj.AddComponent<Image>();
                rewindBadge.color = new Color(0.04f, 0.07f, 0.12f, 0.85f);
                rewindBadge.raycastTarget = false;

                Outline outline = bgObj.AddComponent<Outline>();
                outline.effectColor = new Color(0f, 0.85f, 1f, 0.55f); // Neon Cyan outline
                outline.effectDistance = new Vector2(2f, -2f);
            }
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
            // Chunky Arcade Marquee Styling with Neon Gold and Crisp White
            scoreText.text = $"<color=#FFE600>SCORE</color>  <color=#FFFFFF>{newScore}</color>";

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

        // Clean Segmented Battery Blocks (Single line guaranteed, no wrapping!)
        string batterySegments = "";
        for (int i = 0; i < max; i++)
        {
            if (i < current)
                batterySegments += "<color=#00E5FF>■</color> ";
            else
                batterySegments += "<color=#334155>▪</color> ";
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
