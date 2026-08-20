using UnityEngine;

/// <summary>
/// High-visibility cartoon locomotion dust puffs for Doofus:
/// - Uses explicit world-space EmitParams at Y = 0.38f (directly above the Y = 0.25f platform surface)
/// - Upward expanding billow puffs with zero raycast or clipping issues
/// </summary>
public class DoofusLocomotionVFX : MonoBehaviour
{
    [Header("Locomotion Tuning")]
    [SerializeField] private float stepInterval = 0.16f;

    private DoofusController controller;
    private Rigidbody rb;
    private ParticleSystem dustPS;
    private float stepTimer = 0f;
    private Vector3 lastVelocity = Vector3.zero;

    private void Awake()
    {
        controller = GetComponent<DoofusController>();
        rb = GetComponent<Rigidbody>();

        BuildUniversalDustParticleSystem();
    }

    private void BuildUniversalDustParticleSystem()
    {
        GameObject psObj = new GameObject("Doofus_UniversalDust_PS");
        dustPS = psObj.AddComponent<ParticleSystem>();

        // Find standard reliable unlit shader
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        Material mat = new Material(shader);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.90f));
        else mat.color = new Color(1f, 1f, 1f, 0.90f);

        ParticleSystemRenderer psr = psObj.GetComponent<ParticleSystemRenderer>();
        if (psr != null)
        {
            psr.material = mat;
            psr.renderMode = ParticleSystemRenderMode.Billboard;
            psr.sortingOrder = 50; // Highest sorting order
        }

        var main = dustPS.main;
        main.playOnAwake = false;
        main.loop = false;
        main.maxParticles = 80;
        main.startLifetime = 0.32f;
        main.startSize = 0.40f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = dustPS.emission;
        emission.rateOverTime = 0;

        var sizeOverLifetime = dustPS.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 0.4f);
        curve.AddKey(0.25f, 1.2f);
        curve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

        var colorOverLifetime = dustPS.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.9f, 0.95f, 1f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.90f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = grad;
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
                EmitRunningPuff();
            }
        }
        else
        {
            stepTimer = stepInterval;
        }

        // Skid brake puff
        float velDrop = (lastVelocity - currentVel).magnitude;
        if (velDrop > 2.5f && lastVelocity.sqrMagnitude > 1.2f)
        {
            EmitSkidPuff();
        }

        lastVelocity = currentVel;
    }

    private void EmitRunningPuff()
    {
        if (dustPS == null) return;

        // Platform top is at Y = 0.25 -> Emit at Y = 0.38f (clearly in air right behind feet!)
        Vector3 spawnCenter = new Vector3(transform.position.x, 0.38f, transform.position.z);
        Vector3 backwardDir = -transform.forward * 0.25f;

        for (int i = 0; i < 2; i++)
        {
            ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
            Vector3 offset = new Vector3(Random.Range(-0.15f, 0.15f), Random.Range(0f, 0.08f), Random.Range(-0.15f, 0.15f));
            emitParams.position = spawnCenter + backwardDir + offset;
            emitParams.startSize = Random.Range(0.35f, 0.50f);
            emitParams.startLifetime = Random.Range(0.28f, 0.38f);
            emitParams.velocity = new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(0.6f, 1.2f), Random.Range(-0.2f, 0.2f));
            dustPS.Emit(emitParams, 1);
        }
    }

    private void EmitSkidPuff()
    {
        if (dustPS == null) return;

        Vector3 spawnCenter = new Vector3(transform.position.x, 0.38f, transform.position.z);

        for (int i = 0; i < 6; i++)
        {
            ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
            Vector3 randomDir = Random.insideUnitSphere * 0.3f;
            randomDir.y = Mathf.Abs(randomDir.y) + 0.1f;

            emitParams.position = spawnCenter + new Vector3(randomDir.x, 0f, randomDir.z);
            emitParams.startSize = Random.Range(0.45f, 0.65f);
            emitParams.startLifetime = Random.Range(0.35f, 0.48f);
            emitParams.velocity = new Vector3(randomDir.x * 2.5f, Random.Range(1.0f, 1.8f), randomDir.z * 2.5f);
            dustPS.Emit(emitParams, 1);
        }
    }

    private void OnDestroy()
    {
        if (dustPS != null) Destroy(dustPS.gameObject);
    }
}
