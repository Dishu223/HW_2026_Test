using UnityEngine;

/// <summary>
/// Spawns locomotion visual juice for Doofus:
/// - Running footstep dust puffs timed with stride
/// - Skid brake dust clouds when stopping or reversing quickly
/// </summary>
public class DoofusLocomotionVFX : MonoBehaviour
{
    [Header("Locomotion Tuning")]
    [SerializeField] private float stepInterval = 0.22f;

    private DoofusController controller;
    private Rigidbody rb;
    private ParticleSystem footstepDustPS;
    private ParticleSystem skidDustPS;
    private float stepTimer = 0f;
    private Vector3 lastVelocity = Vector3.zero;

    private void Awake()
    {
        controller = GetComponent<DoofusController>();
        rb = GetComponent<Rigidbody>();

        BuildFootstepParticleSystems();
    }

    private void BuildFootstepParticleSystems()
    {
        Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (particleShader == null) particleShader = Shader.Find("Particles/Standard Unlit");
        if (particleShader == null) particleShader = Shader.Find("Sprites/Default");
        Material mat = new Material(particleShader);

        // 1. Footstep dust puffs
        GameObject footObj = new GameObject("FootstepDust_PS");
        footObj.transform.SetParent(transform, false);
        footObj.transform.localPosition = new Vector3(0f, -0.45f, 0f);

        footstepDustPS = footObj.AddComponent<ParticleSystem>();
        var footRenderer = footObj.GetComponent<ParticleSystemRenderer>();
        if (footRenderer != null) footRenderer.material = mat;

        var main = footstepDustPS.main;
        main.playOnAwake = false;
        main.loop = false;
        main.maxParticles = 25;
        main.startLifetime = 0.25f;
        main.startSize = 0.22f;
        main.startSpeed = 0.6f;
        main.startColor = new Color(1f, 1f, 1f, 0.45f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = footstepDustPS.emission;
        emission.rateOverTime = 0;

        var sizeOverLifetime = footstepDustPS.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 1f);
        curve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

        // 2. Skid brake dust
        GameObject skidObj = new GameObject("SkidDust_PS");
        skidObj.transform.SetParent(transform, false);
        skidObj.transform.localPosition = new Vector3(0f, -0.45f, 0f);

        skidDustPS = skidObj.AddComponent<ParticleSystem>();
        var skidRenderer = skidObj.GetComponent<ParticleSystemRenderer>();
        if (skidRenderer != null) skidRenderer.material = mat;

        var skidMain = skidDustPS.main;
        skidMain.playOnAwake = false;
        skidMain.loop = false;
        skidMain.maxParticles = 30;
        skidMain.startLifetime = 0.35f;
        skidMain.startSize = 0.32f;
        skidMain.startSpeed = 1.2f;
        skidMain.startColor = new Color(1f, 1f, 1f, 0.6f);
        skidMain.simulationSpace = ParticleSystemSimulationSpace.World;

        var skidEmission = skidDustPS.emission;
        skidEmission.rateOverTime = 0;

        var skidSize = skidDustPS.sizeOverLifetime;
        skidSize.enabled = true;
        skidSize.size = new ParticleSystem.MinMaxCurve(1f, curve);
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
            stepTimer = stepInterval; // Instant puff on next movement start
        }

        // Detect abrupt stopping / braking
        float velDrop = (lastVelocity - currentVel).magnitude;
        if (velDrop > 3.0f && lastVelocity.sqrMagnitude > 1.5f)
        {
            EmitSkidDust();
        }

        lastVelocity = currentVel;
    }

    private void EmitFootstepDust()
    {
        if (footstepDustPS != null)
        {
            footstepDustPS.transform.position = transform.position + new Vector3(0f, -0.45f, 0f);
            footstepDustPS.Emit(3);
        }
    }

    private void EmitSkidDust()
    {
        if (skidDustPS != null)
        {
            skidDustPS.transform.position = transform.position + new Vector3(0f, -0.45f, 0f);
            skidDustPS.Emit(10);
        }
    }
}
