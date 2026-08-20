using UnityEngine;

/// <summary>
/// Cartoon dust puffs for Doofus:
/// - Uses Custom/AlwaysOnTopParticle shader with ZTest Always (100% immune to tile clipping!)
/// - Spawns crisp cartoon smoke puffs right behind Doofus as he runs
/// </summary>
public class DoofusLocomotionVFX : MonoBehaviour
{
    [Header("Locomotion Tuning")]
    [SerializeField] private float stepInterval = 0.14f;

    private DoofusController controller;
    private Rigidbody rb;
    private ParticleSystem runningDustPS;
    private ParticleSystem skidDustPS;
    private float stepTimer = 0f;
    private Vector3 lastVelocity = Vector3.zero;

    private void Awake()
    {
        controller = GetComponent<DoofusController>();
        rb = GetComponent<Rigidbody>();

        BuildParticleSystems();
    }

    private void BuildParticleSystems()
    {
        Shader topShader = Shader.Find("Custom/AlwaysOnTopParticle");
        if (topShader == null) topShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (topShader == null) topShader = Shader.Find("Sprites/Default");

        Material topMat = new Material(topShader);

        // 1. Running Footstep Dust
        GameObject runObj = new GameObject("RunningDust_Emitter");
        runObj.transform.SetParent(transform, false);
        runObj.transform.localPosition = new Vector3(0f, 0.35f, -0.30f);

        runningDustPS = runObj.AddComponent<ParticleSystem>();
        var runRenderer = runObj.GetComponent<ParticleSystemRenderer>();
        if (runRenderer != null)
        {
            runRenderer.material = topMat;
            runRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            runRenderer.sortingOrder = 100; // Ultra priority
        }

        var main = runningDustPS.main;
        main.playOnAwake = false;
        main.loop = false;
        main.maxParticles = 60;
        main.startLifetime = 0.30f;
        main.startSize = 0.40f;
        main.startSpeed = 1.0f;
        main.startColor = new Color(1f, 1f, 1f, 0.85f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = runningDustPS.emission;
        emission.rateOverTime = 0;

        var shape = runningDustPS.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;

        var sizeOverLifetime = runningDustPS.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.5f);
        sizeCurve.AddKey(0.25f, 1.3f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var colorOverLifetime = runningDustPS.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.9f, 0.95f, 1f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.85f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = grad;

        // 2. Skid Brake Dust
        GameObject skidObj = new GameObject("SkidDust_Emitter");
        skidObj.transform.SetParent(transform, false);
        skidObj.transform.localPosition = new Vector3(0f, 0.35f, 0f);

        skidDustPS = skidObj.AddComponent<ParticleSystem>();
        var skidRenderer = skidObj.GetComponent<ParticleSystemRenderer>();
        if (skidRenderer != null)
        {
            skidRenderer.material = topMat;
            skidRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            skidRenderer.sortingOrder = 100;
        }

        var skidMain = skidDustPS.main;
        skidMain.playOnAwake = false;
        skidMain.loop = false;
        skidMain.maxParticles = 60;
        skidMain.startLifetime = 0.40f;
        skidMain.startSize = 0.55f;
        skidMain.startSpeed = 1.8f;
        skidMain.startColor = new Color(1f, 1f, 1f, 0.90f);
        skidMain.simulationSpace = ParticleSystemSimulationSpace.World;

        var skidEmission = skidDustPS.emission;
        skidEmission.rateOverTime = 0;

        var skidSize = skidDustPS.sizeOverLifetime;
        skidSize.enabled = true;
        skidSize.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var skidColor = skidDustPS.colorOverLifetime;
        skidColor.enabled = true;
        skidColor.color = grad;
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
                if (runningDustPS != null) runningDustPS.Emit(3);
            }
        }
        else
        {
            stepTimer = stepInterval;
        }

        // Skid brake detection
        float velDrop = (lastVelocity - currentVel).magnitude;
        if (velDrop > 2.6f && lastVelocity.sqrMagnitude > 1.2f)
        {
            if (skidDustPS != null) skidDustPS.Emit(10);
        }

        lastVelocity = currentVel;
    }
}
