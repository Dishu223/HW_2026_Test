using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Calm, atmospheric Prince of Persia Time Rewind overlay:
/// - Soft, cinematic cyan/gold vignette with smooth fade transitions
/// - Clean, legible typography without aggressive flashing or strobing
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class RewindScreenUI : MonoBehaviour
{
    [Header("UI Visual Elements")]
    [SerializeField] private Image vignetteOverlayImage;
    [SerializeField] private TextMeshProUGUI rewindBannerText;

    private CanvasGroup canvasGroup;
    private bool isRewindScreenActive = false;
    private float fadeSpeed = 4f;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        HideImmediate();
    }

    private void OnEnable()
    {
        GameEvents.OnRewindStart += ShowRewindOverlay;
        GameEvents.OnRewindComplete += HideRewindOverlay;
        GameEvents.OnGameStart += HideImmediate;
        GameEvents.OnGameOver += HideImmediate;
    }

    private void OnDisable()
    {
        GameEvents.OnRewindStart -= ShowRewindOverlay;
        GameEvents.OnRewindComplete -= HideRewindOverlay;
        GameEvents.OnGameStart -= HideImmediate;
        GameEvents.OnGameOver -= HideImmediate;
    }

    private void ShowRewindOverlay()
    {
        isRewindScreenActive = true;
    }

    private void HideRewindOverlay()
    {
        isRewindScreenActive = false;
    }

    public void HideImmediate()
    {
        isRewindScreenActive = false;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }
    }

    private void Update()
    {
        if (canvasGroup == null) return;

        // Smooth non-jarring fade in and fade out
        float targetAlpha = isRewindScreenActive ? 1f : 0f;
        canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, Time.unscaledDeltaTime * fadeSpeed);
        canvasGroup.blocksRaycasts = false;

        if (canvasGroup.alpha > 0.01f)
        {
            // Calm, slow atmospheric ambient breathing (no strobing)
            if (vignetteOverlayImage != null)
            {
                float breathe = 0.35f + Mathf.Sin(Time.unscaledTime * 2f) * 0.08f;
                vignetteOverlayImage.color = new Color(0f, 0.75f, 0.95f, breathe);
            }

            if (rewindBannerText != null)
            {
                rewindBannerText.alpha = 0.9f;
            }
        }
    }
}
