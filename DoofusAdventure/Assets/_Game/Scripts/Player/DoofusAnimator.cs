using System.Collections;
using UnityEngine;

/// <summary>
/// Procedural animation controller for Doofus (Snowman character).
/// Handles movement rotation/lean, springy head bobble, landing squash & stretch,
/// idle breathing bounce, and expressive eye reactions based on platform timer.
/// </summary>
public class DoofusAnimator : MonoBehaviour
{
    [Header("Body Hierarchy References")]
    [SerializeField] private Transform bodyTransform;
    [SerializeField] private Transform headTransform;
    [SerializeField] private Transform leftEyeTransform;
    [SerializeField] private Transform rightEyeTransform;

    [Header("Movement & Rotation")]
    [SerializeField] private float turnSpeed = 14f;
    [SerializeField] private float maxLeanAngle = 15f;
    [SerializeField] private float leanSmoothSpeed = 12f;
    [SerializeField] private float headLagSpeed = 14f;

    [Header("Idle Breathing")]
    [SerializeField] private float idleBounceSpeed = 3.5f;
    [SerializeField] private float idleBounceHeight = 0.05f;

    [Header("Squash & Stretch")]
    [SerializeField] private float squashAmount = 0.8f;
    [SerializeField] private float squashDuration = 0.25f;

    private Rigidbody rb;
    private Vector3 defaultBodyLocalPos;
    private Vector3 defaultHeadLocalPos;
    private Vector3 defaultEyeScale;
    private Vector3 headVelocity;
    private Coroutine squashRoutine;
    private bool isFalling = false;

    private void Awake()
    {
        rb = GetComponentInParent<Rigidbody>();

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

        ApplyMovementRotationAndLean();
        ApplyHeadBobble();
        ApplyIdleBounce();
    }

    /// <summary>
    /// Smoothly rotates Doofus to face travel direction and applies cartoon lean.
    /// </summary>
    private void ApplyMovementRotationAndLean()
    {
        if (rb == null) return;

        Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        float speed = horizontalVel.magnitude;

        // 1. Rotate whole character towards movement direction
        if (speed > 0.1f)
        {
            Quaternion targetLookRotation = Quaternion.LookRotation(horizontalVel.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetLookRotation, Time.deltaTime * turnSpeed);
        }

        // 2. Lean body slightly into / opposite movement
        if (bodyTransform != null)
        {
            Quaternion targetLean = Quaternion.identity;
            if (speed > 0.1f)
            {
                Vector3 tiltAxis = Vector3.Cross(Vector3.up, horizontalVel.normalized);
                targetLean = Quaternion.AngleAxis(-maxLeanAngle * Mathf.Clamp01(speed / 3f), tiltAxis);
            }
            bodyTransform.localRotation = Quaternion.Slerp(bodyTransform.localRotation, targetLean, Time.deltaTime * leanSmoothSpeed);
        }
    }

    /// <summary>
    /// Spring physics on head following the body motion.
    /// </summary>
    private void ApplyHeadBobble()
    {
        if (headTransform == null) return;
        headTransform.localPosition = Vector3.SmoothDamp(headTransform.localPosition, defaultHeadLocalPos, ref headVelocity, 1f / headLagSpeed);
    }

    /// <summary>
    /// Subtle breathing bounce preserving body position.
    /// </summary>
    private void ApplyIdleBounce()
    {
        if (bodyTransform == null) return;

        float bounce = Mathf.Sin(Time.time * idleBounceSpeed) * idleBounceHeight;
        bodyTransform.localPosition = defaultBodyLocalPos + new Vector3(0f, bounce, 0f);
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
            // Panicked state: eyes bulge huge and shake!
            scaleMultiplier = 1.7f + Mathf.Sin(Time.time * 25f) * 0.15f;
        }
        else if (normalizedTime < 0.5f)
        {
            // Worried state
            scaleMultiplier = 1.3f;
        }
        else
        {
            // Happy normal
            scaleMultiplier = 1f;
        }

        Vector3 targetScale = defaultEyeScale * scaleMultiplier;
        leftEyeTransform.localScale = Vector3.Lerp(leftEyeTransform.localScale, targetScale, Time.deltaTime * 10f);
        rightEyeTransform.localScale = Vector3.Lerp(rightEyeTransform.localScale, targetScale, Time.deltaTime * 10f);
    }

    private void HandleDoofusFell()
    {
        isFalling = true;

        if (leftEyeTransform != null) leftEyeTransform.localScale = new Vector3(defaultEyeScale.x * 1.5f, defaultEyeScale.y * 0.1f, defaultEyeScale.z);
        if (rightEyeTransform != null) rightEyeTransform.localScale = new Vector3(defaultEyeScale.x * 1.5f, defaultEyeScale.y * 0.1f, defaultEyeScale.z);
    }

    private void ApplyFallingTumble()
    {
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
