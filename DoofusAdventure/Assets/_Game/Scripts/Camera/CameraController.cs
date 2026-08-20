using UnityEngine;

/// <summary>
/// Smoothly tracks Doofus from an isometric perspective,
/// provides dynamic camera zoom during Prince of Persia Time Rewind,
/// and applies screen shake on platform collapses and milestones.
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Follow Target")]
    [SerializeField] private Transform target;

    [Header("Isometric Camera Offset")]
    [SerializeField] private Vector3 defaultOffset = new Vector3(0f, 11f, -13f);
    [SerializeField] private float smoothSpeed = 8f;

    [Header("Rewind Dynamic Zoom")]
    [SerializeField] private float rewindZoomFactor = 0.75f; // 25% closer for high-intensity cinematic rewind
    [SerializeField] private float zoomSpeed = 6f;

    [Header("Screen Shake")]
    [SerializeField] private float shakeDamping = 10f;

    private Vector3 currentOffset;
    private Vector3 targetOffset;
    private Vector3 shakeOffset = Vector3.zero;
    private float currentShakeIntensity = 0f;

    private void Awake()
    {
        transform.rotation = Quaternion.Euler(38f, 0f, 0f);
        currentOffset = defaultOffset;
        targetOffset = defaultOffset;
    }

    private void OnEnable()
    {
        GameEvents.OnPulpitDestroyed += HandlePulpitDestroyed;
        GameEvents.OnMilestoneReached += HandleMilestone;
        GameEvents.OnRewindStart += HandleRewindStart;
        GameEvents.OnRewindComplete += HandleRewindComplete;
    }

    private void OnDisable()
    {
        GameEvents.OnPulpitDestroyed -= HandlePulpitDestroyed;
        GameEvents.OnMilestoneReached -= HandleMilestone;
        GameEvents.OnRewindStart -= HandleRewindStart;
        GameEvents.OnRewindComplete -= HandleRewindComplete;
    }

    private void Start()
    {
        if (target == null)
        {
            DoofusController doofus = FindAnyObjectByType<DoofusController>();
            if (doofus != null) target = doofus.transform;
        }

        if (target != null)
        {
            transform.position = target.position + defaultOffset;
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Smoothly adjust camera zoom offset
        currentOffset = Vector3.Lerp(currentOffset, targetOffset, Time.unscaledDeltaTime * zoomSpeed);

        Vector3 desiredPosition = target.position + currentOffset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.unscaledDeltaTime * smoothSpeed);

        if (currentShakeIntensity > 0.01f)
        {
            shakeOffset = Random.insideUnitSphere * currentShakeIntensity;
            shakeOffset.z = 0f;
            transform.position += shakeOffset;

            currentShakeIntensity = Mathf.Lerp(currentShakeIntensity, 0f, Time.unscaledDeltaTime * shakeDamping);
        }
    }

    public void TriggerShake(float intensity)
    {
        currentShakeIntensity = Mathf.Max(currentShakeIntensity, intensity);
    }

    private void HandlePulpitDestroyed(Vector3 pos)
    {
        TriggerShake(0.18f);
    }

    private void HandleMilestone(int milestone)
    {
        TriggerShake(0.35f);
    }

    private void HandleRewindStart()
    {
        // Swoop camera in closer during Time Rewind!
        targetOffset = defaultOffset * rewindZoomFactor;
        TriggerShake(0.15f);
    }

    private void HandleRewindComplete()
    {
        // Smoothly zoom back out to normal isometric view
        targetOffset = defaultOffset;
        TriggerShake(0.25f); // Satisfying punch on landing!
    }
}
