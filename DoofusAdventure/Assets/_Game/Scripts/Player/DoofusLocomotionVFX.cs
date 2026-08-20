using UnityEngine;

/// <summary>
/// High-visibility cartoon locomotion dust puffs for Doofus:
/// - Positioned at Y = 0.55m (well above platform top at Y = 0.25m)
/// - Strictly upward + backward velocity (zero downward travel)
/// - Guaranteed 100% visibility above all ground tiles!
/// </summary>
public class DoofusLocomotionVFX : MonoBehaviour
{
    [Header("Locomotion Tuning")]
    [SerializeField] private float stepInterval = 0.12f;

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
        // Standard URP Unlit Material with white base color
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        Material puffMat = new Material(shader);
        if (puffMat.HasProperty("_BaseColor")) puffMat.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.95f));
        else puffMat.color = new Color(1f, 1f, 1f, 0.95f);

        // 1. Running Dust Puffs (Elevated, shooting up & back)
        GameObject runObj = new GameObject("RunningDust_Emitter");
        runObj.transform.SetParent(transform, false);
        runObj.transform.localPosition = new Vector3(0f, 0.50f, -0.30f); // 50cm above Doofus base!

        runningDustPS = runObj.AddComponent<ParticleSystem>();
        var runRenderer = runObj.GetComponent<ParticleSystemRenderer>();
        if (runRenderer != null)
        {
            runRenderer.material = puffMat;
            runRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            runRenderer.sortingOrder = 100;
        }

        var main = runningDustPS.main;
        main.playOnAwake = false;
        main.loop = false;
        main.maxParticles = 60;
        main.startLifetime = 0.35f;
        main.startSize = 0.45f;
        main.startSpeed = 1.2f;
        main.gravityModifier = -0.3f; // Gentle upward anti-gravity lift!
        main.startColor = new Color(1f, 1f, 1f, 0.90f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = runningDustPS.emission;
        emission.rateOverTime = 0;

        var shape = runningDustPS.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 15f;
        shape.radius = 0.1f;
        shape.rotation = new Vector3(-70f, 0f, 0f); // Angled UP and BACKWARD!

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
            new GradientAlphaKey[] { new GradientAlphaKey(0.90f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = grad;

        // 2. Skid Brake Dust (Elevated, wide upward fan)
        GameObject skidObj = new GameObject("SkidDust_Emitter");
        skidObj.transform.SetParent(transform, false);
        skidObj.transform.localPosition = new Vector3(0f, 0.50f, 0f);

        skidDustPS = skidObj.AddComponent<ParticleSystem>();
        var skidRenderer = skidObj.GetComponent<ParticleSystemRenderer>();
        if (skidRenderer != null)
        {
            skidRenderer.material = puffMat;
            skidRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            skidRenderer.sortingOrder = 100;
        }

        var skidMain = skidDustPS.main;
        skidMain.playOnAwake = false;
        skidMain.loop = false;
        skidMain.maxParticles = 60;
        skidMain.startLifetime = 0.40f;
        skidMain.startSize = 0.60f;
        skidMain.startSpeed = 2.0f;
        skidMain.gravityModifier = -0.4f;
        skidMain.startColor = new Color(1f, 1f, 1f, 0.95f);
        skidMain.simulationSpace = ParticleSystemSimulationSpace.World;

        var skidEmission = skidDustPS.emission;
        skidEmission.rateOverTime = 0;

        var skidShape = skidDustPS.shape;
        skidShape.enabled = true;
        skidShape.shapeType = ParticleSystemShapeType.Cone;
        skidShape.angle = 35f;
        skidShape.radius = 0.2f;
        skidShape.rotation = new Vector3(-90f, 0f, 0f); // Pointing STRAIGHT UP!

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
        if (velDrop > 2.5f && lastVelocity.sqrMagnitude > 1.2f)
        {
            if (skidDustPS != null) skidDustPS.Emit(10);
        }

        lastVelocity = currentVel;
    }
}
