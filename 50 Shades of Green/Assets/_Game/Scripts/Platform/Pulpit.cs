using UnityEngine;

// Controls an individual 9x9 pulpit platform, its lifetime, visual timer countdown, and landing trigger
public class Pulpit : MonoBehaviour
{
    [Header("Visual Feedback")]
    [SerializeField] private MeshRenderer meshRenderer;

    private float totalLifetime;
    private float remainingTime;
    private bool isDestroyed = false;
    private bool hasBeenSteppedOn = false;
    private System.Action<Pulpit> onDestroyCallback;

    public float RemainingTime => remainingTime;
    public float TotalLifetime => totalLifetime;
    public Vector3 Position => transform.position;

    public void Initialize(float lifetime, System.Action<Pulpit> onDestroyed)
    {
        totalLifetime = lifetime;
        remainingTime = lifetime;
        onDestroyCallback = onDestroyed;

        if (meshRenderer == null)
        {
            meshRenderer = GetComponentInChildren<MeshRenderer>();
        }
    }

    private void Update()
    {
        if (isDestroyed) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

        remainingTime -= Time.deltaTime;
        float normalizedTime = Mathf.Clamp01(remainingTime / totalLifetime);

        GameEvents.TriggerPulpitTimerTick(normalizedTime);

        // Visual cue: change tint slightly as it nears destruction
        if (meshRenderer != null && meshRenderer.material != null)
        {
            Color baseColor = Color.green;
            Color warnColor = Color.red;
            meshRenderer.material.color = Color.Lerp(warnColor, baseColor, normalizedTime);
        }

        if (remainingTime <= 0f)
        {
            DestroyPulpit();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasBeenSteppedOn) return;

        // Check if Doofus walked onto this platform
        if (other.GetComponent<DoofusController>() != null || other.CompareTag("Player"))
        {
            hasBeenSteppedOn = true;
            GameEvents.TriggerPulpitLanded();
        }
    }

    private void DestroyPulpit()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        GameEvents.TriggerPulpitDestroyed(transform.position);
        onDestroyCallback?.Invoke(this);

        Destroy(gameObject);
    }
}
