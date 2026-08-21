using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Cute Environment, Infinite Cloud Field & Lighting Manager:
/// - Infinite Camera-Centered Cloud Field: Recycles clouds ahead as the player travels forward
/// - Authentic Multi-Puff Cartoon Cloud Geometry (flat underside, rounded dome puffs)
/// - 3 Parallax Cloud Layers (Distant Horizon, Mid-Level Marshmallows, Foreground Cloudlets)
/// - Gentle floating vertical sinusoidal bobbing
/// - Pixar-style URP Color Vibrancy, Bloom, and Ambient Sunny Sparkles
/// </summary>
public class CuteEnvironmentManager : MonoBehaviour
{
    public static CuteEnvironmentManager Instance { get; private set; }

    [Header("Dreamy Sky Colors")]
    [SerializeField] private Color skyTopColor = new Color(0.35f, 0.76f, 0.98f);     // Sunny Cyan
    [SerializeField] private Color skyHorizonColor = new Color(0.70f, 0.88f, 1f);    // Soft Ice Blue
    [SerializeField] private Color skyGroundColor = new Color(0.95f, 0.78f, 0.88f);  // Pastel Lilac

    [Header("Infinite Cloud Configuration")]
    [SerializeField] private int totalClouds = 28;
    [SerializeField] private float cloudForwardRadius = 55f;
    [SerializeField] private float cloudBehindThreshold = 25f;
    [SerializeField] private float cloudLateralRadius = 45f;
    [SerializeField] private float baseDriftSpeed = 1.1f;

    private struct CloudData
    {
        public Transform transform;
        public float driftSpeed;
        public float bobSpeed;
        public float bobAmplitude;
        public float bobPhase;
        public float baseY;
        public int layer; // 0 = Distant, 1 = Mid, 2 = Close
    }

    private List<CloudData> cloudPool = new List<CloudData>();
    private Material cloudMaterial;
    private ParticleSystem ambientSparklesPS;
    private Transform playerOrCam;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        ApplyCuteCameraBackground();
        SetupCutePostProcessing();
        CreateCloudMaterial();
        BuildInfiniteCloudPool();
        BuildAmbientSunSparkles();
    }

    private void Start()
    {
        // Cache camera or player reference
        Camera cam = Camera.main;
        if (cam != null) playerOrCam = cam.transform;
    }

    private void ApplyCuteCameraBackground()
    {
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.38f, 0.74f, 0.95f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = skyTopColor * 1.15f;
            RenderSettings.ambientEquatorColor = skyHorizonColor * 0.95f;
            RenderSettings.ambientGroundColor = skyGroundColor * 0.85f;
        }

        Light sun = FindAnyObjectByType<Light>();
        if (sun != null && sun.type == LightType.Directional)
        {
            sun.color = new Color(1f, 0.97f, 0.90f);
            sun.intensity = 1.35f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(52f, -38f, 0f);
        }
    }

    private void SetupCutePostProcessing()
    {
        Volume volume = FindAnyObjectByType<Volume>();
        if (volume == null)
        {
            GameObject volObj = new GameObject("Cute_PostProcessing_Volume");
            volume = volObj.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 5;
        }

        VolumeProfile profile = volume.profile != null ? volume.profile : volume.sharedProfile;
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.profile = profile;
        }

        if (!profile.TryGet(out Bloom bloom)) bloom = profile.Add<Bloom>(true);
        if (bloom != null)
        {
            bloom.threshold.overrideState = true;
            bloom.threshold.value = 0.92f;
            bloom.intensity.overrideState = true;
            bloom.intensity.value = 0.60f;
            bloom.scatter.overrideState = true;
            bloom.scatter.value = 0.70f;
        }

        if (!profile.TryGet(out ColorAdjustments colorAdj)) colorAdj = profile.Add<ColorAdjustments>(true);
        if (colorAdj != null)
        {
            colorAdj.postExposure.overrideState = true;
            colorAdj.postExposure.value = 0.15f;
            colorAdj.contrast.overrideState = true;
            colorAdj.contrast.value = 14f;
            colorAdj.saturation.overrideState = true;
            colorAdj.saturation.value = 22f;
        }

        if (!profile.TryGet(out Tonemapping tonemapping)) tonemapping = profile.Add<Tonemapping>(true);
        if (tonemapping != null)
        {
            tonemapping.mode.overrideState = true;
            tonemapping.mode.value = TonemappingMode.ACES;
        }

        if (!profile.TryGet(out Vignette vignette)) vignette = profile.Add<Vignette>(true);
        if (vignette != null)
        {
            vignette.intensity.overrideState = true;
            vignette.intensity.value = 0.20f;
            vignette.smoothness.overrideState = true;
            vignette.smoothness.value = 0.40f;
            vignette.color.overrideState = true;
            vignette.color.value = new Color(0.08f, 0.18f, 0.32f);
        }
    }

    private void CreateCloudMaterial()
    {
        Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
        if (litShader == null) litShader = Shader.Find("Universal Render Pipeline/Simple Lit");
        if (litShader == null) litShader = Shader.Find("Standard");

        cloudMaterial = new Material(litShader);
        cloudMaterial.color = new Color(1f, 0.99f, 0.98f);
        cloudMaterial.enableInstancing = true;
    }

    private void BuildInfiniteCloudPool()
    {
        GameObject root = new GameObject("--- Infinite_Cartoon_Clouds ---");
        root.transform.SetParent(transform);

        Vector3 centerPos = Vector3.zero;
        if (playerOrCam != null) centerPos = playerOrCam.position;

        for (int i = 0; i < totalClouds; i++)
        {
            int layer = i % 3; // 0 = Deep Horizon, 1 = Mid, 2 = Foreground
            GameObject cloudObj = BuildCutePuffyCloud(root.transform, layer);

            // Random initial placement in a large cylinder around origin/camera
            float x = Random.Range(-cloudLateralRadius, cloudLateralRadius);
            float z = Random.Range(-cloudBehindThreshold, cloudForwardRadius);
            float y = GetLayerBaseY(layer);

            cloudObj.transform.position = new Vector3(centerPos.x + x, y, centerPos.z + z);

            CloudData data = new CloudData
            {
                transform = cloudObj.transform,
                driftSpeed = GetLayerDriftSpeed(layer),
                bobSpeed = Random.Range(0.8f, 1.6f),
                bobAmplitude = Random.Range(0.2f, 0.6f),
                bobPhase = Random.Range(0f, Mathf.PI * 2f),
                baseY = y,
                layer = layer
            };

            cloudPool.Add(data);
        }
    }

    private float GetLayerBaseY(int layer)
    {
        switch (layer)
        {
            case 0: return Random.Range(-14f, -22f); // Deep Horizon
            case 1: return Random.Range(-7f, -13f);  // Mid-Level
            default: return Random.Range(-3f, -6f);  // Foreground
        }
    }

    private float GetLayerDriftSpeed(int layer)
    {
        switch (layer)
        {
            case 0: return baseDriftSpeed * Random.Range(0.35f, 0.65f); // Slow distant parallax
            case 1: return baseDriftSpeed * Random.Range(0.8f, 1.2f);
            default: return baseDriftSpeed * Random.Range(1.3f, 1.8f);  // Faster foreground breeze
        }
    }

    private GameObject BuildCutePuffyCloud(Transform parent, int layer)
    {
        GameObject cloud = new GameObject($"Cartoon_Cloud_L{layer}");
        cloud.transform.SetParent(parent, false);

        // Cute low-poly fluffy cloud composition:
        // 1 large center dome + 2 medium side domes + 2 small outer puffs + flat bottom
        int puffCount = Random.Range(5, 8);

        // Center main dome
        CreatePuffSphere(cloud.transform, Vector3.up * 0.2f, new Vector3(1.4f, 1.1f, 1.3f));

        // Side domes
        CreatePuffSphere(cloud.transform, new Vector3(-0.95f, -0.05f, 0.1f), new Vector3(1.1f, 0.9f, 1.0f));
        CreatePuffSphere(cloud.transform, new Vector3(0.95f, -0.05f, -0.1f), new Vector3(1.15f, 0.95f, 1.05f));

        // Outer small puffs
        CreatePuffSphere(cloud.transform, new Vector3(-1.65f, -0.25f, -0.05f), new Vector3(0.75f, 0.65f, 0.75f));
        CreatePuffSphere(cloud.transform, new Vector3(1.65f, -0.22f, 0.08f), new Vector3(0.80f, 0.70f, 0.80f));

        // Back puff for 3D depth
        CreatePuffSphere(cloud.transform, new Vector3(0.2f, 0.05f, 0.65f), new Vector3(0.9f, 0.8f, 0.9f));

        // Scale by layer
        float overallScale = 1f;
        if (layer == 0) overallScale = Random.Range(3.5f, 6.0f);     // Massive distant clouds
        else if (layer == 1) overallScale = Random.Range(2.2f, 3.8f); // Mid marshmallow clouds
        else overallScale = Random.Range(1.2f, 2.0f);                // Small cute cloudlets

        cloud.transform.localScale = Vector3.one * overallScale;
        return cloud;
    }

    private void CreatePuffSphere(Transform parent, Vector3 localPos, Vector3 localScale)
    {
        GameObject puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        puff.transform.SetParent(parent, false);
        puff.transform.localPosition = localPos;
        puff.transform.localScale = localScale;

        Collider c = puff.GetComponent<Collider>();
        if (c != null) Destroy(c);

        MeshRenderer mr = puff.GetComponent<MeshRenderer>();
        if (mr != null && cloudMaterial != null)
        {
            mr.sharedMaterial = cloudMaterial;
            mr.shadowCastingMode = ShadowCastingMode.Off; // Pure soft ambient visual
        }
    }

    private void BuildAmbientSunSparkles()
    {
        GameObject psObj = new GameObject("Ambient_Sun_Sparkles");
        psObj.transform.SetParent(transform);
        psObj.transform.position = new Vector3(0f, 2f, 0f);

        ambientSparklesPS = psObj.AddComponent<ParticleSystem>();
        var main = ambientSparklesPS.main;
        main.maxParticles = 50;
        main.startLifetime = 4f;
        main.startSize = 0.15f;
        main.startSpeed = 0.35f;
        main.startColor = new Color(1f, 0.98f, 0.85f, 0.60f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ambientSparklesPS.emission;
        emission.rateOverTime = 10;

        var shape = ambientSparklesPS.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(30f, 10f, 30f);

        var sizeOverLifetime = ambientSparklesPS.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 0f);
        curve.AddKey(0.5f, 1f);
        curve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

        ParticleSystemRenderer psr = psObj.GetComponent<ParticleSystemRenderer>();
        if (psr != null)
        {
            Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (particleShader == null) particleShader = Shader.Find("Particles/Standard Unlit");
            if (particleShader == null) particleShader = Shader.Find("Sprites/Default");
            psr.material = new Material(particleShader);
        }
    }

    private void Update()
    {
        if (playerOrCam == null)
        {
            Camera cam = Camera.main;
            if (cam != null) playerOrCam = cam.transform;
        }

        Vector3 centerPos = (playerOrCam != null) ? playerOrCam.position : Vector3.zero;

        // Update ambient sun sparkles to follow the player area
        if (ambientSparklesPS != null)
        {
            ambientSparklesPS.transform.position = new Vector3(centerPos.x, centerPos.y + 1.5f, centerPos.z);
        }

        // Update each cloud: Drift horizontally + Bob vertically + Infinite wrap around player/camera!
        for (int i = 0; i < cloudPool.Count; i++)
        {
            CloudData c = cloudPool[i];
            if (c.transform == null) continue;

            Vector3 pos = c.transform.position;

            // 1. Horizontal drift (wind breeze along +X)
            pos.x += c.driftSpeed * Time.deltaTime;

            // 2. Gentle sinusoidal vertical floating bob
            float bobOffset = Mathf.Sin(Time.time * c.bobSpeed + c.bobPhase) * c.bobAmplitude;
            pos.y = c.baseY + bobOffset;

            // 3. INFINITE HORIZON RECYCLING (Relative to Camera / Player Center)
            // If the player moved forward in +Z, recycle clouds that fell behind to the front!
            if (pos.z < centerPos.z - cloudBehindThreshold)
            {
                pos.z = centerPos.z + cloudForwardRadius + Random.Range(-5f, 15f);
                pos.x = centerPos.x + Random.Range(-cloudLateralRadius, cloudLateralRadius);
            }
            else if (pos.z > centerPos.z + cloudForwardRadius + 20f)
            {
                pos.z = centerPos.z - cloudBehindThreshold + Random.Range(-5f, 5f);
                pos.x = centerPos.x + Random.Range(-cloudLateralRadius, cloudLateralRadius);
            }

            // Lateral wind wrap around along X
            if (pos.x > centerPos.x + cloudLateralRadius + 15f)
            {
                pos.x = centerPos.x - cloudLateralRadius - 15f;
            }
            else if (pos.x < centerPos.x - cloudLateralRadius - 15f)
            {
                pos.x = centerPos.x + cloudLateralRadius + 15f;
            }

            c.transform.position = pos;
        }
    }
}
