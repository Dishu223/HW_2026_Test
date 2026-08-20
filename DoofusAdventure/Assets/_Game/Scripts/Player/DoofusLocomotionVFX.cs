using UnityEngine;

/// <summary>
/// Spawns locomotion visual juice for Doofus:
/// - Raycasts to the top surface of platforms so footstep dust NEVER clips below tiles!
/// - Skid brake dust clouds when stopping or reversing quickly
/// </summary>
public class DoofusLocomotionVFX : MonoBehaviour
{
    [Header("Locomotion Tuning")]
    [SerializeField] private float stepInterval = 0.20f;

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

        footstepDustPS = footObj.AddComponent<ParticleSystem>();
        var footRenderer = footObj.GetComponent<ParticleSystemRenderer>();
        if (footRenderer != null) footRenderer.material = mat;

        var main = footstepDustPS.main;
        main.playOnAwake = false;
        main.loop = false;
        main.maxParticles = 30;
        main.startLifetime = 0.28f;
        main.startSize = 0.24f;
        main.startSpeed = 0.8f;
        main.startColor = new Color(1f, 1f, 1f, 0.55f);
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

        skidDustPS = skidObj.AddComponent<ParticleSystem>();
        var skidRenderer = skidObj.GetComponent<ParticleSystemRenderer>();
        if (skidRenderer != null) skidRenderer.material = mat;

        var skidMain = skidDustPS.main;
        skidMain.playOnAwake = false;
        skidMain.loop = false;
        skidMain.maxParticles = 35;
        skidMain.startLifetime = 0.35f;
        skidMain.startSize = 0.35f;
        skidMain.startSpeed = 1.4f;
        skidMain.startColor = new Color(1f, 1f, 1f, 0.65f);
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
            stepTimer = stepInterval;
        }

        // Detect abrupt stopping / braking
        float velDrop = (lastVelocity - currentVel).magnitude;
        if (velDrop > 3.0f && lastVelocity.sqrMagnitude > 1.5f)
        {
            EmitSkidDust();
        }

        lastVelocity = currentVel;
    }

    private Vector3 GetSurfaceContactPoint()
    {
        // Raycast down from above the character to find exact surface height
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 2.5f))
        {
            return hit.point + Vector3.up * 0.05f; // Raised 5cm above surface to guarantee zero clipping
        }
        return transform.position + new Vector3(0f, 0.05f, 0f);
    }

    private void EmitFootstepDust()
    {
        if (footstepDustPS != null)
        {
            footstepDustPS.transform.position = GetSurfaceContactPoint();
            footstepDustPS.Emit(4);
        }
    }

    private void EmitSkidDust()
    {
        if (skidDustPS != null)
        {
            skidDustPS.transform.position = GetSurfaceContactPoint();
            skidDustPS.Emit(12);
        }
    }
}
