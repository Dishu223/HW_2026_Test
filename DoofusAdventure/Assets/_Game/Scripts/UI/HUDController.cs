using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// In-Game HUD Controller:
/// - 100% respects all your custom Inspector settings: Font Size, Alignment, Colors, Positions, Sizes!
/// - Only updates the text string values dynamically during gameplay.
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

        // Remove any old stray RewindGlitchFX canvas
        RewindGlitchFX oldGlitch = GetComponent<RewindGlitchFX>();
        if (oldGlitch != null) Destroy(oldGlitch);

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
            scoreText.text = $"SCORE : {newScore}";

            if (gameObject.activeInHierarchy && canvasGroup != null && canvasGroup.alpha > 0.5f)
            {
                if (scorePunchRoutine != null) StopCoroutine(scorePunchRoutine);
                scorePunchRoutine = StartCoroutine(PunchScale(scoreText.transform, 1.20f, 0.15f));
            }
        }

        if (highScoreText != null && ScoreManager.Instance != null)
        {
            highScoreText.text = $"BEST : {ScoreManager.Instance.HighScore}";
        }
    }

    private void UpdateChargesDisplay(int current, int max)
    {
        if (rewindChargesText == null) return;

        string batterySegments = "";
        for (int i = 0; i < max; i++)
        {
            if (i < current)
                batterySegments += "/ ";
            else
                batterySegments += ". ";
        }

        rewindChargesText.text = $"REWIND [ {batterySegments.Trim()} ]";
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
