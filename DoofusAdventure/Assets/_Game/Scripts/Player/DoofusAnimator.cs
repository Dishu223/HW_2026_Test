using System.Collections;
using UnityEngine;

/// <summary>
/// Cartoon procedural animator with organic inertia follow-through and overshoot:
/// - Smooth acceleration lean and head drag
/// - Exaggerated stopping brake: body pitches forward, head whips forward with jelly spring decay
/// - Dynamic eye expressions & landing squash
/// </summary>
public class DoofusAnimator : MonoBehaviour
{
    [Header("Body Hierarchy References")]
    [SerializeField] private Transform bodyTransform;
    [SerializeField] private Transform headTransform;
    [SerializeField] private Transform leftEyeTransform;
    [SerializeField] private Transform rightEyeTransform;

    [Header("Turning & Acceleration Lean")]
    [SerializeField] private float turnSpeed = 18f;
    [SerializeField] private float runLeanBackwardAngle = 22f; // Lean back while running
    [SerializeField] private float stopOvershootAngle = 18f;     // Pitch forward when braking
    [SerializeField] private float leanSmoothSpeed = 10f;

    [Header("Head Spring Physics")]
    [SerializeField] private float headRunLagDistance = 0.22f;   // Head drags back while running
    [SerializeField] private float headStopOvershoot = 0.25f;    // Head whips forward on stop
    [SerializeField] private float headSpringTime = 0.12f;

    [Header("Idle Breathing")]
    [SerializeField] private float idleBounceSpeed = 4f;
    [SerializeField] private float idleBounceHeight = 0.04f;

    [Header("Squash & Stretch")]
    [SerializeField] private float squashAmount = 0.72f;
    [SerializeField] private float squashDuration = 0.22f;

    private DoofusController controller;
    private Vector3 defaultBodyLocalPos;
    private Vector3 defaultHeadLocalPos;
    private Vector3 defaultEyeScale;
    private Vector3 headVelocity;

    private bool wasMovingLastFrame = false;
    private float stopOvershootTimer = 0f;
    private const float OVERSHOOT_DURATION = 0.35f;

    private Coroutine squashRoutine;
    private bool isFalling = false;

    private void Awake()
    {
        controller = GetComponent<DoofusController>();

        if (bodyTransform != null)
            defaultBodyLocalPos = bodyTransform.localPosition;
        else
            defaultBodyLocalPos = new Vector3(0f, 0.5f, 0f);

        if (headTransform != null)
            defaultHeadLocalPos = headTransform.localPosition;
        else
            defaultHeadLocalPos = new Vector3(0f, 1.35f, 0f);

        if (leftEyeTransform != null)
            defaultEyeScale = leftEyeTransform.localScale;
        else
            defaultEyeScale = new Vector3(0.16f, 0.16f, 0.16f);
    }

    private void OnEnable()
    {
        GameEvents.OnPulpitLanded += TriggerLandSquash;
        GameEvents.OnPulpitTimerTick += HandleTimerTickForEyes;
        GameEvents.OnDoofusFell += HandleDoofusFell;
        GameEvents.OnGameStart += HandleGameStart;
    }

    private void OnDisable()
    {
        GameEvents.OnPulpitLanded -= TriggerLandSquash;
        GameEvents.OnPulpitTimerTick -= HandleTimerTickForEyes;
        GameEvents.OnDoofusFell -= HandleDoofusFell;
        GameEvents.OnGameStart -= HandleGameStart;
    }

    private void Update()
    {
        if (isFalling)
        {
            ApplyFallingTumble();
            return;
        }

        Vector3 moveInput = controller != null ? controller.MoveInput : Vector3.zero;
        bool isMoving = moveInput.sqrMagnitude > 0.01f;

        // Detect stopping moment (transition from moving to stopped)
        if (wasMovingLastFrame && !isMoving)
        {
            stopOvershootTimer = OVERSHOOT_DURATION; // Trigger the forward brake whip
        }
        wasMovingLastFrame = isMoving;

        if (stopOvershootTimer > 0f)
        {
            stopOvershootTimer -= Time.deltaTime;
        }

        ApplyMovementAndBraking(moveInput, isMoving);
        ApplyHeadSpringAndOvershoot(moveInput, isMoving);
        ApplyIdleBounce(isMoving);
    }

    /// <summary>
    /// Smoothly rotates character towards movement, leans back during run,
    /// and pitches forward with spring damping when stopping.
    /// </summary>
    private void ApplyMovementAndBraking(Vector3 moveInput, bool isMoving)
    {
        // 1. Smooth rotation
        if (isMoving)
        {
            Quaternion targetLook = Quaternion.LookRotation(moveInput, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetLook, Time.deltaTime * turnSpeed);
        }

        // 2. Body Lean / Brake Overshoot
        if (bodyTransform != null)
        {
            Quaternion targetLean = Quaternion.identity;

            if (isMoving)
            {
                // Smoothly lean back while running
                targetLean = Quaternion.Euler(runLeanBackwardAngle, 0f, 0f);
            }
            else if (stopOvershootTimer > 0f)
            {
                // Brake Overshoot: pitch forward with decaying spring oscillation
                float progress = 1f - (stopOvershootTimer / OVERSHOOT_DURATION);
                float springFactor = Mathf.Sin(progress * Mathf.PI * 2f) * (1f - progress);
                float overshootAngle = -stopOvershootAngle * springFactor; // Negative X = pitch forward
                targetLean = Quaternion.Euler(overshootAngle, 0f, 0f);
            }

            bodyTransform.localRotation = Quaternion.Slerp(bodyTransform.localRotation, targetLean, Time.deltaTime * leanSmoothSpeed);
        }
    }

    /// <summary>
    /// Head drags backwards while moving, and whips forward on sudden stop with organic elasticity.
    /// </summary>
    private void ApplyHeadSpringAndOvershoot(Vector3 moveInput, bool isMoving)
    {
        if (headTransform == null) return;

        Vector3 targetHeadPos = defaultHeadLocalPos;

        if (isMoving)
        {
            // Head drags back smoothly
            targetHeadPos = defaultHeadLocalPos + new Vector3(0f, -0.04f, -headRunLagDistance);
        }
        else if (stopOvershootTimer > 0f)
        {
            // Head whips forward with decaying bounce
            float progress = 1f - (stopOvershootTimer / OVERSHOOT_DURATION);
            float springFactor = Mathf.Sin(progress * Mathf.PI * 2.5f) * (1f - progress);
            float forwardOffset = headStopOvershoot * springFactor;
            targetHeadPos = defaultHeadLocalPos + new Vector3(0f, -0.02f, forwardOffset);
        }

        headTransform.localPosition = Vector3.SmoothDamp(headTransform.localPosition, targetHeadPos, ref headVelocity, headSpringTime);
    }

    /// <summary>
    /// Subtle breathing bounce when completely at rest.
    /// </summary>
    private void ApplyIdleBounce(bool isMoving)
    {
        if (bodyTransform == null) return;

        float targetBounce = (isMoving || stopOvershootTimer > 0f) ? 0f : Mathf.Sin(Time.time * idleBounceSpeed) * idleBounceHeight;
        Vector3 targetPos = defaultBodyLocalPos + new Vector3(0f, targetBounce, 0f);
        bodyTransform.localPosition = Vector3.Lerp(bodyTransform.localPosition, targetPos, Time.deltaTime * 8f);
    }

    private void TriggerLandSquash()
    {
        if (!gameObject.activeInHierarchy) return;

        if (squashRoutine != null) StopCoroutine(squashRoutine);
        squashRoutine = StartCoroutine(SquashAndStretchCoroutine());
    }

    private IEnumerator SquashAndStretchCoroutine()
    {
        Vector3 originalScale = Vector3.one;
        Vector3 squashedScale = new Vector3(1.25f, squashAmount, 1.25f);
        float halfDuration = squashDuration / 2f;

        float elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(originalScale, squashedScale, elapsed / halfDuration);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(squashedScale, originalScale, elapsed / halfDuration);
            yield return null;
        }

        transform.localScale = originalScale;
    }

    private void HandleTimerTickForEyes(float normalizedTime)
    {
        if (leftEyeTransform == null || rightEyeTransform == null || isFalling) return;

        float scaleMultiplier = 1f;

        if (normalizedTime < 0.25f)
        {
            // Panicked state: bulging eyes with fast vibration
            scaleMultiplier = 1.75f + Mathf.Sin(Time.time * 30f) * 0.2f;
        }
        else if (normalizedTime < 0.5f)
        {
            // Worried state
            scaleMultiplier = 1.35f;
        }
        else
        {
            // Happy normal
            scaleMultiplier = 1f;
        }

        Vector3 targetScale = defaultEyeScale * scaleMultiplier;
        leftEyeTransform.localScale = Vector3.Lerp(leftEyeTransform.localScale, targetScale, Time.deltaTime * 12f);
        rightEyeTransform.localScale = Vector3.Lerp(rightEyeTransform.localScale, targetScale, Time.deltaTime * 12f);
    }

    private void HandleDoofusFell()
    {
        isFalling = true;

        if (leftEyeTransform != null) leftEyeTransform.localScale = new Vector3(defaultEyeScale.x * 1.5f, defaultEyeScale.y * 0.1f, defaultEyeScale.z);
        if (rightEyeTransform != null) rightEyeTransform.localScale = new Vector3(defaultEyeScale.x * 1.5f, defaultEyeScale.y * 0.1f, defaultEyeScale.z);
    }

    private void ApplyFallingTumble()
    {
        transform.Rotate(new Vector3(220f, 120f, 60f) * Time.deltaTime, Space.Self);
    }

    private void HandleGameStart()
    {
        isFalling = false;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        if (leftEyeTransform != null) leftEyeTransform.localScale = defaultEyeScale;
        if (rightEyeTransform != null) rightEyeTransform.localScale = defaultEyeScale;
    }
}
