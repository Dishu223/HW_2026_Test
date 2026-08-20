using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Controls an individual pulpit platform:
/// - Subtle flash of light when Doofus enters
/// - 3D Shatter Explosion when reaching 0.00s
/// - Reverse reassembly during Time Rewind
/// - Deterministic WorldTime binding
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

    private PlatformShatterFX shatterFX;
    private float lifetime = 5f;
    private float spawnWorldTime = 0f;
    private float destroyWorldTime = 5f;
    private float remainingTime = 5f;
    private bool isDestroyed = false;
    private bool hasPlayerVisited = false;
    private Material runtimeMaterial;
    private bool isInitialized = false;
    private int platformSequenceIndex = 0;

    private Coroutine flashRoutine;
    private float entryFlashIntensity = 0f;

    public bool IsDestroyed => isDestroyed;
    public float RemainingTime => remainingTime;
    public float SpawnWorldTime => spawnWorldTime;
    public int PlatformSequenceIndex => platformSequenceIndex;

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

        shatterFX = GetComponent<PlatformShatterFX>();
        if (shatterFX == null)
            shatterFX = gameObject.AddComponent<PlatformShatterFX>();
    }

    private void Start()
    {
        if (!isInitialized)
        {
            InitializeTimeline(RewindManager.Instance != null ? RewindManager.Instance.WorldTime : 0f, 0);
        }
    }

    public void InitializeTimeline(float currentWorldTime, int sequenceIndex)
    {
        isInitialized = true;
        platformSequenceIndex = sequenceIndex;

        float minTime = GameConfig.Instance != null ? GameConfig.Instance.MinDestroyTime : 5f;
        float maxTime = GameConfig.Instance != null ? GameConfig.Instance.MaxDestroyTime : 5f;

        lifetime = (minTime == maxTime) ? minTime : Random.Range(minTime, maxTime);
        spawnWorldTime = currentWorldTime;
        destroyWorldTime = spawnWorldTime + lifetime;
        remainingTime = lifetime;
        isDestroyed = false;
        hasPlayerVisited = false;
        entryFlashIntensity = 0f;

        if (shatterFX != null) shatterFX.ResetDebris();

        SetPlatformVisibility(true);
        UpdateVisuals(1f);
        UpdateTileText(remainingTime);
    }

    private void Update()
    {
        float currentWorldTime = RewindManager.Instance != null ? RewindManager.Instance.WorldTime : 0f;

        if (currentWorldTime < spawnWorldTime - 0.05f && spawnWorldTime > 0.1f)
        {
            Destroy(gameObject);
            return;
        }

        if (currentWorldTime >= destroyWorldTime)
        {
            if (!isDestroyed)
            {
                CollapsePulpit();
            }
            return;
        }

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

        if (shatterFX != null)
        {
            Color currentColor = runtimeMaterial != null ? runtimeMaterial.color : criticalColor;
            shatterFX.Explode(currentColor, runtimeMaterial);
        }

        GameEvents.TriggerPulpitDestroyed(transform.position);
    }

    private void ResurrectPulpit()
    {
        isDestroyed = false;
        if (shatterFX != null) shatterFX.ResetDebris();
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

        Color baseColor;
        if (normalizedTime > 0.5f)
        {
            float t = (1f - normalizedTime) * 2f;
            baseColor = Color.Lerp(normalColor, warningColor, t);
        }
        else
        {
            float t = (0.5f - normalizedTime) * 2f;
            baseColor = Color.Lerp(warningColor, criticalColor, t);
        }

        // Apply subtle entry flash highlight
        Color finalColor = (entryFlashIntensity > 0.01f) 
            ? Color.Lerp(baseColor, Color.white, entryFlashIntensity) 
            : baseColor;

        try
        {
            if (runtimeMaterial.HasProperty("_BaseColor"))
                runtimeMaterial.SetColor("_BaseColor", finalColor);
            else
                runtimeMaterial.color = finalColor;
        }
        catch
        {
            runtimeMaterial.color = finalColor;
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

            // Trigger juicy tile light flash!
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(TileFlashCoroutine());
        }
    }

    private IEnumerator TileFlashCoroutine()
    {
        entryFlashIntensity = 0.70f; // Bright flash
        float duration = 0.22f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            entryFlashIntensity = Mathf.Lerp(0.70f, 0f, elapsed / duration);
            yield return null;
        }

        entryFlashIntensity = 0f;
    }

    private void OnDestroy()
    {
        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
        }
    }
}
