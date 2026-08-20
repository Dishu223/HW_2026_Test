using UnityEngine;

/// <summary>
/// Smoothly tracks Doofus from an isometric perspective,
/// provides cinematic buttery-smooth camera zoom during Prince of Persia Time Rewind (zero snapping),
/// and softly overlooks falls without violent downward camera judder.
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Follow Target")]
    [SerializeField] private Transform target;

    [Header("Isometric Camera Offset")]
    [SerializeField] private Vector3 defaultOffset = new Vector3(0f, 11f, -13f);
    [SerializeField] private float positionSmoothTime = 0.25f;

    [Header("Rewind Dynamic Zoom")]
    [SerializeField] private float rewindZoomFactor = 0.80f;
    [SerializeField] private float zoomSmoothTime = 0.40f;

    [Header("Screen Shake")]
    [SerializeField] private float shakeDamping = 10f;

    private Vector3 currentOffset;
    private Vector3 targetOffset;
    private Vector3 positionVelocity;
    private Vector3 zoomVelocity;
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
        GameEvents.OnGameStart += HandleGameReset;
        GameEvents.OnGameRestart += HandleGameReset;
    }

    private void OnDisable()
    {
        GameEvents.OnPulpitDestroyed -= HandlePulpitDestroyed;
        GameEvents.OnMilestoneReached -= HandleMilestone;
        GameEvents.OnRewindStart -= HandleRewindStart;
        GameEvents.OnRewindComplete -= HandleRewindComplete;
        GameEvents.OnGameStart -= HandleGameReset;
        GameEvents.OnGameRestart -= HandleGameReset;
    }

    private void Start()
    {
        FindTarget();
        if (target != null)
        {
            transform.position = target.position + defaultOffset;
        }
    }

    private void FindTarget()
    {
        if (target == null)
        {
            DoofusController doofus = FindAnyObjectByType<DoofusController>();
            if (doofus != null) target = doofus.transform;
        }
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            FindTarget();
            if (target == null) return;
        }

        // Smooth zoom offset transition
        currentOffset = Vector3.SmoothDamp(currentOffset, targetOffset, ref zoomVelocity, zoomSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);

        // When falling (Y < 0), clamp target Y so camera remains high and gracefully overlooks the fall
        Vector3 trackedTargetPos = target.position;
        if (trackedTargetPos.y < -0.2f)
        {
            trackedTargetPos.y = Mathf.Lerp(trackedTargetPos.y, -0.2f, 0.90f);
        }

        Vector3 desiredPosition = trackedTargetPos + currentOffset;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref positionVelocity, positionSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);

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

    private void HandlePulpitDestroyed(Vector3 pos) => TriggerShake(0.18f);
    private void HandleMilestone(int milestone) => TriggerShake(0.35f);
    private void HandleRewindStart() => targetOffset = defaultOffset * rewindZoomFactor;
    private void HandleRewindComplete() => targetOffset = defaultOffset;

    private void HandleGameReset()
    {
        targetOffset = defaultOffset;
        currentOffset = defaultOffset;
        positionVelocity = Vector3.zero;
        zoomVelocity = Vector3.zero;
        currentShakeIntensity = 0f;
    }
}
