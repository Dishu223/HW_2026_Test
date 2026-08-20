using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Controls an individual pulpit platform:
/// - Records rolling snapshot history of lifetime and destruction state
/// - Plays in reverse during Prince of Persia Time Rewind (reconstructing destroyed platforms!)
/// - Smooth color shift (Green -> Yellow -> Red in forward, Red -> Yellow -> Green in reverse)
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

    public struct PulpitSnapshot
    {
        public float remainingTime;
        public bool isDestroyed;

        public PulpitSnapshot(float time, bool destroyed)
        {
            remainingTime = time;
            isDestroyed = destroyed;
        }
    }

    private readonly LinkedList<PulpitSnapshot> historyBuffer = new LinkedList<PulpitSnapshot>();
    private float lifetime = 5f;
    private float remainingTime = 5f;
    private bool isDestroyed = false;
    private bool isRewinding = false;
    private bool hasPlayerVisited = false;
    private Material runtimeMaterial;
    private float timeSinceDestroyed = 0f;

    public bool IsDestroyed => isDestroyed;
    public float RemainingTime => remainingTime;

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
        InitializeLifetime();
    }

    private void OnEnable()
    {
        GameEvents.OnRewindStart += HandleRewindStart;
        GameEvents.OnRewindComplete += HandleRewindComplete;
    }

    private void OnDisable()
    {
        GameEvents.OnRewindStart -= HandleRewindStart;
        GameEvents.OnRewindComplete -= HandleRewindComplete;
    }

    public void InitializeLifetime()
    {
        float minTime = GameConfig.Instance != null ? GameConfig.Instance.MinDestroyTime : 5f;
        float maxTime = GameConfig.Instance != null ? GameConfig.Instance.MaxDestroyTime : 5f;

        lifetime = (minTime == maxTime) ? minTime : Random.Range(minTime, maxTime);
        remainingTime = lifetime;
        isDestroyed = false;
        hasPlayerVisited = false;
        timeSinceDestroyed = 0f;

        SetPlatformVisibility(true);
        UpdateVisuals(1f);
        UpdateTileText(remainingTime);
    }

    private void FixedUpdate()
    {
        // 1. Record history while playing normally
        if (!isRewinding && GameManager.Instance != null && GameManager.Instance.IsPlaying)
        {
            historyBuffer.AddLast(new PulpitSnapshot(remainingTime, isDestroyed));

            // Keep ~4.0 seconds of history
            int maxSnapshots = Mathf.RoundToInt(4.0f / Time.fixedDeltaTime);
            while (historyBuffer.Count > maxSnapshots)
            {
                historyBuffer.RemoveFirst();
            }
        }
        // 2. Play history in reverse during Time Rewind
        else if (isRewinding)
        {
            RewindStep();
        }
    }

    private void Update()
    {
        if (isRewinding) return;

        if (isDestroyed)
        {
            timeSinceDestroyed += Time.deltaTime;
            // Only truly destroy from memory after 6 seconds (ensuring it stays in the rewind window!)
            if (timeSinceDestroyed > 6.0f)
            {
                Destroy(gameObject);
            }
            return;
        }

        // Freeze when on start menu
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
            CollapsePulpit();
        }
    }

    /// <summary>
    /// Rewinds this platform's timer and resurrects it if it was collapsed!
    /// </summary>
    public void RewindStep()
    {
        if (historyBuffer.Count == 0) return;

        // Skip frames to match rewind playback speed
        int skip = 2;
        for (int i = 0; i < skip && historyBuffer.Count > 0; i++)
        {
            PulpitSnapshot snap = historyBuffer.Last.Value;
            historyBuffer.RemoveLast();

            remainingTime = snap.remainingTime;
            
            // If it was dead, resurrect it!
            if (!snap.isDestroyed && isDestroyed)
            {
                ResurrectPulpit();
            }
            else if (snap.isDestroyed && !isDestroyed)
            {
                SetPlatformVisibility(false);
                isDestroyed = true;
            }
        }

        float normalizedTime = Mathf.Clamp01(remainingTime / lifetime);
        UpdateVisuals(normalizedTime);
        UpdateTileText(remainingTime);
    }

    private void CollapsePulpit()
    {
        if (isDestroyed) return;
        isDestroyed = true;
        timeSinceDestroyed = 0f;

        SetPlatformVisibility(false);
        GameEvents.TriggerPulpitDestroyed(transform.position);
    }

    private void ResurrectPulpit()
    {
        isDestroyed = false;
        timeSinceDestroyed = 0f;
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

    private void HandleRewindStart()
    {
        isRewinding = true;
    }

    private void HandleRewindComplete()
    {
        isRewinding = false;
        if (!isDestroyed)
        {
            SetPlatformVisibility(true);
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
