using UnityEngine;

/// <summary>
/// Controls Doofus locomotion particles using Inspector-assigned Particle Systems.
/// Simply assign your Particle System from the Unity Editor!
/// </summary>
public class DoofusLocomotionVFX : MonoBehaviour
{
    [Header("Particle Systems (Assign in Inspector)")]
    [Tooltip("Particle system for running dust puffs")]
    [SerializeField] private ParticleSystem footstepParticles;

    [Tooltip("Particle system for skid brake dust")]
    [SerializeField] private ParticleSystem skidParticles;

    [Header("Emission Settings")]
    [SerializeField] private float stepInterval = 0.18f;
    [SerializeField] private int dustPerStep = 2;
    [SerializeField] private int dustPerSkid = 8;

    private DoofusController controller;
    private Rigidbody rb;
    private float stepTimer = 0f;
    private Vector3 lastVelocity = Vector3.zero;

    private void Awake()
    {
        controller = GetComponent<DoofusController>();
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (controller == null || rb == null) return;
        if (RewindManager.Instance != null && RewindManager.Instance.IsRewinding) return;

        Vector3 currentVel = rb.linearVelocity;
        bool isMoving = controller.IsMoving && currentVel.sqrMagnitude > 0.1f;

        if (isMoving)
        {
            stepTimer += Time.deltaTime;
            if (stepTimer >= stepInterval)
            {
                stepTimer = 0f;
                EmitFootstepDust();
            }
        }
        else
        {
            stepTimer = stepInterval;
        }

        // Skid brake detection
        float velDrop = (lastVelocity - currentVel).magnitude;
        if (velDrop > 2.8f && lastVelocity.sqrMagnitude > 1.2f)
        {
            EmitSkidDust();
        }

        lastVelocity = currentVel;
    }

    private void EmitFootstepDust()
    {
        if (footstepParticles != null)
        {
            footstepParticles.Emit(dustPerStep);
        }
    }

    private void EmitSkidDust()
    {
        if (skidParticles != null)
        {
            skidParticles.Emit(dustPerSkid);
        }
    }
}
