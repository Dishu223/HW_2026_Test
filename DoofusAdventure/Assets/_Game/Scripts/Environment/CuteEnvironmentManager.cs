using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Cute Environment & Lighting Manager:
/// - Transforms the plain void into a vibrant, dreamy pastel wonderland!
/// - Spawns procedural floating fluffy cartoon clouds drifting gently in the depth abyss
/// - Spawns subtle ambient floating sun sparkles
/// - Configures warm Pixar-style sunlight and URP Bloom + Color Vibrancy post-processing
/// </summary>
public class CuteEnvironmentManager : MonoBehaviour
{
    public static CuteEnvironmentManager Instance { get; private set; }

    [Header("Dreamy Sky Colors")]
    [SerializeField] private Color skyTopColor = new Color(0.38f, 0.78f, 0.98f);     // Sunny Sky Cyan
    [SerializeField] private Color skyHorizonColor = new Color(0.72f, 0.88f, 1f);    // Soft Pastel Ice
    [SerializeField] private Color skyGroundColor = new Color(0.95f, 0.75f, 0.85f);  // Sweet Candy Lilac

    [Header("Drifting Fluffy Clouds")]
    [SerializeField] private int cloudCount = 18;
    [SerializeField] private float cloudSpeed = 1.2f;
    [SerializeField] private float cloudSpawnRadius = 35f;

    private List<Transform> activeClouds = new List<Transform>();
    private List<float> cloudSpeeds = new List<float>();

    private Material cloudMaterial;
    private ParticleSystem ambientSparklesPS;

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
        SpawnDriftingCartoonClouds();
        BuildAmbientSunSparkles();
    }

    private void ApplyCuteCameraBackground()
    {
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.42f, 0.76f, 0.94f); // Dreamy Sunny Cyan

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = skyTopColor * 1.1f;
            RenderSettings.ambientEquatorColor = skyHorizonColor * 0.9f;
            RenderSettings.ambientGroundColor = skyGroundColor * 0.8f;
        }

        Light sun = FindAnyObjectByType<Light>();
        if (sun != null && sun.type == LightType.Directional)
        {
            sun.color = new Color(1f, 0.97f, 0.90f); // Warm golden sunlight
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

        // 1. Dreamy Bloom
        if (!profile.TryGet(out Bloom bloom)) bloom = profile.Add<Bloom>(true);
        if (bloom != null)
        {
            bloom.threshold.overrideState = true;
            bloom.threshold.value = 0.92f;
            bloom.intensity.overrideState = true;
            bloom.intensity.value = 0.65f;
            bloom.scatter.overrideState = true;
            bloom.scatter.value = 0.70f;
        }

        // 2. Color Adjustments (Pixar/Mario Vibrancy)
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

        // 3. Tonemapping (ACES)
        if (!profile.TryGet(out Tonemapping tonemapping)) tonemapping = profile.Add<Tonemapping>(true);
        if (tonemapping != null)
        {
            tonemapping.mode.overrideState = true;
            tonemapping.mode.value = TonemappingMode.ACES;
        }

        // 4. Soft Dreamy Vignette
        if (!profile.TryGet(out Vignette vignette)) vignette = profile.Add<Vignette>(true);
        if (vignette != null)
        {
            vignette.intensity.overrideState = true;
            vignette.intensity.value = 0.22f;
            vignette.smoothness.overrideState = true;
            vignette.smoothness.value = 0.40f;
            vignette.color.overrideState = true;
            vignette.color.value = new Color(0.1f, 0.2f, 0.35f);
        }
    }

    private void CreateCloudMaterial()
    {
        Shader unlitShader = Shader.Find("Universal Render Pipeline/Lit");
        if (unlitShader == null) unlitShader = Shader.Find("Universal Render Pipeline/Simple Lit");
        if (unlitShader == null) unlitShader = Shader.Find("Standard");

        cloudMaterial = new Material(unlitShader);
        cloudMaterial.color = new Color(0.98f, 0.98f, 1f, 0.95f);
    }

    private void SpawnDriftingCartoonClouds()
    {
        GameObject cloudRoot = new GameObject("--- Dreamy_Clouds_Backdrop ---");
        cloudRoot.transform.SetParent(transform);

        for (int i = 0; i < cloudCount; i++)
        {
            GameObject cloudObj = CreateSingleFluffyCloud(cloudRoot.transform);

            float angle = Random.Range(0f, Mathf.PI * 2f);
            float dist = Random.Range(15f, cloudSpawnRadius);
            float x = Mathf.Cos(angle) * dist;
            float z = Mathf.Sin(angle) * dist;
            float y = Random.Range(-12f, -3f);

            cloudObj.transform.position = new Vector3(x, y, z);
            float scale = Random.Range(1.8f, 4.2f);
            cloudObj.transform.localScale = new Vector3(scale * 1.5f, scale * 0.8f, scale * 1.2f);

            activeClouds.Add(cloudObj.transform);
            cloudSpeeds.Add(Random.Range(0.6f, 1.8f) * cloudSpeed);
        }
    }

    private GameObject CreateSingleFluffyCloud(Transform parent)
    {
        GameObject cloud = new GameObject("Fluffy_Cloud");
        cloud.transform.SetParent(parent, false);

        int puffCount = Random.Range(3, 5);
        for (int p = 0; p < puffCount; p++)
        {
            GameObject puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            puff.transform.SetParent(cloud.transform, false);

            Collider col = puff.GetComponent<Collider>();
            if (col != null) Destroy(col);

            MeshRenderer mr = puff.GetComponent<MeshRenderer>();
            if (mr != null && cloudMaterial != null) mr.sharedMaterial = cloudMaterial;

            float xOffset = (p - (puffCount * 0.5f)) * 0.75f + Random.Range(-0.2f, 0.2f);
            float yOffset = Random.Range(-0.15f, 0.3f);
            float zOffset = Random.Range(-0.3f, 0.3f);
            puff.transform.localPosition = new Vector3(xOffset, yOffset, zOffset);

            float s = Random.Range(0.8f, 1.3f);
            puff.transform.localScale = Vector3.one * s;
        }

        return cloud;
    }

    private void BuildAmbientSunSparkles()
    {
        GameObject psObj = new GameObject("Ambient_Sun_Sparkles");
        psObj.transform.SetParent(transform);
        psObj.transform.position = new Vector3(0f, 2f, 0f);

        ambientSparklesPS = psObj.AddComponent<ParticleSystem>();
        var main = ambientSparklesPS.main;
        main.maxParticles = 60;
        main.startLifetime = 4f;
        main.startSize = 0.18f;
        main.startSpeed = 0.4f;
        main.startColor = new Color(1f, 0.98f, 0.8f, 0.65f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ambientSparklesPS.emission;
        emission.rateOverTime = 12;

        var shape = ambientSparklesPS.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(25f, 10f, 25f);

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
        for (int i = 0; i < activeClouds.Count; i++)
        {
            if (activeClouds[i] != null)
            {
                Transform c = activeClouds[i];
                float speed = (i < cloudSpeeds.Count) ? cloudSpeeds[i] : 1f;

                c.position += new Vector3(speed * Time.deltaTime, 0f, 0f);

                if (c.position.x > cloudSpawnRadius + 10f)
                {
                    c.position = new Vector3(-cloudSpawnRadius - 10f, c.position.y, c.position.z);
                }
            }
        }
    }
}
