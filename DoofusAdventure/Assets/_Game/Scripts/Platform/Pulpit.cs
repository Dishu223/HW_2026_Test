using TMPro;
using UnityEngine;

/// <summary>
/// Controls an individual platform:
/// - Random countdown lifetime (frozen until Game Starts)
/// - Diegetic on-platform 3D World-Space countdown timers (Destroy & Next Spawn)
/// - Visual color transition (green -> yellow -> red)
/// - Player landing detection and destruction VFX trigger.
/// </summary>
public class Pulpit : MonoBehaviour
{
    [Header("Visual Feedback")]
    [SerializeField] private Renderer platformRenderer;
    [SerializeField] private Color normalColor = new Color(0.18f, 0.8f, 0.44f);  // Crisp Green
    [SerializeField] private Color warningColor = new Color(0.95f, 0.77f, 0.06f); // Yellow Warning
    [SerializeField] private Color criticalColor = new Color(0.91f, 0.3f, 0.24f); // Red Critical

    [Header("On-Tile 3D Text Displays (Auto-Created if Null)")]
    [SerializeField] private TextMeshPro destroyTimerText;
    [SerializeField] private TextMeshPro spawnTimerText;

    private float lifetime;
    private float remainingTime;
    private bool isDestroyed = false;
    private bool hasPlayerVisited = false;
    private bool isTimerActive = false;
    private Material runtimeMaterial;

    private void Awake()
    {
        if (platformRenderer == null)
            platformRenderer = GetComponent<Renderer>();

        if (platformRenderer != null)
            runtimeMaterial = platformRenderer.material;

        CreateWorldSpaceTimerUI();
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

        // If game is already actively playing, start countdown immediately
        if (GameManager.Instance != null && GameManager.Instance.IsPlaying)
        {
            isTimerActive = true;
        }
        else
        {
            isTimerActive = false;
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

        runtimeMaterial.color = targetColor;
    }

    private void UpdateTileText(float timeRemaining)
    {
        if (destroyTimerText != null)
        {
            float displaySec = Mathf.Max(0f, timeRemaining);
            destroyTimerText.text = $"{displaySec:0.00}";

            // Match text color to platform urgency
            if (displaySec < 1.5f)
            {
                destroyTimerText.color = Color.white;
                float pulse = 1f + Mathf.Sin(Time.time * 20f) * 0.2f;
                destroyTimerText.transform.localScale = Vector3.one * pulse;
            }
            else
            {
                destroyTimerText.color = Color.white;
                destroyTimerText.transform.localScale = Vector3.one;
            }
        }

        if (spawnTimerText != null && PulpitManager.Instance != null)
        {
            float spawnTimeLeft = PulpitManager.Instance.RemainingSpawnTime;
            if (spawnTimeLeft > 0f)
            {
                spawnTimerText.text = $"{spawnTimeLeft:0.00}";
            }
            else
            {
                spawnTimerText.text = "";
            }
        }
    }

    /// <summary>
    /// Programmatically generates crisp, stylized 3D TextMeshPro floating timers on the tile surface.
    /// </summary>
    private void CreateWorldSpaceTimerUI()
    {
        if (destroyTimerText != null && spawnTimerText != null) return;

        // Destroy Timer (Left side of platform)
        if (destroyTimerText == null)
        {
            GameObject destroyObj = new GameObject("DestroyTimer_3DText");
            destroyObj.transform.SetParent(transform, false);
            destroyObj.transform.localPosition = new Vector3(-0.35f, 0.55f, -0.38f);
            destroyObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            destroyTimerText = destroyObj.AddComponent<TextMeshPro>();
            destroyTimerText.fontSize = 7;
            destroyTimerText.fontStyle = FontStyles.Bold;
            destroyTimerText.alignment = TextAlignmentOptions.Center;
            destroyTimerText.color = Color.white;
            destroyTimerText.text = "4.50";
        }

        // Next Spawn Timer (Right side of platform)
        if (spawnTimerText == null)
        {
            GameObject spawnObj = new GameObject("SpawnTimer_3DText");
            spawnObj.transform.SetParent(transform, false);
            spawnObj.transform.localPosition = new Vector3(0.35f, 0.55f, -0.38f);
            spawnObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            spawnTimerText = spawnObj.AddComponent<TextMeshPro>();
            spawnTimerText.fontSize = 7;
            spawnTimerText.fontStyle = FontStyles.Bold;
            spawnTimerText.alignment = TextAlignmentOptions.Center;
            spawnTimerText.color = new Color(1f, 1f, 1f, 0.85f);
            spawnTimerText.text = "2.50";
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
