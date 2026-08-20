using UnityEngine;

/// <summary>
/// Spawns high-visibility cartoon dust puffs strictly on top of platforms:
/// - Filters out Doofus colliders from raycasts so it always detects the true platform surface
/// - Elevates particles 15cm above tile with upward drift and high render queue
/// - Generates skid brake dust clouds on abrupt stops
/// </summary>
public class DoofusLocomotionVFX : MonoBehaviour
{
    [Header("Locomotion Tuning")]
    [SerializeField] private float stepInterval = 0.18f;

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
        mat.renderQueue = 3100; // Render on top of opaque platform geometry!

        // 1. High-Visibility Footstep Dust Puffs
        GameObject footObj = new GameObject("Doofus_FootstepDust_PS");
        footstepDustPS = footObj.AddComponent<ParticleSystem>();
        var footRenderer = footObj.GetComponent<ParticleSystemRenderer>();
        if (footRenderer != null)
        {
            footRenderer.material = mat;
            footRenderer.sortingOrder = 10;
        }

        var main = footstepDustPS.main;
        main.playOnAwake = false;
        main.loop = false;
        main.maxParticles = 50;
        main.startLifetime = 0.35f;
        main.startSize = 0.45f; // Bigger, visible cartoon puffs!
        main.startSpeed = 1.2f;
        main.startColor = new Color(1f, 1f, 1f, 0.85f); // High visibility crisp white
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = footstepDustPS.emission;
        emission.rateOverTime = 0;

        var shape = footstepDustPS.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;

        var sizeOverLifetime = footstepDustPS.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 0.5f);
        curve.AddKey(0.3f, 1.2f); // Pop expand
        curve.AddKey(1f, 0f);     // Dissipate
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

        var velocityOverLifetime = footstepDustPS.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(0.8f); // Upward float above floor

        // 2. High-Visibility Skid Brake Dust
        GameObject skidObj = new GameObject("Doofus_SkidDust_PS");
        skidDustPS = skidObj.AddComponent<ParticleSystem>();
        var skidRenderer = skidObj.GetComponent<ParticleSystemRenderer>();
        if (skidRenderer != null)
        {
            skidRenderer.material = mat;
            skidRenderer.sortingOrder = 10;
        }

        var skidMain = skidDustPS.main;
        skidMain.playOnAwake = false;
        skidMain.loop = false;
        skidMain.maxParticles = 50;
        skidMain.startLifetime = 0.40f;
        skidMain.startSize = 0.55f;
        skidMain.startSpeed = 2.0f;
        skidMain.startColor = new Color(1f, 1f, 1f, 0.90f);
        skidMain.simulationSpace = ParticleSystemSimulationSpace.World;

        var skidEmission = skidDustPS.emission;
        skidEmission.rateOverTime = 0;

        var skidSize = skidDustPS.sizeOverLifetime;
        skidSize.enabled = true;
        skidSize.size = new ParticleSystem.MinMaxCurve(1f, curve);

        var skidVel = skidDustPS.velocityOverLifetime;
        skidVel.enabled = true;
        skidVel.y = new ParticleSystem.MinMaxCurve(1.2f);
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

        // Detect abrupt brake
        float velDrop = (lastVelocity - currentVel).magnitude;
        if (velDrop > 3.0f && lastVelocity.sqrMagnitude > 1.5f)
        {
            EmitSkidDust();
        }

        lastVelocity = currentVel;
    }

    /// <summary>
    /// Performs non-self raycasting to accurately find the platform top surface.
    /// </summary>
    private Vector3 GetPlatformSurfacePoint()
    {
        Ray ray = new Ray(transform.position + Vector3.up * 0.5f, Vector3.down);
        RaycastHit[] hits = Physics.RaycastAll(ray, 3.0f);

        foreach (RaycastHit hit in hits)
        {
            // Ignore Doofus himself and child colliders
            if (hit.collider.gameObject != gameObject && !hit.collider.transform.IsChildOf(transform))
            {
                return hit.point + Vector3.up * 0.18f; // Elevated 18cm above tile face!
            }
        }

        // Fallback: top of platform is standard Y = 0.25 -> spawn at Y = 0.40
        return new Vector3(transform.position.x, 0.40f, transform.position.z);
    }

    private void EmitFootstepDust()
    {
        if (footstepDustPS != null)
        {
            footstepDustPS.transform.position = GetPlatformSurfacePoint();
            footstepDustPS.Emit(4);
        }
    }

    private void EmitSkidDust()
    {
        if (skidDustPS != null)
        {
            skidDustPS.transform.position = GetPlatformSurfacePoint();
            skidDustPS.Emit(14);
        }
    }

    private void OnDestroy()
    {
        if (footstepDustPS != null) Destroy(footstepDustPS.gameObject);
        if (skidDustPS != null) Destroy(skidDustPS.gameObject);
    }
}
