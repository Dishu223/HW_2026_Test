using System.Collections;
using UnityEngine;

/// <summary>
/// Procedural animation controller for Doofus (Snowman character).
/// Handles movement lean, springy head bobble, squash & stretch on landing,
/// idle breathing bounce, and expressive eye reactions based on platform timer.
/// (Requires zero animator clips / states!).
/// </summary>
public class DoofusAnimator : MonoBehaviour
{
    [Header("Body Hierarchy References")]
    [SerializeField] private Transform bodyTransform;
    [SerializeField] private Transform headTransform;
    [SerializeField] private Transform leftEyeTransform;
    [SerializeField] private Transform rightEyeTransform;

    [Header("Procedural Wobble & Lean")]
    [SerializeField] private float maxLeanAngle = 18f;
    [SerializeField] private float leanSmoothSpeed = 12f;
    [SerializeField] private float headLagSpeed = 14f;

    [Header("Idle Breathing")]
    [SerializeField] private float idleBounceSpeed = 3.5f;
    [SerializeField] private float idleBounceHeight = 0.06f;

    [Header("Squash & Stretch")]
    [SerializeField] private float squashAmount = 0.78f;
    [SerializeField] private float squashDuration = 0.25f;

    private Rigidbody rb;
    private Vector3 defaultHeadLocalPos;
    private Vector3 defaultEyeScale;
    private Vector3 headVelocity;
    private Coroutine squashRoutine;
    private bool isFalling = false;

    private void Awake()
    {
        rb = GetComponentInParent<Rigidbody>();

        if (headTransform != null)
            defaultHeadLocalPos = headTransform.localPosition;

        if (leftEyeTransform != null)
            defaultEyeScale = leftEyeTransform.localScale;
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

        ApplyMovementLean();
        ApplyHeadBobble();
        ApplyIdleBounce();
    }

    /// <summary>
    /// Leans the body opposite/aligned with movement direction for cartoon inertia.
    /// </summary>
    private void ApplyMovementLean()
    {
        if (bodyTransform == null || rb == null) return;

        // Get movement velocity on horizontal plane
        Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        float speed = horizontalVel.magnitude;

        Quaternion targetRotation = Quaternion.identity;

        if (speed > 0.1f)
        {
            // Lean slightly backwards/sideways based on velocity
            Vector3 tiltAxis = Vector3.Cross(Vector3.up, horizontalVel.normalized);
            targetRotation = Quaternion.AngleAxis(-maxLeanAngle * Mathf.Clamp01(speed / 3f), tiltAxis);
        }

        bodyTransform.localRotation = Quaternion.Slerp(bodyTransform.localRotation, targetRotation, Time.deltaTime * leanSmoothSpeed);
    }

    /// <summary>
    /// Simulates organic spring physics on the head as the body shifts.
    /// </summary>
    private void ApplyHeadBobble()
    {
        if (headTransform == null || bodyTransform == null) return;

        // Head lags slightly behind body position
        Vector3 targetLocalPos = defaultHeadLocalPos;
        headTransform.localPosition = Vector3.SmoothDamp(headTransform.localPosition, targetLocalPos, ref headVelocity, 1f / headLagSpeed);
    }

    /// <summary>
    /// Subtle sine-wave breathing bounce when stationary.
    /// </summary>
    private void ApplyIdleBounce()
    {
        if (bodyTransform == null) return;

        float bounce = Mathf.Sin(Time.time * idleBounceSpeed) * idleBounceHeight;
        bodyTransform.localPosition = new Vector3(0f, bounce, 0f);
    }

    /// <summary>
    /// Dynamic squash-and-stretch on landing.
    /// </summary>
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

        // Squash Down
        float elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(originalScale, squashedScale, elapsed / halfDuration);
            yield return null;
        }

        // Spring back Up
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(squashedScale, originalScale, elapsed / halfDuration);
            yield return null;
        }

        transform.localScale = originalScale;
    }

    /// <summary>
    /// Expressive eyes reaction based on platform timer:
    /// > 50%: Normal Happy
    /// 25% - 50%: Worried (slightly widened)
    /// < 25%: Panicked (eyes bulge huge!)
    /// </summary>
    private void HandleTimerTickForEyes(float normalizedTime)
    {
        if (leftEyeTransform == null || rightEyeTransform == null || isFalling) return;

        float scaleMultiplier = 1f;

        if (normalizedTime < 0.25f)
        {
            // Panicked state: eyes bulge out wide and shake
            scaleMultiplier = 1.7f + Mathf.Sin(Time.time * 25f) * 0.15f;
        }
        else if (normalizedTime < 0.5f)
        {
            // Worried state: slightly enlarged
            scaleMultiplier = 1.3f;
        }
        else
        {
            // Happy relaxed state
            scaleMultiplier = 1f;
        }

        Vector3 targetScale = defaultEyeScale * scaleMultiplier;
        leftEyeTransform.localScale = Vector3.Lerp(leftEyeTransform.localScale, targetScale, Time.deltaTime * 10f);
        rightEyeTransform.localScale = Vector3.Lerp(rightEyeTransform.localScale, targetScale, Time.deltaTime * 10f);
    }

    private void HandleDoofusFell()
    {
        isFalling = true;

        // Squeeze eyes shut on fall
        if (leftEyeTransform != null) leftEyeTransform.localScale = new Vector3(defaultEyeScale.x * 1.5f, defaultEyeScale.y * 0.1f, defaultEyeScale.z);
        if (rightEyeTransform != null) rightEyeTransform.localScale = new Vector3(defaultEyeScale.x * 1.5f, defaultEyeScale.y * 0.1f, defaultEyeScale.z);
    }

    private void ApplyFallingTumble()
    {
        // Comic tumbling spin when plunging into void
        transform.Rotate(new Vector3(180f, 90f, 45f) * Time.deltaTime, Space.Self);
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
