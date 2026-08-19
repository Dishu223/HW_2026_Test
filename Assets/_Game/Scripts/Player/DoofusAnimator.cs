using UnityEngine;

// Procedural animator for Doofus (Snowman character).
// Handles movement lean, head bobble, idle breathing, squash/stretch,
// and expressive eye reactions without requiring an Animator Controller.
public class DoofusAnimator : MonoBehaviour
{
    [Header("Snowman Part References")]
    [SerializeField] private Transform bodyTransform;
    [SerializeField] private Transform headTransform;
    [SerializeField] private Transform leftEyeTransform;
    [SerializeField] private Transform rightEyeTransform;

    [Header("Wobble & Lean Settings")]
    [SerializeField] private float maxLeanAngle = 14f;
    [SerializeField] private float leanSpeed = 10f;
    [SerializeField] private float headLagSpeed = 12f;

    [Header("Idle Breathing")]
    [SerializeField] private float idleBounceFrequency = 3.5f;
    [SerializeField] private float idleBounceHeight = 0.05f;

    [Header("Eye Scaling for Expressions")]
    [SerializeField] private Vector3 normalEyeScale = new Vector3(0.12f, 0.12f, 0.06f);
    [SerializeField] private Vector3 worriedEyeScale = new Vector3(0.14f, 0.18f, 0.06f);
    [SerializeField] private Vector3 panicEyeScale = new Vector3(0.20f, 0.20f, 0.08f);

    [Header("VFX References")]
    [SerializeField] private ParticleSystem walkDustParticles;

    private DoofusController controller;
    private Vector3 initialHeadLocalPos;
    private Vector3 headVelocity;
    private float currentNormalizedPlatformTime = 1f;
    private bool isFalling = false;

    private void Awake()
    {
        controller = GetComponentInParent<DoofusController>();
        if (headTransform != null)
        {
            initialHeadLocalPos = headTransform.localPosition;
        }
    }

    private void OnEnable()
    {
        GameEvents.OnPulpitTimerTick += HandlePlatformTimerTick;
        GameEvents.OnPulpitLanded += HandlePulpitLanded;
        GameEvents.OnDoofusFell += HandleDoofusFell;
        GameEvents.OnGameStart += HandleGameStart;
        GameEvents.OnGameRestart += HandleGameRestart;
    }

    private void OnDisable()
    {
        GameEvents.OnPulpitTimerTick -= HandlePlatformTimerTick;
        GameEvents.OnPulpitLanded -= HandlePulpitLanded;
        GameEvents.OnDoofusFell -= HandleDoofusFell;
        GameEvents.OnGameStart -= HandleGameStart;
        GameEvents.OnGameRestart -= HandleGameRestart;
    }

    private void Update()
    {
        if (isFalling)
        {
            ApplyFallingExpression();
            return;
        }

        Vector3 moveInput = controller != null ? controller.CurrentMoveInput : Vector3.zero;
        bool isMoving = moveInput.sqrMagnitude > 0.01f;

        // 1. Procedural Lean opposite to movement direction
        ApplyMovementLean(moveInput, isMoving);

        // 2. Head Spring / Bobble
        ApplyHeadBobble(moveInput);

        // 3. Idle Breathing
        ApplyIdleBreathing(isMoving);

        // 4. Update Eye Reactions to Platform Timer
        UpdateEyeExpressions();

        // 5. Walk particles emission
        UpdateWalkParticles(isMoving);
    }

    private void ApplyMovementLean(Vector3 moveInput, bool isMoving)
    {
        if (bodyTransform == null) return;

        Quaternion targetRotation = Quaternion.identity;

        if (isMoving)
        {
            // Lean slightly into or against movement for a juicy bouncy cartoon feel
            Vector3 leanAxis = Vector3.Cross(Vector3.up, moveInput).normalized;
            targetRotation = Quaternion.AngleAxis(-maxLeanAngle, leanAxis);
        }

        bodyTransform.localRotation = Quaternion.Slerp(bodyTransform.localRotation, targetRotation, Time.deltaTime * leanSpeed);
    }

    private void ApplyHeadBobble(Vector3 moveInput)
    {
        if (headTransform == null) return;

        // Target local position with a tiny lag in movement direction
        Vector3 targetLocalPos = initialHeadLocalPos - (moveInput * 0.08f);
        headTransform.localPosition = Vector3.SmoothDamp(headTransform.localPosition, targetLocalPos, ref headVelocity, 1f / headLagSpeed);
    }

    private void ApplyIdleBreathing(bool isMoving)
    {
        if (bodyTransform == null || isMoving) return;

        float bounceOffset = Mathf.Sin(Time.time * idleBounceFrequency) * idleBounceHeight;
        bodyTransform.localPosition = new Vector3(0f, bounceOffset, 0f);
    }

    private void UpdateEyeExpressions()
    {
        if (leftEyeTransform == null || rightEyeTransform == null) return;

        Vector3 targetScale;

        if (currentNormalizedPlatformTime > 0.5f)
        {
            // Happy / Calm
            targetScale = normalEyeScale;
        }
        else if (currentNormalizedPlatformTime > 0.25f)
        {
            // Worried
            targetScale = worriedEyeScale;
        }
        else
        {
            // Panic! Eyes bulge out and jitter
            targetScale = panicEyeScale;
            float jitter = Mathf.Sin(Time.time * 30f) * 0.015f;
            targetScale += new Vector3(jitter, jitter, 0f);
        }

        leftEyeTransform.localScale = Vector3.Lerp(leftEyeTransform.localScale, targetScale, Time.deltaTime * 8f);
        rightEyeTransform.localScale = Vector3.Lerp(rightEyeTransform.localScale, targetScale, Time.deltaTime * 8f);
    }

    private void ApplyFallingExpression()
    {
        if (leftEyeTransform == null || rightEyeTransform == null) return;

        // Eyes squeeze shut while falling
        Vector3 closedScale = new Vector3(normalEyeScale.x * 1.5f, 0.02f, normalEyeScale.z);
        leftEyeTransform.localScale = Vector3.Lerp(leftEyeTransform.localScale, closedScale, Time.deltaTime * 15f);
        rightEyeTransform.localScale = Vector3.Lerp(rightEyeTransform.localScale, closedScale, Time.deltaTime * 15f);
    }

    private void UpdateWalkParticles(bool isMoving)
    {
        if (walkDustParticles == null) return;

        if (isMoving && !walkDustParticles.isPlaying)
        {
            walkDustParticles.Play();
        }
        else if (!isMoving && walkDustParticles.isPlaying)
        {
            walkDustParticles.Stop();
        }
    }

    private void HandlePlatformTimerTick(float normalizedTime)
    {
        currentNormalizedPlatformTime = normalizedTime;
    }

    private void HandlePulpitLanded()
    {
        currentNormalizedPlatformTime = 1f;
        // Little squash effect on landing
        if (bodyTransform != null)
        {
            bodyTransform.localScale = new Vector3(1.15f, 0.85f, 1.15f);
        }
    }

    private void HandleDoofusFell()
    {
        isFalling = true;
    }

    private void HandleGameStart()
    {
        isFalling = false;
        currentNormalizedPlatformTime = 1f;
        if (bodyTransform != null) bodyTransform.localScale = Vector3.one;
    }

    private void HandleGameRestart()
    {
        isFalling = false;
        currentNormalizedPlatformTime = 1f;
        if (bodyTransform != null) bodyTransform.localScale = Vector3.one;
    }
}
