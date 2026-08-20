using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the cinematic Prince of Persia / VHS Time Rewind screen overlay:
/// - Cyan/Chrono vignette border that pulses during reverse playback
/// - High-energy "⏪ REWINDING TIME ⏪" glitching banner
/// - Flash-fade on rewind completion
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class RewindScreenUI : MonoBehaviour
{
    [Header("UI Visual Elements")]
    [SerializeField] private Image vignetteOverlayImage;
    [SerializeField] private TextMeshProUGUI rewindBannerText;

    private CanvasGroup canvasGroup;
    private bool isRewindScreenActive = false;

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
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = false;
        }
    }

    private void HideRewindOverlay()
    {
        isRewindScreenActive = false;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }
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
        if (!isRewindScreenActive) return;

        // Pulsing vignette glow (Cyan -> Gold)
        if (vignetteOverlayImage != null)
        {
            float pulse = 0.35f + Mathf.PingPong(Time.unscaledTime * 5f, 0.35f);
            vignetteOverlayImage.color = new Color(0f, 0.85f, 1f, pulse);
        }

        // Glitch pulse on banner text
        if (rewindBannerText != null)
        {
            float alpha = 0.7f + Mathf.PingPong(Time.unscaledTime * 8f, 0.3f);
            rewindBannerText.alpha = alpha;
            float wobble = Mathf.Sin(Time.unscaledTime * 20f) * 4f;
            rewindBannerText.rectTransform.anchoredPosition = new Vector2(wobble, rewindBannerText.rectTransform.anchoredPosition.y);
        }
    }
}
