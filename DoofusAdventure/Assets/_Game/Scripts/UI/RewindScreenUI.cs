using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the Prince of Persia Time Rewind & Resume overlay:
/// - "⏪ REWINDING TIME" during reverse flight
/// - "► PRESS WASD OR SPACE TO RESUME ◄" when safely landed
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class RewindScreenUI : MonoBehaviour
{
    [Header("UI Visual Elements")]
    [SerializeField] private Image vignetteOverlayImage;
    [SerializeField] private TextMeshProUGUI rewindBannerText;

    private CanvasGroup canvasGroup;
    private bool isOverlayActive = false;
    private bool isReadyToResume = false;
    private float fadeSpeed = 5f;

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
        isReadyToResume = false;
        if (rewindBannerText != null)
        {
            rewindBannerText.text = "⏪ REWINDING TIME ⏪";
        }
    }

    private void HandleReadyToResume()
    {
        isOverlayActive = true;
        isReadyToResume = true;
        if (rewindBannerText != null)
        {
            rewindBannerText.text = "► PRESS WASD OR SPACE TO RESUME ◄";
        }
    }

    private void HandleRewindComplete()
    {
        isOverlayActive = false;
        isReadyToResume = false;
    }

    public void HideImmediate()
    {
        isOverlayActive = false;
        isReadyToResume = false;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }
    }

    private void Update()
    {
        if (canvasGroup == null) return;

        float targetAlpha = isOverlayActive ? 1f : 0f;
        canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, Time.unscaledDeltaTime * fadeSpeed);
        canvasGroup.blocksRaycasts = false;

        if (canvasGroup.alpha > 0.01f)
        {
            if (vignetteOverlayImage != null)
            {
                float breathe = isReadyToResume 
                    ? 0.40f + Mathf.PingPong(Time.unscaledTime * 3f, 0.15f)
                    : 0.35f + Mathf.Sin(Time.unscaledTime * 2f) * 0.08f;
                vignetteOverlayImage.color = new Color(0f, 0.75f, 0.95f, breathe);
            }

            if (rewindBannerText != null)
            {
                if (isReadyToResume)
                {
                    float pulse = 0.5f + Mathf.PingPong(Time.unscaledTime * 3f, 0.5f);
                    rewindBannerText.alpha = pulse;
                }
                else
                {
                    rewindBannerText.alpha = 0.9f;
                }
            }
        }
    }
}
