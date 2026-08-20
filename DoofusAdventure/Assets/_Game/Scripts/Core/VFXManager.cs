using UnityEngine;

/// <summary>
/// Fully Configurable Particle VFX & Game Juice Manager:
/// - Platform Spawn Edge Dust: Bursts from underneath the perimeter edges of the tile!
/// - Inspector-exposed tuning parameters (colors, sizes, speeds, particle counts)
/// - Optional Custom Prefab / ParticleSystem slots for complete DIY customization in Inspector
/// </summary>
public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    [Header("--- Custom VFX Prefabs (Optional Override) ---")]
    [Tooltip("Drop your own custom ParticleSystem prefab here if you want to override the procedural spawn puff")]
    [SerializeField] private ParticleSystem customSpawnPuffPrefab;
    [SerializeField] private ParticleSystem customLandingRingPrefab;
    [SerializeField] private ParticleSystem customCrumbleDustPrefab;
    [SerializeField] private ParticleSystem customChronoSparklePrefab;
    [SerializeField] private ParticleSystem customMilestoneConfettiPrefab;

    [Header("--- Platform Spawn Edge Particles Tuning ---")]
    [SerializeField] private Color spawnEdgeColor = new Color(0.92f, 0.95f, 1f, 0.85f);
    [SerializeField] private float spawnEdgeParticleSize = 0.38f;
    [SerializeField] private float spawnEdgeParticleLifetime = 0.45f;
    [SerializeField] private float spawnEdgeParticleSpeed = 1.6f;
    [SerializeField] private int spawnEdgeParticleCount = 48;
    [SerializeField] private float platformDimension = 5f; // Platform X/Z size

    [Header("--- Landing Shockwave Tuning ---")]
    [SerializeField] private Color landingColor = new Color(1f, 1f, 1f, 0.75f);
    [SerializeField] private float landingParticleSize = 0.35f;
    [SerializeField] private float landingParticleLifetime = 0.30f;
    [SerializeField] private float landingParticleSpeed = 2.0f;
    [SerializeField] private int landingParticleCount = 16;

    [Header("--- Platform Crumble Dust Tuning ---")]
    [SerializeField] private Color crumbleColor = new Color(0.85f, 0.45f, 0.35f, 0.75f);
    [SerializeField] private float crumbleParticleSize = 0.50f;
    [SerializeField] private float crumbleParticleLifetime = 0.55f;
    [SerializeField] private float crumbleParticleSpeed = 3.0f;
    [SerializeField] private int crumbleParticleCount = 32;

    [Header("--- Rewind Chrono Sparkles Tuning ---")]
    [SerializeField] private Color chronoColor = new Color(0f, 0.90f, 1f, 0.95f);
    [SerializeField] private float chronoParticleSize = 0.25f;
    [SerializeField] private float chronoParticleLifetime = 0.50f;
    [SerializeField] private int chronoParticleCount = 18;

    [Header("--- Milestone Confetti Tuning ---")]
    [SerializeField] private Color confettiColor = new Color(1f, 0.85f, 0.15f, 1f);
    [SerializeField] private float confettiParticleSize = 0.35f;
    [SerializeField] private float confettiParticleLifetime = 0.90f;
    [SerializeField] private int confettiParticleCount = 60;

    private ParticleSystem landingRingPS;
    private ParticleSystem spawnEdgeDustPS;
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
        Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (particleShader == null) particleShader = Shader.Find("Particles/Standard Unlit");
        if (particleShader == null) particleShader = Shader.Find("Mobile/Particles/Additive");
        if (particleShader == null) particleShader = Shader.Find("Sprites/Default");

        defaultParticleMaterial = new Material(particleShader);
    }

    private void BuildProceduralParticleSystems()
    {
        // 1. Edge perimeter dust for platform placement (Edge Box Perimeter)
        if (customSpawnPuffPrefab != null)
            spawnEdgeDustPS = Instantiate(customSpawnPuffPrefab, transform);
        else
            spawnEdgeDustPS = CreateEdgeBoxParticleSystem("PlatformSpawn_EdgeDust_PS", spawnEdgeParticleCount, spawnEdgeColor, spawnEdgeParticleLifetime, spawnEdgeParticleSize, spawnEdgeParticleSpeed, platformDimension);

        // 2. Landing Shockwave
        if (customLandingRingPrefab != null)
            landingRingPS = Instantiate(customLandingRingPrefab, transform);
        else
            landingRingPS = CreateCircleParticleSystem("LandingShockwave_PS", landingParticleCount, landingColor, landingParticleLifetime, landingParticleSize, landingParticleSpeed);

        // 3. Crumble Dust
        if (customCrumbleDustPrefab != null)
            crumbleDustPS = Instantiate(customCrumbleDustPrefab, transform);
        else
            crumbleDustPS = CreateCircleParticleSystem("CrumbleDust_PS", crumbleParticleCount, crumbleColor, crumbleParticleLifetime, crumbleParticleSize, crumbleParticleSpeed);

        // 4. Chrono Sparkle
        if (customChronoSparklePrefab != null)
            chronoSparklePS = Instantiate(customChronoSparklePrefab, transform);
        else
            chronoSparklePS = CreateCircleParticleSystem("ChronoSparkle_PS", chronoParticleCount, chronoColor, chronoParticleLifetime, chronoParticleSize, 1.8f);

        // 5. Milestone Confetti
        if (customMilestoneConfettiPrefab != null)
            milestoneConfettiPS = Instantiate(customMilestoneConfettiPrefab, transform);
        else
            milestoneConfettiPS = CreateCircleParticleSystem("MilestoneConfetti_PS", confettiParticleCount, confettiColor, confettiParticleLifetime, confettiParticleSize, 5.0f);
    }

    private ParticleSystem CreateEdgeBoxParticleSystem(string name, int maxParticles, Color color, float lifetime, float size, float speed, float sizeBox)
    {
        GameObject psObj = new GameObject(name);
        psObj.transform.SetParent(transform);

        ParticleSystem ps = psObj.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = psObj.GetComponent<ParticleSystemRenderer>();
        if (psr != null && defaultParticleMaterial != null)
        {
            psr.material = defaultParticleMaterial;
            psr.renderMode = ParticleSystemRenderMode.Billboard;
            psr.sortingOrder = 20;
        }

        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.maxParticles = maxParticles * 2;
        main.startLifetime = lifetime;
        main.startSize = size;
        main.startSpeed = speed;
        main.startColor = color;
        main.gravityModifier = -0.2f; // Soft upward lift
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0;

        // Shape: Box Edge perimeter (X/Z edge perimeter around platform dimensions)
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.BoxEdge;
        shape.scale = new Vector3(sizeBox * 0.96f, 0.1f, sizeBox * 0.96f);

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.4f);
        sizeCurve.AddKey(0.25f, 1.2f);
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

    private ParticleSystem CreateCircleParticleSystem(string name, int maxParticles, Color color, float lifetime, float size, float speed)
    {
        GameObject psObj = new GameObject(name);
        psObj.transform.SetParent(transform);

        ParticleSystem ps = psObj.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = psObj.GetComponent<ParticleSystemRenderer>();
        if (psr != null && defaultParticleMaterial != null)
        {
            psr.material = defaultParticleMaterial;
            psr.renderMode = ParticleSystemRenderMode.Billboard;
            psr.sortingOrder = 20;
        }

        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.maxParticles = maxParticles * 2;
        main.startLifetime = lifetime;
        main.startSize = size;
        main.startSpeed = speed;
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.5f;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.5f);
        sizeCurve.AddKey(0.3f, 1.2f);
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
    public void SpawnPlatformEdgeDust(Vector3 platformCenter)
    {
        if (spawnEdgeDustPS != null)
        {
            // Position at base of platform (Y = 0) so dust shoots up and outward from under the perimeter!
            spawnEdgeDustPS.transform.position = new Vector3(platformCenter.x, 0.05f, platformCenter.z);
            spawnEdgeDustPS.Emit(spawnEdgeParticleCount);
        }
    }

    public void SpawnLandingDust(Vector3 position)
    {
        if (landingRingPS != null)
        {
            landingRingPS.transform.position = new Vector3(position.x, 0.28f, position.z);
            landingRingPS.Emit(landingParticleCount);
        }
    }

    public void SpawnPlatformCrumbleDust(Vector3 position)
    {
        if (crumbleDustPS != null)
        {
            crumbleDustPS.transform.position = new Vector3(position.x, 0.1f, position.z);
            crumbleDustPS.Emit(crumbleParticleCount);
        }
    }

    public void SpawnChronoSparkle(Vector3 position)
    {
        if (chronoSparklePS != null)
        {
            chronoSparklePS.transform.position = position;
            chronoSparklePS.Emit(chronoParticleCount);
        }
    }

    public void SpawnMilestoneConfetti(Vector3 position)
    {
        if (milestoneConfettiPS != null)
        {
            milestoneConfettiPS.transform.position = position + new Vector3(0f, 2f, 0f);
            milestoneConfettiPS.Emit(confettiParticleCount);
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
        SpawnPlatformEdgeDust(pos);
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
