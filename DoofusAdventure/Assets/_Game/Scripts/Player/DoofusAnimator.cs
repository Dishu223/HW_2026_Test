using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cartoon procedural animator for Doofus:
/// - Smooth acceleration lean and head drag
/// - Continuous rhythmic back-and-forth head bobbing while running
/// - Exaggerated stopping brake & forward whip
/// - High-visibility cartoon eyes
/// - Buttery smooth falling tumble into the void!
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

    private float nextBlinkTime = 2.0f;
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

        nextBlinkTime = Random.Range(2.0f, 3.5f);
    }

    private void OnEnable()
    {
        GameEvents.OnPulpitLanded += TriggerLandSquash;
        GameEvents.OnPulpitTimerTick += HandleTimerTickForEyes;
        GameEvents.OnDoofusFell += HandleDoofusFell;
        GameEvents.OnGameStart += HandleGameStart;
        GameEvents.OnGameRestart += HandleGameStart;
        GameEvents.OnRewindStart += HandleRewindStart;
        GameEvents.OnGameOver += HandleGameOver;
    }

    private void OnDisable()
    {
        GameEvents.OnPulpitLanded -= TriggerLandSquash;
        GameEvents.OnPulpitTimerTick -= HandleTimerTickForEyes;
        GameEvents.OnDoofusFell -= HandleDoofusFell;
        GameEvents.OnGameStart -= HandleGameStart;
        GameEvents.OnGameRestart -= HandleGameStart;
        GameEvents.OnRewindStart -= HandleRewindStart;
        GameEvents.OnGameOver -= HandleGameOver;
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
            TriggerBlink(0.20f);
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
                float angle = Mathf.Sin(progress * Mathf.PI) * stopOvershootAngle;
                targetLean = Quaternion.Euler(-angle, 0f, 0f);
            }

            bodyTransform.localRotation = Quaternion.Slerp(bodyTransform.localRotation, targetLean, Time.deltaTime * leanSmoothSpeed);
        }
    }

    private void ApplyHeadSpringAndBobbing(Vector3 moveInput, bool isMoving)
    {
        if (headTransform == null) return;

        Vector3 targetHeadLocalPos = defaultHeadLocalPos;

        if (isMoving)
        {
            targetHeadLocalPos.z -= headRunLagDistance;
            float bob = Mathf.Sin(Time.time * headRunBobSpeed) * headRunBobAmount;
            targetHeadLocalPos.y += bob;
        }
        else if (stopOvershootTimer > 0f)
        {
            float progress = 1f - (stopOvershootTimer / OVERSHOOT_DURATION);
            float overshootZ = Mathf.Sin(progress * Mathf.PI) * headStopOvershoot;
            targetHeadLocalPos.z += overshootZ;
        }

        headTransform.localPosition = Vector3.SmoothDamp(
            headTransform.localPosition,
            targetHeadLocalPos,
            ref headVelocity,
            headSpringTime
        );
    }

    private void ApplyIdleBounce(bool isMoving)
    {
        if (isMoving || bodyTransform == null) return;

        float bounce = Mathf.Sin(Time.time * idleBounceSpeed) * idleBounceHeight;
        bodyTransform.localPosition = defaultBodyLocalPos + new Vector3(0f, bounce, 0f);
    }

    private void UpdateEyeExpressions(bool isMoving)
    {
        if (leftEyeTransform == null || rightEyeTransform == null) return;

        if (!isBlinking)
        {
            blinkTimer += Time.deltaTime;
            if (blinkTimer >= nextBlinkTime)
            {
                blinkTimer = 0f;
                nextBlinkTime = Random.Range(2.5f, 4.5f);
                TriggerBlink(0.18f);
            }
        }

        if (!isBlinking)
        {
            Vector3 targetScale = defaultEyeScale;

            if (currentPulpitTimer < 0.35f)
            {
                float panicT = 1f - (currentPulpitTimer / 0.35f);
                float panicScale = 1.0f + panicT * 0.50f;
                targetScale = new Vector3(defaultEyeScale.x * panicScale, defaultEyeScale.y * panicScale, defaultEyeScale.z);
            }
            else if (isMoving)
            {
                targetScale = new Vector3(defaultEyeScale.x * 1.25f, defaultEyeScale.y * 1.25f, defaultEyeScale.z);
            }

            leftEyeTransform.localScale = Vector3.Lerp(leftEyeTransform.localScale, targetScale, Time.deltaTime * 12f);
            rightEyeTransform.localScale = Vector3.Lerp(rightEyeTransform.localScale, targetScale, Time.deltaTime * 12f);
        }
    }

    private void TriggerBlink(float duration)
    {
        if (!gameObject.activeInHierarchy) return;

        if (blinkRoutine != null) StopCoroutine(blinkRoutine);
        blinkRoutine = StartCoroutine(HighVisibilityBlinkCoroutine(duration));
    }

    private IEnumerator HighVisibilityBlinkCoroutine(float totalDuration)
    {
        isBlinking = true;

        Vector3 openScale = leftEyeTransform != null ? leftEyeTransform.localScale : defaultEyeScale;
        Vector3 closedSlitScale = new Vector3(defaultEyeScale.x * 1.5f, 0.02f, defaultEyeScale.z * 1.1f);

        float closeTime = totalDuration * 0.35f;
        float holdTime = totalDuration * 0.25f;
        float openTime = totalDuration * 0.40f;

        float elapsed = 0f;
        while (elapsed < closeTime)
        {
            elapsed += Time.deltaTime;
            Vector3 s = Vector3.Lerp(openScale, closedSlitScale, elapsed / closeTime);
            SetEyeScale(s);
            yield return null;
        }
        SetEyeScale(closedSlitScale);

        yield return new WaitForSeconds(holdTime);

        Vector3 overshootScale = new Vector3(openScale.x * 0.95f, openScale.y * 1.25f, openScale.z);
        elapsed = 0f;
        while (elapsed < openTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / openTime;
            Vector3 s = t < 0.6f 
                ? Vector3.Lerp(closedSlitScale, overshootScale, t / 0.6f) 
                : Vector3.Lerp(overshootScale, openScale, (t - 0.6f) / 0.4f);

            SetEyeScale(s);
            yield return null;
        }

        SetEyeScale(openScale);
        isBlinking = false;
    }

    private void SetEyeScale(Vector3 scale)
    {
        if (leftEyeTransform != null) leftEyeTransform.localScale = scale;
        if (rightEyeTransform != null) rightEyeTransform.localScale = scale;
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

    private void HandleRewindStart()
    {
        isFalling = false;
    }

    private void HandleGameOver()
    {
        // Keep mesh continuously visible for smooth physics fall
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
        // Smooth World-Space gentle rotation while falling
        transform.Rotate(new Vector3(110f, 65f, 40f) * Time.deltaTime, Space.World);
    }

    private void HandleGameStart()
    {
        isFalling = false;
        transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        transform.localScale = Vector3.one;

        if (leftEyeTransform != null) leftEyeTransform.localScale = defaultEyeScale;
        if (rightEyeTransform != null) rightEyeTransform.localScale = defaultEyeScale;
    }
}
