using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the Prince of Persia Time Rewind & Resume overlay:
/// - Solid, static, non-flashing typography
/// - Soft ambient static vignette
/// - Uses 100% universal ASCII / Standard Unicode symbols (no emoji missing glyph boxes)
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class RewindScreenUI : MonoBehaviour
{
    [Header("UI Visual Elements")]
    [SerializeField] private Image vignetteOverlayImage;
    [SerializeField] private TextMeshProUGUI rewindBannerText;

    private CanvasGroup canvasGroup;
    private bool isOverlayActive = false;
    private float fadeSpeed = 6f;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        HideImmediate();
    }

    private void OnEnable()
    {
        GameEvents.OnRewindStart += HandleRewindStart;
        GameEvents.OnRewindReadyToResume += HandleReadyToResume;
        GameEvents.OnRewindComplete += HandleRewindComplete;
        GameEvents.OnGameStart += HideImmediate;
        GameEvents.OnGameOver += HideImmediate;
    }

    private void OnDisable()
    {
        GameEvents.OnRewindStart -= HandleRewindStart;
        GameEvents.OnRewindReadyToResume -= HandleReadyToResume;
        GameEvents.OnRewindComplete -= HandleRewindComplete;
        GameEvents.OnGameStart -= HideImmediate;
        GameEvents.OnGameOver -= HideImmediate;
    }

    private void HandleRewindStart()
    {
        isOverlayActive = true;
        if (rewindBannerText != null)
        {
            rewindBannerText.text = "<< TIME REWIND <<";
            rewindBannerText.color = new Color(0f, 0.9f, 1f, 1f); // Bright solid Cyan
        }
    }

    private void HandleReadyToResume()
    {
        isOverlayActive = true;
        if (rewindBannerText != null)
        {
            rewindBannerText.text = ">> PRESS WASD OR SPACE TO RESUME <<";
            rewindBannerText.color = Color.white; // Solid crisp White
        }
    }

    private void HandleRewindComplete()
    {
        isOverlayActive = false;
    }

    public void HideImmediate()
    {
        isOverlayActive = false;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }
    }

    private void Update()
    {
        if (canvasGroup == null) return;

        // Smooth fade without any flashing
        float targetAlpha = isOverlayActive ? 1f : 0f;
        canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, Time.unscaledDeltaTime * fadeSpeed);
        canvasGroup.blocksRaycasts = false;

        if (vignetteOverlayImage != null)
        {
            // Static, gentle, solid ambient tint (zero flashing/strobing)
            vignetteOverlayImage.color = new Color(0f, 0.70f, 0.90f, 0.28f);
        }
    }
}
