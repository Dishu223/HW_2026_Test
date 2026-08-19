using UnityEngine;

/// <summary>
/// Controls an individual platform: its random countdown lifetime,
/// visual color transition (green -> yellow -> red), player landing detection,
/// and destruction trigger.
/// </summary>
public class Pulpit : MonoBehaviour
{
    [Header("Visual Feedback")]
    [SerializeField] private Renderer platformRenderer;
    [SerializeField] private Color normalColor = new Color(0.18f, 0.8f, 0.44f); // Crisp Green
    [SerializeField] private Color warningColor = new Color(0.95f, 0.77f, 0.06f); // Yellow Warning
    [SerializeField] private Color criticalColor = new Color(0.91f, 0.3f, 0.24f); // Red Critical

    private float lifetime;
    private float remainingTime;
    private bool isDestroyed = false;
    private bool hasPlayerVisited = false;
    private Material runtimeMaterial;

    private void Awake()
    {
        if (platformRenderer == null)
            platformRenderer = GetComponent<Renderer>();

        // Create an instance of the material so changing its color does not affect other platforms
        if (platformRenderer != null)
            runtimeMaterial = platformRenderer.material;
    }

    private void Start()
    {
        InitializeLifetime();
    }

    /// <summary>
    /// Sets a random destroy time between min and max destroy times from GameConfig.
    /// </summary>
    public void InitializeLifetime()
    {
        float minTime = GameConfig.Instance != null ? GameConfig.Instance.MinDestroyTime : 4f;
        float maxTime = GameConfig.Instance != null ? GameConfig.Instance.MaxDestroyTime : 5f;

        lifetime = Random.Range(minTime, maxTime);
        remainingTime = lifetime;
        isDestroyed = false;
        hasPlayerVisited = false;

        UpdateVisuals(1f);
    }

    private void Update()
    {
        if (isDestroyed) return;

        remainingTime -= Time.deltaTime;
        float normalizedTime = Mathf.Clamp01(remainingTime / lifetime);

        // Broadcast current timer tick for active HUD display
        GameEvents.TriggerPulpitTimerTick(normalizedTime);

        // Update platform color based on remaining time percentage
        UpdateVisuals(normalizedTime);

        // When timer runs out, destroy platform
        if (remainingTime <= 0f)
        {
            DestroyPulpit();
        }
    }

    /// <summary>
    /// Smoothly transitions platform color from Green (100%-50%) to Yellow (50%-25%) to Red (<25%).
    /// </summary>
    private void UpdateVisuals(float normalizedTime)
    {
        if (runtimeMaterial == null) return;

        Color targetColor;
        if (normalizedTime > 0.5f)
        {
            // Transition from Green to Yellow
            float t = (1f - normalizedTime) * 2f; // 0.0 to 1.0
            targetColor = Color.Lerp(normalColor, warningColor, t);
        }
        else
        {
            // Transition from Yellow to Red
            float t = (0.5f - normalizedTime) * 2f; // 0.0 to 1.0
            targetColor = Color.Lerp(warningColor, criticalColor, t);
        }

        runtimeMaterial.color = targetColor;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Check if Doofus just stepped onto this platform for the first time
        if (!hasPlayerVisited && collision.gameObject.GetComponent<DoofusController>() != null)
        {
            hasPlayerVisited = true;
            GameEvents.TriggerPulpitLanded();
        }
    }

    /// <summary>
    /// Handles platform expiration: broadcasts event for VFX/Audio, then removes GameObject.
    /// </summary>
    public void DestroyPulpit()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        // Broadcast location so particle sparks and shatter sounds trigger
        GameEvents.TriggerPulpitDestroyed(transform.position);

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        // Clean up instantiated material from memory
        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
        }
    }
}
