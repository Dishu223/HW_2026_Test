using TMPro;
using UnityEngine;

/// <summary>
/// Controls an individual platform:
/// - Countdown lifetime that counts down reliably during active gameplay
/// - Smooth color shift (Green -> Yellow -> Red)
/// - Single compact countdown text at bottom-left corner with parent scale compensation
/// </summary>
public class Pulpit : MonoBehaviour
{
    [Header("Visual Feedback")]
    [SerializeField] private Renderer platformRenderer;
    [SerializeField] private Color normalColor = new Color(0.18f, 0.8f, 0.44f);  // Crisp Green
    [SerializeField] private Color warningColor = new Color(0.95f, 0.77f, 0.06f); // Yellow Warning
    [SerializeField] private Color criticalColor = new Color(0.91f, 0.3f, 0.24f); // Red Critical

    [Header("On-Tile Timer Text")]
    [SerializeField] private TextMeshPro timerText;

    private float lifetime = 4.5f;
    private float remainingTime = 4.5f;
    private bool isDestroyed = false;
    private bool hasPlayerVisited = false;
    private Material runtimeMaterial;

    private void Awake()
    {
        if (platformRenderer == null)
            platformRenderer = GetComponent<Renderer>();

        if (platformRenderer != null)
            runtimeMaterial = platformRenderer.material;

        if (timerText == null)
            timerText = GetComponentInChildren<TextMeshPro>();

        // Compensate for parent platform scaling (5, 0.5, 5) so text is not stretched or huge!
        if (timerText != null)
        {
            // Position at bottom-left corner
            timerText.transform.localPosition = new Vector3(-0.35f, 0.55f, -0.35f);
            timerText.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            // Counteract parent (5, 0.5, 5) scale
            timerText.transform.localScale = new Vector3(0.2f, 2.0f, 0.2f);
            timerText.fontSize = 6f;
            timerText.fontStyle = FontStyles.Bold;
            timerText.alignment = TextAlignmentOptions.Center;
        }
    }

    private void Start()
    {
        InitializeLifetime();
    }

    public void InitializeLifetime()
    {
        float minTime = GameConfig.Instance != null ? GameConfig.Instance.MinDestroyTime : 4f;
        float maxTime = GameConfig.Instance != null ? GameConfig.Instance.MaxDestroyTime : 5f;

        lifetime = Random.Range(minTime, maxTime);
        remainingTime = lifetime;
        isDestroyed = false;
        hasPlayerVisited = false;

        UpdateVisuals(1f);
        UpdateTileText(remainingTime);
    }

    private void Update()
    {
        if (isDestroyed) return;

        // Only count down if game is actively playing
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
        {
            UpdateTileText(remainingTime);
            return;
        }

        remainingTime -= Time.deltaTime;
        float normalizedTime = Mathf.Clamp01(remainingTime / lifetime);

        GameEvents.TriggerPulpitTimerTick(normalizedTime);

        UpdateVisuals(normalizedTime);
        UpdateTileText(remainingTime);

        if (remainingTime <= 0f)
        {
            DestroyPulpit();
        }
    }

    private void UpdateVisuals(float normalizedTime)
    {
        if (runtimeMaterial == null) return;

        Color targetColor;
        if (normalizedTime > 0.5f)
        {
            float t = (1f - normalizedTime) * 2f;
            targetColor = Color.Lerp(normalColor, warningColor, t);
        }
        else
        {
            float t = (0.5f - normalizedTime) * 2f;
            targetColor = Color.Lerp(warningColor, criticalColor, t);
        }

        try
        {
            if (runtimeMaterial.HasProperty("_BaseColor"))
                runtimeMaterial.SetColor("_BaseColor", targetColor);
            else
                runtimeMaterial.color = targetColor;
        }
        catch
        {
            runtimeMaterial.color = targetColor;
        }
    }

    private void UpdateTileText(float timeRemaining)
    {
        if (timerText == null) return;

        float displaySec = Mathf.Max(0f, timeRemaining);
        timerText.text = $"{displaySec:0.00}";

        // Color and gentle pulse when low on time
        if (displaySec < 1.5f)
        {
            float pulse = 1f + Mathf.Sin(Time.time * 24f) * 0.12f;
            timerText.transform.localScale = new Vector3(0.2f * pulse, 2.0f * pulse, 0.2f * pulse);
            timerText.color = new Color(1f, 0.35f, 0.35f, 1f);
        }
        else if (displaySec < 2.5f)
        {
            timerText.transform.localScale = new Vector3(0.2f, 2.0f, 0.2f);
            timerText.color = new Color(1f, 0.9f, 0.4f, 1f);
        }
        else
        {
            timerText.transform.localScale = new Vector3(0.2f, 2.0f, 0.2f);
            timerText.color = Color.white;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!hasPlayerVisited && collision.gameObject.GetComponent<DoofusController>() != null)
        {
            hasPlayerVisited = true;
            GameEvents.TriggerPulpitLanded();
        }
    }

    public void DestroyPulpit()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        GameEvents.TriggerPulpitDestroyed(transform.position);
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
        }
    }
}
