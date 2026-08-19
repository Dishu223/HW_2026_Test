using UnityEngine;

// Controls an individual 9x9 Pulpit platform, its countdown timer,
// visual warning states, player step detection, and destruction behavior.
public class Pulpit : MonoBehaviour
{
    [Header("Visual References")]
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private GameObject shatterPrefab;

    [Header("Color States")]
    [SerializeField] private Color normalColor = new Color(0.1f, 0.8f, 0.2f);   // Bright metallic green
    [SerializeField] private Color warningColor = new Color(1.0f, 0.7f, 0.0f);  // Warning amber
    [SerializeField] private Color dangerColor = new Color(0.9f, 0.1f, 0.1f);   // Critical red

    private float totalLifetime;
    private float timeRemaining;
    private bool isDestroyed = false;
    private bool hasBeenVisited = false;
    private Material platformMaterial;
    private Vector3 originalPosition;

    public bool HasBeenVisited => hasBeenVisited;
    public float NormalizedTimeRemaining => totalLifetime > 0f ? Mathf.Clamp01(timeRemaining / totalLifetime) : 0f;
    public float TimeRemaining => timeRemaining;

    private void Awake()
    {
        if (meshRenderer == null)
        {
            meshRenderer = GetComponentInChildren<MeshRenderer>();
        }

        if (meshRenderer != null)
        {
            // Create an instance of the material so each pulpit can change color independently
            platformMaterial = meshRenderer.material;
        }

        originalPosition = transform.position;
    }

    // Initializes platform timer with configured min/max range
    public void Initialize(float minTime, float maxTime)
    {
        totalLifetime = Random.Range(minTime, maxTime);
        timeRemaining = totalLifetime;
        isDestroyed = false;
        originalPosition = transform.position;
        UpdateVisuals(1f);
    }

    private void Update()
    {
        if (isDestroyed) return;

        // Only count down during active gameplay
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
        {
            return;
        }

        timeRemaining -= Time.deltaTime;
        float normalized = NormalizedTimeRemaining;

        // Update color and vibration warnings
        UpdateVisuals(normalized);

        // Notify systems (e.g. HUD timer bar, Doofus facial reaction)
        if (hasBeenVisited)
        {
            GameEvents.TriggerPulpitTimerTick(normalized);
        }

        // Time ran out: collapse platform
        if (timeRemaining <= 0f)
        {
            DestroyPlatform();
        }
    }

    private void UpdateVisuals(float normalized)
    {
        if (platformMaterial == null) return;

        // Color transition: Green -> Yellow -> Red
        Color targetColor;
        if (normalized > 0.5f)
        {
            // Transition from normal green to warning yellow (1.0 -> 0.5)
            float t = (normalized - 0.5f) / 0.5f;
            targetColor = Color.Lerp(warningColor, normalColor, t);
        }
        else
        {
            // Transition from warning yellow to critical red (0.5 -> 0.0)
            float t = normalized / 0.5f;
            targetColor = Color.Lerp(dangerColor, warningColor, t);
        }

        platformMaterial.color = targetColor;

        // Shake visual effect when timer is critical (< 25%)
        if (normalized < 0.25f)
        {
            float shakeIntensity = (1f - (normalized / 0.25f)) * 0.08f;
            Vector2 randomOffset = Random.insideUnitCircle * shakeIntensity;
            transform.position = originalPosition + new Vector3(randomOffset.x, 0f, randomOffset.y);
        }
        else
        {
            transform.position = originalPosition;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // When Doofus touches this platform for the first time, award the score
        if (!hasBeenVisited && other.GetComponent<DoofusController>() != null)
        {
            hasBeenVisited = true;
            GameEvents.TriggerPulpitLanded();
            Debug.Log($"<color=yellow>[Pulpit]</color> Player landed on Pulpit at {transform.position}!");
        }
    }

    // Called on collision or when timer reaches 0
    public void DestroyPlatform()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        GameEvents.TriggerPulpitDestroyed(originalPosition);

        // Spawn shattered physics chunks if assigned
        if (shatterPrefab != null)
        {
            GameObject shatteredObj = Instantiate(shatterPrefab, originalPosition, Quaternion.identity);
            Rigidbody[] chunkBodies = shatteredObj.GetComponentsInChildren<Rigidbody>();
            foreach (var rb in chunkBodies)
            {
                rb.AddExplosionForce(350f, originalPosition + Vector3.down * 0.5f, 6f);
            }
            Destroy(shatteredObj, 2.5f);
        }

        Destroy(gameObject);
    }
}
