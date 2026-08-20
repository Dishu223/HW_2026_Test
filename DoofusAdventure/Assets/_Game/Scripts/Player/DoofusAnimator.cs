using System.Collections;
using UnityEngine;

/// <summary>
/// Cartoon procedural animator with organic inertia follow-through and overshoot:
/// - Smooth acceleration lean and head drag
/// - Continuous rhythmic back-and-forth head bobbing while running
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
    [SerializeField] private float runLeanBackwardAngle = 22f;
    [SerializeField] private float stopOvershootAngle = 18f;
    [SerializeField] private float leanSmoothSpeed = 10f;

    [Header("Head Movement & Continuous Running Bob")]
    [SerializeField] private float headRunLagDistance = 0.22f;   // Base head pull-back
    [SerializeField] private float headRunBobSpeed = 16f;        // Rhythm frequency of running bob
    [SerializeField] private float headRunBobAmount = 0.08f;     // Back-and-forth nodding intensity
    [SerializeField] private float headStopOvershoot = 0.25f;    // Forward whip on stop
    [SerializeField] private float headSpringTime = 0.10f;

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

        if (wasMovingLastFrame && !isMoving)
        {
            stopOvershootTimer = OVERSHOOT_DURATION;
        }
        wasMovingLastFrame = isMoving;

        if (stopOvershootTimer > 0f)
        {
            stopOvershootTimer -= Time.deltaTime;
        }

        ApplyMovementAndBraking(moveInput, isMoving);
        ApplyHeadSpringAndBobbing(moveInput, isMoving);
        ApplyIdleBounce(isMoving);
    }

    private void ApplyMovementAndBraking(Vector3 moveInput, bool isMoving)
    {
        if (isMoving)
        {
            Quaternion targetLook = Quaternion.LookRotation(moveInput, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetLook, Time.deltaTime * turnSpeed);
        }

        if (bodyTransform != null)
        {
            Quaternion targetLean = Quaternion.identity;

            if (isMoving)
            {
                targetLean = Quaternion.Euler(runLeanBackwardAngle, 0f, 0f);
            }
            else if (stopOvershootTimer > 0f)
            {
                float progress = 1f - (stopOvershootTimer / OVERSHOOT_DURATION);
                float springFactor = Mathf.Sin(progress * Mathf.PI * 2f) * (1f - progress);
                float overshootAngle = -stopOvershootAngle * springFactor;
                targetLean = Quaternion.Euler(overshootAngle, 0f, 0f);
            }

            bodyTransform.localRotation = Quaternion.Slerp(bodyTransform.localRotation, targetLean, Time.deltaTime * leanSmoothSpeed);
        }
    }

    /// <summary>
    /// Handles continuous rhythmic running bobble, head pull-back, and stopping forward whip.
    /// </summary>
    private void ApplyHeadSpringAndBobbing(Vector3 moveInput, bool isMoving)
    {
        if (headTransform == null) return;

        Vector3 targetHeadPos = defaultHeadLocalPos;

        if (isMoving)
        {
            // Continuous back-and-forth nodding while running
            float rhythmicBobZ = Mathf.Sin(Time.time * headRunBobSpeed) * headRunBobAmount;
            float rhythmicBobY = Mathf.Abs(Mathf.Cos(Time.time * headRunBobSpeed)) * 0.04f;

            targetHeadPos = defaultHeadLocalPos + new Vector3(0f, -0.03f + rhythmicBobY, -headRunLagDistance + rhythmicBobZ);
        }
        else if (stopOvershootTimer > 0f)
        {
            // Forward whip on stop
            float progress = 1f - (stopOvershootTimer / OVERSHOOT_DURATION);
            float springFactor = Mathf.Sin(progress * Mathf.PI * 2.5f) * (1f - progress);
            float forwardOffset = headStopOvershoot * springFactor;
            targetHeadPos = defaultHeadLocalPos + new Vector3(0f, -0.02f, forwardOffset);
        }

        headTransform.localPosition = Vector3.SmoothDamp(headTransform.localPosition, targetHeadPos, ref headVelocity, headSpringTime);
    }

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
            scaleMultiplier = 1.75f + Mathf.Sin(Time.time * 30f) * 0.2f;
        }
        else if (normalizedTime < 0.5f)
        {
            scaleMultiplier = 1.35f;
        }
        else
        {
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
