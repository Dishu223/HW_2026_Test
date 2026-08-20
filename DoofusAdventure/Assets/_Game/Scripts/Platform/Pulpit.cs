using TMPro;
using UnityEngine;

/// <summary>
/// Controls an individual pulpit platform using a deterministic mathematical timeline:
/// State at time t = f(worldTime, spawnTime, destroyTime).
/// - If t < spawnTime: Platform was born in the future -> Un-spawns / self-destructs!
/// - If spawnTime <= t < destroyTime: Platform is ALIVE with remainingTime = destroyTime - t!
/// - If t >= destroyTime: Platform is collapsed.
/// </summary>
public class Pulpit : MonoBehaviour
{
    [Header("Visual Feedback")]
    [SerializeField] private Renderer platformRenderer;
    [SerializeField] private Collider platformCollider;
    [SerializeField] private Color normalColor = new Color(0.18f, 0.8f, 0.44f);
    [SerializeField] private Color warningColor = new Color(0.95f, 0.77f, 0.06f);
    [SerializeField] private Color criticalColor = new Color(0.91f, 0.3f, 0.24f);

    [Header("On-Tile Timer Text")]
    [SerializeField] private TextMeshPro timerText;

    private float lifetime = 5f;
    private float spawnWorldTime = 0f;
    private float destroyWorldTime = 5f;
    private float remainingTime = 5f;
    private bool isDestroyed = false;
    private bool hasPlayerVisited = false;
    private Material runtimeMaterial;
    private bool isInitialized = false;

    public bool IsDestroyed => isDestroyed;
    public float RemainingTime => remainingTime;
    public float SpawnWorldTime => spawnWorldTime;

    private void Awake()
    {
        if (platformRenderer == null)
            platformRenderer = GetComponent<Renderer>();

        if (platformCollider == null)
            platformCollider = GetComponent<Collider>();

        if (platformRenderer != null)
            runtimeMaterial = platformRenderer.material;

        if (timerText == null)
            timerText = GetComponentInChildren<TextMeshPro>();
    }

    private void Start()
    {
        if (!isInitialized)
        {
            InitializeTimeline(RewindManager.Instance != null ? RewindManager.Instance.WorldTime : 0f);
        }
    }

    public void InitializeTimeline(float currentWorldTime)
    {
        isInitialized = true;
        float minTime = GameConfig.Instance != null ? GameConfig.Instance.MinDestroyTime : 5f;
        float maxTime = GameConfig.Instance != null ? GameConfig.Instance.MaxDestroyTime : 5f;

        lifetime = (minTime == maxTime) ? minTime : Random.Range(minTime, maxTime);
        spawnWorldTime = currentWorldTime;
        destroyWorldTime = spawnWorldTime + lifetime;
        remainingTime = lifetime;
        isDestroyed = false;
        hasPlayerVisited = false;

        SetPlatformVisibility(true);
        UpdateVisuals(1f);
        UpdateTileText(remainingTime);
    }

    private void Update()
    {
        float currentWorldTime = RewindManager.Instance != null ? RewindManager.Instance.WorldTime : 0f;

        // 1. If we rewound past the moment this platform was spawned -> It does not exist yet in the past!
        if (currentWorldTime < spawnWorldTime - 0.05f && spawnWorldTime > 0.1f)
        {
            Destroy(gameObject);
            return;
        }

        // 2. If current time is past expiration -> Collapse platform
        if (currentWorldTime >= destroyWorldTime)
        {
            if (!isDestroyed)
            {
                CollapsePulpit();
            }
            return;
        }

        // 3. Platform is ALIVE in the current slice of time!
        if (isDestroyed)
        {
            ResurrectPulpit();
        }

        remainingTime = Mathf.Clamp(destroyWorldTime - currentWorldTime, 0f, lifetime);
        float normalizedTime = Mathf.Clamp01(remainingTime / lifetime);

        GameEvents.TriggerPulpitTimerTick(normalizedTime);
        UpdateVisuals(normalizedTime);
        UpdateTileText(remainingTime);
    }

    private void CollapsePulpit()
    {
        isDestroyed = true;
        SetPlatformVisibility(false);
        GameEvents.TriggerPulpitDestroyed(transform.position);
    }

    private void ResurrectPulpit()
    {
        isDestroyed = false;
        SetPlatformVisibility(true);
    }

    private void SetPlatformVisibility(bool visible)
    {
        if (platformRenderer != null) platformRenderer.enabled = visible;
        if (platformCollider != null) platformCollider.enabled = visible;
        if (timerText != null) timerText.gameObject.SetActive(visible);
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

        if (displaySec < 1.5f)
        {
            timerText.color = new Color(1f, 0.25f, 0.25f, 1f);
        }
        else if (displaySec < 2.5f)
        {
            timerText.color = new Color(1f, 0.9f, 0.3f, 1f);
        }
        else
        {
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

    private void OnDestroy()
    {
        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
        }
    }
}
