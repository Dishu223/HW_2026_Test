using UnityEngine;

/// <summary>
/// Smoothly tracks Doofus from an isometric 3/4 perspective,
/// maintaining target visibility and providing screen shake on platform collapses and milestones.
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Follow Target")]
    [SerializeField] private Transform target;

    [Header("Isometric Camera Offset")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 11f, -13f);
    [SerializeField] private float smoothSpeed = 8f;

    [Header("Screen Shake")]
    [SerializeField] private float shakeDamping = 10f;

    private Vector3 shakeOffset = Vector3.zero;
    private float currentShakeIntensity = 0f;

    private void Awake()
    {
        // Preset optimal isometric pitch angle
        transform.rotation = Quaternion.Euler(38f, 0f, 0f);
    }

    private void OnEnable()
    {
        GameEvents.OnPulpitDestroyed += HandlePulpitDestroyed;
        GameEvents.OnMilestoneReached += HandleMilestone;
    }

    private void OnDisable()
    {
        GameEvents.OnPulpitDestroyed -= HandlePulpitDestroyed;
        GameEvents.OnMilestoneReached -= HandleMilestone;
    }

    private void Start()
    {
        // Auto-find Doofus if not manually assigned
        if (target == null)
        {
            DoofusController doofus = FindFirstObjectByType<DoofusController>();
            if (doofus != null) target = doofus.transform;
        }

        // Snap immediately to target position on start
        if (target != null)
        {
            transform.position = target.position + offset;
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Smooth follow target
        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * smoothSpeed);

        // Apply screen shake
        if (currentShakeIntensity > 0.01f)
        {
            shakeOffset = Random.insideUnitSphere * currentShakeIntensity;
            shakeOffset.z = 0f; // Keep shake on screen plane
            transform.position += shakeOffset;

            currentShakeIntensity = Mathf.Lerp(currentShakeIntensity, 0f, Time.deltaTime * shakeDamping);
        }
    }

    public void TriggerShake(float intensity)
    {
        currentShakeIntensity = Mathf.Max(currentShakeIntensity, intensity);
    }

    private void HandlePulpitDestroyed(Vector3 pos)
    {
        TriggerShake(0.18f); // Subtle satisfying rumble on platform collapse
    }

    private void HandleMilestone(int milestone)
    {
        TriggerShake(0.35f); // Juicy screen punch on milestones
    }
}
