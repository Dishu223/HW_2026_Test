using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Premium In-Game HUD Controller:
/// - Displays SCORE and BEST Score (Top-Left)
/// - Displays Sand of Time Rewind Charges Capsule (Top-Right)
/// - Automatically removes on-screen timer bar (using the on-tile floating timer instead)
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

        FormatPremiumHUDLayout();
        HideHUD();
    }

    private void FormatPremiumHUDLayout()
    {
        // 1. Hide redundant timer bar background
        Transform timerBg = transform.Find("TimerBar_Background");
        if (timerBg != null) timerBg.gameObject.SetActive(false);

        // 2. Position Score in Top-Left
        if (scoreText != null)
        {
            RectTransform rt = scoreText.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(35f, -30f);
            rt.sizeDelta = new Vector2(300f, 50f);
            scoreText.alignment = TextAlignmentOptions.TopLeft;
            scoreText.fontSize = 32f;
            scoreText.fontStyle = FontStyles.Bold;
        }

        // 3. Position Rewind Charges in Top-Right
        if (rewindChargesText != null)
        {
            RectTransform rt = rewindChargesText.rectTransform;
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-35f, -30f);
            rt.sizeDelta = new Vector2(300f, 50f);
            rewindChargesText.alignment = TextAlignmentOptions.TopRight;
            rewindChargesText.fontSize = 26f;
            rewindChargesText.fontStyle = FontStyles.Bold;
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
            scoreText.text = $"SCORE: {newScore}";

            if (gameObject.activeInHierarchy && canvasGroup != null && canvasGroup.alpha > 0.5f)
            {
                if (scorePunchRoutine != null) StopCoroutine(scorePunchRoutine);
                scorePunchRoutine = StartCoroutine(PunchScale(scoreText.transform, 1.25f, 0.18f));
            }
        }

        if (highScoreText != null && ScoreManager.Instance != null)
        {
            highScoreText.text = $"BEST: {ScoreManager.Instance.HighScore}";
        }
    }

    private void UpdateChargesDisplay(int current, int max)
    {
        if (rewindChargesText == null) return;

        string chargesDisplay = "";
        for (int i = 0; i < max; i++)
        {
            if (i < current)
                chargesDisplay += "<color=#00E5FF>◆</color> ";
            else
                chargesDisplay += "<color=#444444>◆</color> ";
        }

        rewindChargesText.text = $"REWIND: {chargesDisplay.Trim()}";
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
