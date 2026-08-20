using TMPro;
using UnityEngine;

/// <summary>
/// Controls an individual pulpit platform:
/// - Random countdown lifetime (synced with game state)
/// - Robust URP material color lerp (supports both _BaseColor and _Color)
/// - Single on-tile diegetic timer display at bottom-left corner
/// - Panic pulse effect when timer < 1.5s
/// </summary>
public class Pulpit : MonoBehaviour
{
    [Header("Visual Feedback")]
    [SerializeField] private Renderer platformRenderer;
    [SerializeField] private Color normalColor = new Color(0.18f, 0.8f, 0.44f);  // Crisp Green
    [SerializeField] private Color warningColor = new Color(0.95f, 0.77f, 0.06f); // Yellow Warning
    [SerializeField] private Color criticalColor = new Color(0.91f, 0.3f, 0.24f); // Red Critical

    [Header("On-Tile Timer Display (Assigned in Prefab)")]
    [SerializeField] private TextMeshPro timerText;

    private float lifetime;
    private float remainingTime;
    private bool isDestroyed = false;
    private bool hasPlayerVisited = false;
    private bool isTimerActive = false;
    private Material runtimeMaterial;
    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorID = Shader.PropertyToID("_Color");

    private void Awake()
    {
        if (platformRenderer == null)
            platformRenderer = GetComponent<Renderer>();

        if (platformRenderer != null)
            runtimeMaterial = platformRenderer.material;

        // Auto-find timer text if child exists and not assigned
        if (timerText == null)
            timerText = GetComponentInChildren<TextMeshPro>();
    }

    private void OnEnable()
    {
        GameEvents.OnGameStart += StartCountdown;
    }

    private void OnDisable()
    {
        GameEvents.OnGameStart -= StartCountdown;
    }

    private void Start()
    {
        InitializeLifetime();

        // Check if game is already active
        if (GameManager.Instance != null && GameManager.Instance.IsPlaying)
        {
            isTimerActive = true;
        }
    }

    public void StartCountdown()
    {
        isTimerActive = true;
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

        // Failsafe: Sync with GameManager state if event was missed
        if (!isTimerActive && GameManager.Instance != null && GameManager.Instance.IsPlaying)
        {
            isTimerActive = true;
        }

        // Freeze countdown when on start menu
        if (!isTimerActive)
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

    /// <summary>
    /// Smoothly transitions platform color from Green to Yellow to Red.
    /// Supports both URP Lit (_BaseColor) and Standard (_Color).
    /// </summary>
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

        // Set color for URP Lit shader
        if (runtimeMaterial.HasProperty(BaseColorID))
            runtimeMaterial.SetColor(BaseColorID, targetColor);

        // Fallback for standard shaders
        if (runtimeMaterial.HasProperty(ColorID))
            runtimeMaterial.SetColor(ColorID, targetColor);

        runtimeMaterial.color = targetColor;
    }

    private void UpdateTileText(float timeRemaining)
    {
        if (timerText == null) return;

        float displaySec = Mathf.Max(0f, timeRemaining);
        timerText.text = $"{displaySec:0.00}";

        // Urgent pulse & color change when time is low (< 1.5s)
        if (displaySec < 1.5f)
        {
            float pulse = 1f + Mathf.Sin(Time.time * 24f) * 0.15f;
            timerText.transform.localScale = Vector3.one * pulse;
            timerText.color = new Color(1f, 0.3f, 0.3f, 1f); // Soft Red glow
        }
        else if (displaySec < 2.5f)
        {
            timerText.transform.localScale = Vector3.one;
            timerText.color = new Color(1f, 0.9f, 0.4f, 1f); // Soft Yellow
        }
        else
        {
            timerText.transform.localScale = Vector3.one;
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
