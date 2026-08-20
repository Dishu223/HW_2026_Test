using System.Collections;
using UnityEngine;

/// <summary>
/// Exaggerated cartoon procedural animation controller for Doofus.
/// Creates hypercasual juicy movement: snappy direction turning, exaggerated body leaning,
/// trailing bobblehead lag, squash & stretch, and dynamic eye expressions.
/// </summary>
public class DoofusAnimator : MonoBehaviour
{
    [Header("Body Hierarchy References")]
    [SerializeField] private Transform bodyTransform;
    [SerializeField] private Transform headTransform;
    [SerializeField] private Transform leftEyeTransform;
    [SerializeField] private Transform rightEyeTransform;

    [Header("Hypercasual Movement Tuning")]
    [SerializeField] private float turnSpeed = 22f;          // Snappy fast turning
    [SerializeField] private float maxLeanAngle = 28f;       // Exaggerated cartoon lean
    [SerializeField] private float leanSmoothSpeed = 16f;    // Snappy lean responsiveness
    [SerializeField] private float headLagDistance = 0.28f;  // Head pulls visibly backward
    [SerializeField] private float headSpringTime = 0.08f;   // Spring elasticity

    [Header("Idle Breathing")]
    [SerializeField] private float idleBounceSpeed = 4.5f;
    [SerializeField] private float idleBounceHeight = 0.05f;

    [Header("Squash & Stretch")]
    [SerializeField] private float squashAmount = 0.72f;
    [SerializeField] private float squashDuration = 0.22f;

    private DoofusController controller;
    private Vector3 defaultBodyLocalPos;
    private Vector3 defaultHeadLocalPos;
    private Vector3 defaultEyeScale;
    private Vector3 headVelocity;
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

        ApplyMovementRotationAndLean();
        ApplyHeadBobbleAndLag();
        ApplyIdleBounce();
    }

    /// <summary>
    /// Snappily rotates Doofus to face travel direction and applies exaggerated cartoon lean.
    /// </summary>
    private void ApplyMovementRotationAndLean()
    {
        Vector3 moveInput = controller != null ? controller.MoveInput : Vector3.zero;
        bool isMoving = moveInput.sqrMagnitude > 0.01f;

        // 1. Snappy rotation towards travel direction
        if (isMoving)
        {
            Quaternion targetLookRotation = Quaternion.LookRotation(moveInput, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetLookRotation, Time.deltaTime * turnSpeed);
        }

        // 2. Exaggerated body lean (tilts backward from travel direction for cartoon inertia)
        if (bodyTransform != null)
        {
            Quaternion targetLean = Quaternion.identity;
            if (isMoving)
            {
                // Tilt backward relative to current forward direction
                targetLean = Quaternion.Euler(maxLeanAngle, 0f, 0f);
            }

            bodyTransform.localRotation = Quaternion.Slerp(bodyTransform.localRotation, targetLean, Time.deltaTime * leanSmoothSpeed);
        }
    }

    /// <summary>
    /// Head pulls backward in local space when moving and bobbles back when stopping.
    /// </summary>
    private void ApplyHeadBobbleAndLag()
    {
        if (headTransform == null) return;

        Vector3 moveInput = controller != null ? controller.MoveInput : Vector3.zero;
        bool isMoving = moveInput.sqrMagnitude > 0.01f;

        // When moving, pull head backwards on local Z axis
        Vector3 targetHeadPos = defaultHeadLocalPos;
        if (isMoving)
        {
            targetHeadPos = defaultHeadLocalPos + new Vector3(0f, -0.05f, -headLagDistance);
        }

        headTransform.localPosition = Vector3.SmoothDamp(headTransform.localPosition, targetHeadPos, ref headVelocity, headSpringTime);
    }

    /// <summary>
    /// Lively breathing bounce preserving body connection.
    /// </summary>
    private void ApplyIdleBounce()
    {
        if (bodyTransform == null) return;

        // Only bounce when stationary
        bool isMoving = controller != null && controller.IsMoving;
        float targetBounce = isMoving ? 0f : Mathf.Sin(Time.time * idleBounceSpeed) * idleBounceHeight;

        Vector3 targetPos = defaultBodyLocalPos + new Vector3(0f, targetBounce, 0f);
        bodyTransform.localPosition = Vector3.Lerp(bodyTransform.localPosition, targetPos, Time.deltaTime * 10f);
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
        Vector3 squashedScale = new Vector3(1.3f, squashAmount, 1.3f);
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
            // Panicked state: huge bulging eyes with vibration
            scaleMultiplier = 1.75f + Mathf.Sin(Time.time * 30f) * 0.2f;
        }
        else if (normalizedTime < 0.5f)
        {
            // Worried state
            scaleMultiplier = 1.35f;
        }
        else
        {
            // Happy relaxed state
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
