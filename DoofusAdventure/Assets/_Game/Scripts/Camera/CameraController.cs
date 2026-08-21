using UnityEngine;

/// <summary>
/// Smooth Isometric Camera Controller:
/// - Velocity-responsive banking & roll sway (subtly tilts into movement direction)
/// - Organic cinematic breathing sway (gentle harmonic float)
/// - Buttery-smooth position lag & spring follow
/// - Seamless zoom transition during Prince of Persia Time Rewind
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Follow Target")]
    [SerializeField] private Transform target;

    [Header("Isometric Camera Offset")]
    [SerializeField] private Vector3 defaultOffset = new Vector3(0f, 11f, -13f);
    [SerializeField] private float positionSmoothTime = 0.22f;

    [Header("Camera Sway & Dynamic Roll")]
    [SerializeField] private float bankAngleMax = 2.4f;       // Subtle roll tilt when moving laterally
    [SerializeField] private float pitchLagMax = 1.8f;        // Subtle pitch lag when moving forward
    [SerializeField] private float swaySmoothTime = 0.20f;
    [SerializeField] private float idleBreathingAmplitude = 0.35f; // Organic breathing float

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

    private Vector3 lastTargetPos;
    private Vector3 targetVelocity;
    private float currentRoll = 0f;
    private float currentPitch = 38f;
    private float rollVelocity = 0f;
    private float pitchVelocity = 0f;

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
            lastTargetPos = target.position;
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

        // Calculate target velocity for responsive sway
        float dt = Time.unscaledDeltaTime;
        if (dt > 0.0001f)
        {
            targetVelocity = (target.position - lastTargetPos) / dt;
            lastTargetPos = target.position;
        }

        // 1. Smooth Zoom Offset Transition
        currentOffset = Vector3.SmoothDamp(currentOffset, targetOffset, ref zoomVelocity, zoomSmoothTime, Mathf.Infinity, dt);

        // 2. Position Follow with Fall Overlook
        Vector3 trackedTargetPos = target.position;
        if (trackedTargetPos.y < -0.2f)
        {
            trackedTargetPos.y = Mathf.Lerp(trackedTargetPos.y, -0.2f, 0.90f);
        }

        Vector3 desiredPosition = trackedTargetPos + currentOffset;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref positionVelocity, positionSmoothTime, Mathf.Infinity, dt);

        // 3. Screen Shake
        if (currentShakeIntensity > 0.01f)
        {
            shakeOffset = Random.insideUnitSphere * currentShakeIntensity;
            shakeOffset.z = 0f;
            transform.position += shakeOffset;
            currentShakeIntensity = Mathf.Lerp(currentShakeIntensity, 0f, dt * shakeDamping);
        }

        // 4. Subtle Premium Camera Sway & Banking Roll
        // Bank left/right based on horizontal velocity
        float desiredRoll = -Mathf.Clamp(targetVelocity.x * 0.45f, -bankAngleMax, bankAngleMax);

        // Subtle pitch lag based on forward/backward velocity
        float desiredPitch = 38f + Mathf.Clamp(targetVelocity.z * 0.25f, -pitchLagMax, pitchLagMax);

        // Organic idle breathing float (harmonic sway)
        float breathRoll = Mathf.Sin(Time.time * 0.9f) * idleBreathingAmplitude;
        float breathPitch = Mathf.Cos(Time.time * 0.7f) * (idleBreathingAmplitude * 0.6f);

        desiredRoll += breathRoll;
        desiredPitch += breathPitch;

        currentRoll = Mathf.SmoothDamp(currentRoll, desiredRoll, ref rollVelocity, swaySmoothTime, Mathf.Infinity, dt);
        currentPitch = Mathf.SmoothDamp(currentPitch, desiredPitch, ref pitchVelocity, swaySmoothTime, Mathf.Infinity, dt);

        transform.rotation = Quaternion.Euler(currentPitch, 0f, currentRoll);
    }

    private void HandlePulpitDestroyed(Vector3 pos) => TriggerShake(0.18f);
    private void HandleMilestone(int milestone) => TriggerShake(0.25f);
    private void HandleRewindStart() => targetOffset = defaultOffset * rewindZoomFactor;
    private void HandleRewindComplete() => targetOffset = defaultOffset;

    private void HandleGameReset()
    {
        targetOffset = defaultOffset;
        currentShakeIntensity = 0f;
        FindTarget();
        if (target != null)
        {
            lastTargetPos = target.position;
            transform.position = target.position + defaultOffset;
        }
    }

    public void TriggerShake(float intensity)
    {
        currentShakeIntensity = Mathf.Max(currentShakeIntensity, intensity);
    }
}
