using UnityEngine;

/// <summary>
/// High-performance procedural Particle VFX & Game Juice Manager:
/// - Landing shockwaves & dust rings
/// - Platform spawn magic rings
/// - Platform crumble rock-dust puffs
/// - Chrono time-rewind sparkle trails & touchdown sand bursts
/// - Score milestone confetti bursts
/// </summary>
public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    private ParticleSystem landingRingPS;
    private ParticleSystem spawnPuffPS;
    private ParticleSystem crumbleDustPS;
    private ParticleSystem chronoSparklePS;
    private ParticleSystem milestoneConfettiPS;

    private Material defaultParticleMaterial;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        CreateDefaultParticleMaterial();
        BuildProceduralParticleSystems();
    }

    private void OnEnable()
    {
        GameEvents.OnPulpitLanded += HandlePulpitLanded;
        GameEvents.OnPulpitSpawned += HandlePulpitSpawned;
        GameEvents.OnPulpitDestroyed += HandlePulpitDestroyed;
        GameEvents.OnRewindComplete += HandleRewindComplete;
        GameEvents.OnMilestoneReached += HandleMilestoneReached;
    }

    private void OnDisable()
    {
        GameEvents.OnPulpitLanded -= HandlePulpitLanded;
        GameEvents.OnPulpitSpawned -= HandlePulpitSpawned;
        GameEvents.OnPulpitDestroyed -= HandlePulpitDestroyed;
        GameEvents.OnRewindComplete -= HandleRewindComplete;
        GameEvents.OnMilestoneReached -= HandleMilestoneReached;
    }

    private void CreateDefaultParticleMaterial()
    {
        // Find URP or standard mobile particle shader
        Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (particleShader == null) particleShader = Shader.Find("Particles/Standard Unlit");
        if (particleShader == null) particleShader = Shader.Find("Mobile/Particles/Additive");
        if (particleShader == null) particleShader = Shader.Find("Sprites/Default");

        defaultParticleMaterial = new Material(particleShader);
    }

    private void BuildProceduralParticleSystems()
    {
        landingRingPS = CreateParticleSystem("LandingShockwave_PS", 30, new Color(0.9f, 1f, 0.9f, 0.8f), 0.35f, 0.4f, 2.5f);
        spawnPuffPS = CreateParticleSystem("SpawnPuff_PS", 40, new Color(0.3f, 0.9f, 1f, 0.7f), 0.45f, 0.3f, 2.0f);
        crumbleDustPS = CreateParticleSystem("CrumbleDust_PS", 50, new Color(0.85f, 0.4f, 0.3f, 0.75f), 0.6f, 0.5f, 3.5f);
        chronoSparklePS = CreateParticleSystem("ChronoSparkle_PS", 80, new Color(0f, 0.9f, 1f, 0.9f), 0.5f, 0.25f, 1.8f);
        milestoneConfettiPS = CreateParticleSystem("MilestoneConfetti_PS", 100, Color.yellow, 0.9f, 0.35f, 5.0f);
    }

    private ParticleSystem CreateParticleSystem(string name, int maxParticles, Color color, float lifetime, float size, float speed)
    {
        GameObject psObj = new GameObject(name);
        psObj.transform.SetParent(transform);

        ParticleSystem ps = psObj.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = psObj.GetComponent<ParticleSystemRenderer>();
        if (psr != null && defaultParticleMaterial != null)
        {
            psr.material = defaultParticleMaterial;
        }

        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.maxParticles = maxParticles;
        main.startLifetime = lifetime;
        main.startSize = size;
        main.startSpeed = speed;
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.5f;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(color.a, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = grad;

        return ps;
    }

    #region Trigger Methods
    public void SpawnLandingDust(Vector3 position)
    {
        if (landingRingPS != null)
        {
            landingRingPS.transform.position = position + new Vector3(0f, 0.05f, 0f);
            landingRingPS.Emit(18);
        }
    }

    public void SpawnPlatformSpawnPuff(Vector3 position)
    {
        if (spawnPuffPS != null)
        {
            spawnPuffPS.transform.position = position + new Vector3(0f, 0.1f, 0f);
            spawnPuffPS.Emit(22);
        }
    }

    public void SpawnPlatformCrumbleDust(Vector3 position)
    {
        if (crumbleDustPS != null)
        {
            crumbleDustPS.transform.position = position + new Vector3(0f, 0.1f, 0f);
            crumbleDustPS.Emit(28);
        }
    }

    public void SpawnChronoSparkle(Vector3 position)
    {
        if (chronoSparklePS != null)
        {
            chronoSparklePS.transform.position = position;
            chronoSparklePS.Emit(12);
        }
    }

    public void SpawnMilestoneConfetti(Vector3 position)
    {
        if (milestoneConfettiPS != null)
        {
            milestoneConfettiPS.transform.position = position + new Vector3(0f, 2f, 0f);
            milestoneConfettiPS.Emit(60);
        }
    }
    #endregion

    #region Event Handlers
    private void HandlePulpitLanded()
    {
        DoofusController doofus = FindAnyObjectByType<DoofusController>();
        Vector3 pos = doofus != null ? doofus.transform.position : Vector3.zero;
        SpawnLandingDust(pos);
    }

    private void HandlePulpitSpawned(Vector3 pos)
    {
        SpawnPlatformSpawnPuff(pos);
    }

    private void HandlePulpitDestroyed(Vector3 pos)
    {
        SpawnPlatformCrumbleDust(pos);
    }

    private void HandleRewindComplete()
    {
        DoofusController doofus = FindAnyObjectByType<DoofusController>();
        Vector3 pos = doofus != null ? doofus.transform.position : Vector3.zero;
        SpawnChronoSparkle(pos);
    }

    private void HandleMilestoneReached(int milestone)
    {
        DoofusController doofus = FindAnyObjectByType<DoofusController>();
        Vector3 pos = doofus != null ? doofus.transform.position : Vector3.zero;
        SpawnMilestoneConfetti(pos);
    }
    #endregion
}
