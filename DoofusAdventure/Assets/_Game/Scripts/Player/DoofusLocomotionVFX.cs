using UnityEngine;

/// <summary>
/// Cartoon dust puffs for Doofus:
/// - Attached as child at Doofus's base with upward Hemisphere emission
/// - Shoots particles strictly UPWARDS and OUTWARDS so they float above platforms
/// - Guaranteed URP particle rendering
/// </summary>
public class DoofusLocomotionVFX : MonoBehaviour
{
    [Header("Locomotion Tuning")]
    [SerializeField] private float stepInterval = 0.15f;

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
        // Try getting Unity's default particle material or create a clean URP Particle Unlit material
        Material particleMat = null;
        Shader pShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (pShader == null) pShader = Shader.Find("Particles/Standard Unlit");
        if (pShader == null) pShader = Shader.Find("Mobile/Particles/Additive");
        if (pShader == null) pShader = Shader.Find("Sprites/Default");

        if (pShader != null)
        {
            particleMat = new Material(pShader);
        }

        // 1. Running Footstep Dust (Hemisphere shooting upward/backward)
        GameObject runObj = new GameObject("RunningDust_Emitter");
        runObj.transform.SetParent(transform, false);
        runObj.transform.localPosition = new Vector3(0f, 0.20f, -0.35f); // 20cm above base, behind player
        runObj.transform.localRotation = Quaternion.Euler(-60f, 0f, 0f); // Tilted up & back

        runningDustPS = runObj.AddComponent<ParticleSystem>();
        var runRenderer = runObj.GetComponent<ParticleSystemRenderer>();
        if (runRenderer != null && particleMat != null)
        {
            runRenderer.material = particleMat;
            runRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            runRenderer.sortingOrder = 20;
        }

        var main = runningDustPS.main;
        main.playOnAwake = false;
        main.loop = false;
        main.maxParticles = 60;
        main.startLifetime = 0.35f;
        main.startSize = 0.45f;
        main.startSpeed = 1.6f;
        main.startColor = new Color(1f, 1f, 1f, 0.85f);
        main.simulationSpace = ParticleSystemSimulationSpace.World; // World space trails

        var emission = runningDustPS.emission;
        emission.rateOverTime = 0;

        var shape = runningDustPS.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 20f;
        shape.radius = 0.15f;

        var sizeOverLifetime = runningDustPS.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.5f);
        sizeCurve.AddKey(0.3f, 1.3f);
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

        // 2. Skid Brake Dust (Wide upward fan)
        GameObject skidObj = new GameObject("SkidDust_Emitter");
        skidObj.transform.SetParent(transform, false);
        skidObj.transform.localPosition = new Vector3(0f, 0.20f, 0f);
        skidObj.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f); // Pointing straight up!

        skidDustPS = skidObj.AddComponent<ParticleSystem>();
        var skidRenderer = skidObj.GetComponent<ParticleSystemRenderer>();
        if (skidRenderer != null && particleMat != null)
        {
            skidRenderer.material = particleMat;
            skidRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            skidRenderer.sortingOrder = 20;
        }

        var skidMain = skidDustPS.main;
        skidMain.playOnAwake = false;
        skidMain.loop = false;
        skidMain.maxParticles = 60;
        skidMain.startLifetime = 0.45f;
        skidMain.startSize = 0.60f;
        skidMain.startSpeed = 2.5f;
        skidMain.startColor = new Color(1f, 1f, 1f, 0.90f);
        skidMain.simulationSpace = ParticleSystemSimulationSpace.World;

        var skidEmission = skidDustPS.emission;
        skidEmission.rateOverTime = 0;

        var skidShape = skidDustPS.shape;
        skidShape.enabled = true;
        skidShape.shapeType = ParticleSystemShapeType.Cone;
        skidShape.angle = 45f;
        skidShape.radius = 0.3f;

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
        if (velDrop > 2.8f && lastVelocity.sqrMagnitude > 1.5f)
        {
            if (skidDustPS != null) skidDustPS.Emit(12);
        }

        lastVelocity = currentVel;
    }
}
