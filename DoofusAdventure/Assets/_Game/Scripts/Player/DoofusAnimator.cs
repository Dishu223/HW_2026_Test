using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cartoon procedural animator for Doofus:
/// - Smooth acceleration lean and head drag
/// - Continuous rhythmic back-and-forth head bobbing while running
/// - Exaggerated stopping brake & forward whip
/// - High-visibility cartoon eyes
/// - Blinking warning flashes on Rewind and Game Over!
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
    private Coroutine characterFlashRoutine;
    private bool isFalling = false;
    private float currentPulpitTimer = 1f;

    private readonly List<Renderer> characterRenderers = new List<Renderer>();

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

        // Cache all child renderers for character blink flashes
        characterRenderers.AddRange(GetComponentsInChildren<Renderer>());
    }

    private void OnEnable()
    {
        GameEvents.OnPulpitLanded += TriggerLandSquash;
        GameEvents.OnPulpitTimerTick += HandleTimerTickForEyes;
        GameEvents.OnDoofusFell += HandleDoofusFell;
        GameEvents.OnGameStart += HandleGameStart;
        GameEvents.OnRewindStart += HandleRewindStart;
        GameEvents.OnGameOver += HandleGameOver;
    }

    private void OnDisable()
    {
        GameEvents.OnPulpitLanded -= TriggerLandSquash;
        GameEvents.OnPulpitTimerTick -= HandleTimerTickForEyes;
        GameEvents.OnDoofusFell -= HandleDoofusFell;
        GameEvents.OnGameStart -= HandleGameStart;
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

    private void UpdateEyeExpressions(bool isMoving)
    {
        if (leftEyeTransform == null || rightEyeTransform == null || isFalling) return;

        if (!isMoving && !isBlinking)
        {
            blinkTimer += Time.deltaTime;
            if (blinkTimer >= nextBlinkTime)
            {
                blinkTimer = 0f;
                nextBlinkTime = Random.Range(2.0f, 3.5f);
                TriggerBlink(0.20f);
            }
        }
        else if (isMoving)
        {
            blinkTimer = 0f;
        }

        if (isBlinking) return;

        float scaleMultiplier = 1f;

        if (currentPulpitTimer < 0.25f)
        {
            scaleMultiplier = 1.8f + Mathf.Sin(Time.time * 30f) * 0.2f;
        }
        else if (currentPulpitTimer < 0.5f)
        {
            scaleMultiplier = 1.35f;
        }
        else if (isMoving)
        {
            scaleMultiplier = 1.45f;
        }
        else
        {
            scaleMultiplier = 1f;
        }

        Vector3 targetScale = defaultEyeScale * scaleMultiplier;
        leftEyeTransform.localScale = Vector3.Lerp(leftEyeTransform.localScale, targetScale, Time.deltaTime * 14f);
        rightEyeTransform.localScale = Vector3.Lerp(rightEyeTransform.localScale, targetScale, Time.deltaTime * 14f);
    }

    public void TriggerBlink(float duration = 0.20f)
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

    #region Character Blinking Flashes
    private void TriggerCharacterBlinkFlashes(int flashCount = 4, float interval = 0.08f)
    {
        if (!gameObject.activeInHierarchy) return;

        if (characterFlashRoutine != null) StopCoroutine(characterFlashRoutine);
        characterFlashRoutine = StartCoroutine(CharacterFlashCoroutine(flashCount, interval));
    }

    private IEnumerator CharacterFlashCoroutine(int flashCount, float interval)
    {
        for (int i = 0; i < flashCount; i++)
        {
            SetRenderersVisibility(false);
            yield return new WaitForSecondsRealtime(interval);
            SetRenderersVisibility(true);
            yield return new WaitForSecondsRealtime(interval);
        }
        SetRenderersVisibility(true);
    }

    private void SetRenderersVisibility(bool visible)
    {
        foreach (Renderer r in characterRenderers)
        {
            if (r != null) r.enabled = visible;
        }
    }
    #endregion

    private void HandleRewindStart()
    {
        TriggerCharacterBlinkFlashes(4, 0.07f);
    }

    private void HandleGameOver()
    {
        TriggerCharacterBlinkFlashes(5, 0.08f);
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
        transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        transform.localScale = Vector3.one;
        SetRenderersVisibility(true);

        if (leftEyeTransform != null) leftEyeTransform.localScale = defaultEyeScale;
        if (rightEyeTransform != null) rightEyeTransform.localScale = defaultEyeScale;
    }
}
