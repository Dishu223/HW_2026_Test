using System.Collections;
using UnityEngine;

/// <summary>
/// Cartoon procedural animator for Doofus:
/// - Smooth acceleration lean and head drag
/// - Continuous rhythmic back-and-forth head bobbing while running
/// - Exaggerated stopping brake & forward whip
/// - Cute expressive eyes: idle periodic blinking, wide excited running eyes, and cute stop-blinks!
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
    [SerializeField] private float headRunLagDistance = 0.22f;
    [SerializeField] private float headRunBobSpeed = 16f;
    [SerializeField] private float headRunBobAmount = 0.08f;
    [SerializeField] private float headStopOvershoot = 0.25f;
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

    // Eye animation state
    private float nextBlinkTime = 2.5f;
    private float blinkTimer = 0f;
    private bool isBlinking = false;
    private Coroutine blinkRoutine;
    private Coroutine squashRoutine;
    private bool isFalling = false;
    private float currentPulpitTimer = 1f;

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

        nextBlinkTime = Random.Range(2.5f, 4.5f);
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
            stopOvershootTimer = OVERSHOOT_DURATION;
            // Cute instant blink on stopping!
            TriggerBlink(0.08f);
        }
        wasMovingLastFrame = isMoving;

        if (stopOvershootTimer > 0f)
        {
            stopOvershootTimer -= Time.deltaTime;
        }

        ApplyMovementAndBraking(moveInput, isMoving);
        ApplyHeadSpringAndBobbing(moveInput, isMoving);
        ApplyIdleBounce(isMoving);
        UpdateEyeExpressions(isMoving);
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

    private void ApplyHeadSpringAndBobbing(Vector3 moveInput, bool isMoving)
    {
        if (headTransform == null) return;

        Vector3 targetHeadPos = defaultHeadLocalPos;

        if (isMoving)
        {
            float rhythmicBobZ = Mathf.Sin(Time.time * headRunBobSpeed) * headRunBobAmount;
            float rhythmicBobY = Mathf.Abs(Mathf.Cos(Time.time * headRunBobSpeed)) * 0.04f;
            targetHeadPos = defaultHeadLocalPos + new Vector3(0f, -0.03f + rhythmicBobY, -headRunLagDistance + rhythmicBobZ);
        }
        else if (stopOvershootTimer > 0f)
        {
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

    /// <summary>
    /// Updates eye scales based on state:
    /// - Running: Wide excited cartoon eyes (1.38x)
    /// - Idle: Natural scale with periodic cute blinking
    /// - Low Platform Timer: Panicked bulging tremor
    /// </summary>
    private void UpdateEyeExpressions(bool isMoving)
    {
        if (leftEyeTransform == null || rightEyeTransform == null || isFalling || isBlinking) return;

        // Periodic natural idle blinking
        if (!isMoving)
        {
            blinkTimer += Time.deltaTime;
            if (blinkTimer >= nextBlinkTime)
            {
                blinkTimer = 0f;
                nextBlinkTime = Random.Range(2.5f, 4.5f);
                TriggerBlink(0.12f);
                return;
            }
        }
        else
        {
            blinkTimer = 0f;
        }

        // Base Scale Multiplier
        float scaleMultiplier = 1f;

        if (currentPulpitTimer < 0.25f)
        {
            // Panicked state
            scaleMultiplier = 1.8f + Mathf.Sin(Time.time * 30f) * 0.2f;
        }
        else if (currentPulpitTimer < 0.5f)
        {
            // Worried state
            scaleMultiplier = 1.35f;
        }
        else if (isMoving)
        {
            // Wide excited eyes while running!
            scaleMultiplier = 1.4f;
        }
        else
        {
            scaleMultiplier = 1f;
        }

        Vector3 targetScale = defaultEyeScale * scaleMultiplier;
        leftEyeTransform.localScale = Vector3.Lerp(leftEyeTransform.localScale, targetScale, Time.deltaTime * 14f);
        rightEyeTransform.localScale = Vector3.Lerp(rightEyeTransform.localScale, targetScale, Time.deltaTime * 14f);
    }

    public void TriggerBlink(float blinkDuration = 0.1f)
    {
        if (!gameObject.activeInHierarchy) return;

        if (blinkRoutine != null) StopCoroutine(blinkRoutine);
        blinkRoutine = StartCoroutine(BlinkCoroutine(blinkDuration));
    }

    private IEnumerator BlinkCoroutine(float duration)
    {
        isBlinking = true;
        Vector3 currentScale = leftEyeTransform != null ? leftEyeTransform.localScale : defaultEyeScale;
        Vector3 closedScale = new Vector3(currentScale.x * 1.2f, currentScale.y * 0.08f, currentScale.z);
        float halfDuration = duration / 2f;

        // Close eyes
        float elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            Vector3 s = Vector3.Lerp(currentScale, closedScale, elapsed / halfDuration);
            if (leftEyeTransform != null) leftEyeTransform.localScale = s;
            if (rightEyeTransform != null) rightEyeTransform.localScale = s;
            yield return null;
        }

        // Open eyes back
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            Vector3 s = Vector3.Lerp(closedScale, currentScale, elapsed / halfDuration);
            if (leftEyeTransform != null) leftEyeTransform.localScale = s;
            if (rightEyeTransform != null) rightEyeTransform.localScale = s;
            yield return null;
        }

        isBlinking = false;
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
        currentPulpitTimer = normalizedTime;
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
