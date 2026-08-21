using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fully Configurable Particle VFX & Game Juice Manager:
/// - Dedicated VICTORY Confetti Prefab slot (triggers EXCLUSIVELY upon winning/reaching the goal)
/// - StopConfettiEmittingNaturally(): Stops emitting new confetti but lets active particles flutter down and finish their natural loop on Space/resume!
/// - ForceClearAllConfetti(): Instant reset on hard game restart (R)
/// - Platform Spawn Edge Dust: Shoots strictly OUTWARD horizontally from downside perimeter edges
/// </summary>
public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    [Header("--- Custom VFX Prefabs (Drag your Prefabs here!) ---")]
    [Tooltip("Drag your confetti prefab here (e.g. BurstJumpConfetti_Regular_Classic or SimpleConfettiBurst) - Triggered EXCLUSIVELY on Victory!")]
    [SerializeField] private GameObject customVictoryConfettiPrefab;
    [SerializeField] private ParticleSystem customSpawnPuffPrefab;
    [SerializeField] private ParticleSystem customLandingRingPrefab;
    [SerializeField] private ParticleSystem customCrumbleDustPrefab;
    [SerializeField] private ParticleSystem customChronoSparklePrefab;

    [Header("--- Platform Spawn Edge Particles Tuning ---")]
    [SerializeField] private Color spawnEdgeColor = new Color(0.92f, 0.95f, 1f, 0.85f);
    [SerializeField] private float spawnEdgeParticleSize = 0.35f;
    [SerializeField] private float spawnEdgeParticleLifetime = 0.40f;
    [SerializeField] private float spawnEdgeParticleSpeed = 2.4f;
    [SerializeField] private int spawnEdgeParticleCount = 48;
    [SerializeField] private float platformDimension = 5f;

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

    [Header("--- Victory Celebration Confetti Tuning ---")]
    [SerializeField] private Color victoryConfettiColor = new Color(1f, 0.85f, 0.15f, 1f);
    [SerializeField] private float victoryConfettiParticleSize = 0.35f;
    [SerializeField] private float victoryConfettiParticleLifetime = 0.90f;
    [SerializeField] private int victoryConfettiParticleCount = 60;

    private ParticleSystem landingRingPS;
    private ParticleSystem spawnEdgeDustPS;
    private ParticleSystem crumbleDustPS;
    private ParticleSystem chronoSparklePS;
    private ParticleSystem victoryConfettiPS;

    private List<GameObject> activeConfettiInstances = new List<GameObject>();
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
        GameEvents.OnGameVictory += HandleGameVictory;
        GameEvents.OnGameStart += ForceClearAllConfetti;
        GameEvents.OnGameRestart += ForceClearAllConfetti;
        GameEvents.OnReturnToLobby += ForceClearAllConfetti;
    }

    private void OnDisable()
    {
        GameEvents.OnPulpitLanded -= HandlePulpitLanded;
        GameEvents.OnPulpitSpawned -= HandlePulpitSpawned;
        GameEvents.OnPulpitDestroyed -= HandlePulpitDestroyed;
        GameEvents.OnRewindComplete -= HandleRewindComplete;
        GameEvents.OnGameVictory -= HandleGameVictory;
        GameEvents.OnGameStart -= ForceClearAllConfetti;
        GameEvents.OnGameRestart -= ForceClearAllConfetti;
        GameEvents.OnReturnToLobby -= ForceClearAllConfetti;
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
        landingRingPS = customLandingRingPrefab != null
            ? Instantiate(customLandingRingPrefab, transform)
            : CreateCircleParticleSystem("Landing_Shockwave_PS", landingParticleCount, landingColor, landingParticleLifetime, landingParticleSize, landingParticleSpeed);

        spawnEdgeDustPS = customSpawnPuffPrefab != null
            ? Instantiate(customSpawnPuffPrefab, transform)
            : CreateEdgeDustParticleSystem("Platform_Edge_Spawn_PS", spawnEdgeParticleCount, spawnEdgeColor, spawnEdgeParticleLifetime, spawnEdgeParticleSize, spawnEdgeParticleSpeed);

        crumbleDustPS = customCrumbleDustPrefab != null
            ? Instantiate(customCrumbleDustPrefab, transform)
            : CreateCircleParticleSystem("Platform_Crumble_PS", crumbleParticleCount, crumbleColor, crumbleParticleLifetime, crumbleParticleSize, crumbleParticleSpeed);

        chronoSparklePS = customChronoSparklePrefab != null
            ? Instantiate(customChronoSparklePrefab, transform)
            : CreateCircleParticleSystem("Chrono_Sparkle_PS", chronoParticleCount, chronoColor, chronoParticleLifetime, chronoParticleSize, 1.2f);

        victoryConfettiPS = CreateCircleParticleSystem("Victory_Confetti_PS", victoryConfettiParticleCount, victoryConfettiColor, victoryConfettiParticleLifetime, victoryConfettiParticleSize, 3.5f);
        if (victoryConfettiPS != null)
        {
            var main = victoryConfettiPS.main;
            main.useUnscaledTime = true;
        }
    }

    private ParticleSystem CreateEdgeDustParticleSystem(string name, int maxParticles, Color color, float lifetime, float size, float speed)
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
        main.maxParticles = maxParticles * 4;
        main.startLifetime = lifetime;
        main.startSize = size;
        main.startSpeed = speed;
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0;

        var shape = ps.shape;
        shape.enabled = false;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.4f);
        sizeCurve.AddKey(0.25f, 1.25f);
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
        sizeCurve.AddKey(0f, 0.3f);
        sizeCurve.AddKey(0.3f, 1f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        return ps;
    }

    #region Public Emission Triggers
    public void SpawnPlatformEdgeDust(Vector3 platformCenter)
    {
        if (spawnEdgeDustPS == null) return;

        float halfDim = platformDimension * 0.5f;
        float bottomY = platformCenter.y - 0.20f;
        int particlesPerEdge = spawnEdgeParticleCount / 4;

        for (int i = 0; i < particlesPerEdge; i++)
        {
            float t = (float)i / (particlesPerEdge - 1);
            float offset = Mathf.Lerp(-halfDim, halfDim, t);

            spawnEdgeDustPS.Emit(new ParticleSystem.EmitParams
            {
                position = new Vector3(platformCenter.x + offset, bottomY, platformCenter.z + halfDim),
                velocity = new Vector3(0f, 0.1f, spawnEdgeParticleSpeed),
                startColor = spawnEdgeColor,
                startSize = spawnEdgeParticleSize,
                startLifetime = spawnEdgeParticleLifetime
            }, 1);

            spawnEdgeDustPS.Emit(new ParticleSystem.EmitParams
            {
                position = new Vector3(platformCenter.x + offset, bottomY, platformCenter.z - halfDim),
                velocity = new Vector3(0f, 0.1f, -spawnEdgeParticleSpeed),
                startColor = spawnEdgeColor,
                startSize = spawnEdgeParticleSize,
                startLifetime = spawnEdgeParticleLifetime
            }, 1);

            spawnEdgeDustPS.Emit(new ParticleSystem.EmitParams
            {
                position = new Vector3(platformCenter.x + halfDim, bottomY, platformCenter.z + offset),
                velocity = new Vector3(spawnEdgeParticleSpeed, 0.1f, 0f),
                startColor = spawnEdgeColor,
                startSize = spawnEdgeParticleSize,
                startLifetime = spawnEdgeParticleLifetime
            }, 1);

            spawnEdgeDustPS.Emit(new ParticleSystem.EmitParams
            {
                position = new Vector3(platformCenter.x - halfDim, bottomY, platformCenter.z + offset),
                velocity = new Vector3(-spawnEdgeParticleSpeed, 0.1f, 0f),
                startColor = spawnEdgeColor,
                startSize = spawnEdgeParticleSize,
                startLifetime = spawnEdgeParticleLifetime
            }, 1);
        }
    }

    public void SpawnLandingDust(Vector3 position)
    {
        if (landingRingPS != null)
        {
            landingRingPS.transform.position = position + new Vector3(0f, 0.05f, 0f);
            landingRingPS.Emit(landingParticleCount);
        }
    }

    public void SpawnPlatformCrumbleDust(Vector3 position)
    {
        if (crumbleDustPS != null)
        {
            crumbleDustPS.transform.position = position;
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

    public void SpawnVictoryConfetti(Vector3 position)
    {
        if (customVictoryConfettiPrefab != null)
        {
            GameObject confettiObj = Instantiate(customVictoryConfettiPrefab, position + Vector3.up * 1.5f, Quaternion.identity);
            activeConfettiInstances.Add(confettiObj);

            ParticleSystem[] systems = confettiObj.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in systems)
            {
                var main = ps.main;
                main.useUnscaledTime = true;
                ps.Play();
            }
            StartCoroutine(DestroyRealtimeCoroutine(confettiObj, 6f));
        }
        else if (victoryConfettiPS != null)
        {
            var main = victoryConfettiPS.main;
            main.useUnscaledTime = true;
            victoryConfettiPS.transform.position = position + new Vector3(0f, 2f, 0f);
            victoryConfettiPS.Emit(victoryConfettiParticleCount);
        }
    }

    public void StopConfettiEmittingNaturally()
    {
        for (int i = 0; i < activeConfettiInstances.Count; i++)
        {
            if (activeConfettiInstances[i] != null)
            {
                ParticleSystem[] systems = activeConfettiInstances[i].GetComponentsInChildren<ParticleSystem>();
                foreach (var ps in systems)
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
            }
        }
    }

    public void ForceClearAllConfetti()
    {
        for (int i = activeConfettiInstances.Count - 1; i >= 0; i--)
        {
            if (activeConfettiInstances[i] != null)
            {
                Destroy(activeConfettiInstances[i]);
            }
        }
        activeConfettiInstances.Clear();

        if (victoryConfettiPS != null)
        {
            victoryConfettiPS.Clear();
        }
    }

    private IEnumerator DestroyRealtimeCoroutine(GameObject obj, float realtimeSeconds)
    {
        yield return new WaitForSecondsRealtime(realtimeSeconds);
        if (obj != null)
        {
            activeConfettiInstances.Remove(obj);
            Destroy(obj);
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

    private void HandlePulpitSpawned(Vector3 pos) => SpawnPlatformEdgeDust(pos);
    private void HandlePulpitDestroyed(Vector3 pos) => SpawnPlatformCrumbleDust(pos);

    private void HandleRewindComplete()
    {
        DoofusController doofus = FindAnyObjectByType<DoofusController>();
        Vector3 pos = doofus != null ? doofus.transform.position : Vector3.zero;
        SpawnChronoSparkle(pos);
    }

    private void HandleGameVictory()
    {
        DoofusController doofus = FindAnyObjectByType<DoofusController>();
        Vector3 pos = doofus != null ? doofus.transform.position : Vector3.zero;
        SpawnVictoryConfetti(pos);
    }
    #endregion
}
