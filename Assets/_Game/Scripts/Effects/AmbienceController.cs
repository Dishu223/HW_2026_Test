using UnityEngine;

// Smoothly evolves ambient lighting and directional light colors
// as Doofus reaches higher score milestones towards the 50-Pulpit goal.
public class AmbienceController : MonoBehaviour
{
    [Header("Light References")]
    [SerializeField] private Light directionalLight;
    [SerializeField] private Camera mainCamera;

    [Header("Progression Palette Tiers")]
    [SerializeField] private Color tier1Sky = new Color(0.08f, 0.12f, 0.18f); // 0-9: Cool Cyan Abyss
    [SerializeField] private Color tier1Light = new Color(0.8f, 0.95f, 1.0f);

    [SerializeField] private Color tier2Sky = new Color(0.05f, 0.18f, 0.12f); // 10-24: Neon Emerald
    [SerializeField] private Color tier2Light = new Color(0.85f, 1.0f, 0.85f);

    [SerializeField] private Color tier3Sky = new Color(0.08f, 0.10f, 0.25f); // 25-39: Deep Electric Indigo
    [SerializeField] private Color tier3Light = new Color(0.75f, 0.85f, 1.0f);

    [SerializeField] private Color tier4Sky = new Color(0.22f, 0.10f, 0.05f); // 40-49: Sunset Orange Tension
    [SerializeField] private Color tier4Light = new Color(1.0f, 0.80f, 0.65f);

    [SerializeField] private Color tier5Sky = new Color(0.25f, 0.20f, 0.05f); // 50+: Golden Victory Aura
    [SerializeField] private Color tier5Light = new Color(1.0f, 0.95f, 0.70f);

    private Color targetSkyColor;
    private Color targetLightColor;

    private void Awake()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        targetSkyColor = tier1Sky;
        targetLightColor = tier1Light;
    }

    private void OnEnable()
    {
        GameEvents.OnScoreChanged += HandleScoreChanged;
        GameEvents.OnGameStart += ResetAmbience;
        GameEvents.OnGameRestart += ResetAmbience;
    }

    private void OnDisable()
    {
        GameEvents.OnScoreChanged -= HandleScoreChanged;
        GameEvents.OnGameStart -= ResetAmbience;
        GameEvents.OnGameRestart -= ResetAmbience;
    }

    private void Update()
    {
        // Smoothly interpolate colors over time
        if (mainCamera != null)
        {
            mainCamera.backgroundColor = Color.Lerp(mainCamera.backgroundColor, targetSkyColor, Time.deltaTime * 2f);
        }

        if (directionalLight != null)
        {
            directionalLight.color = Color.Lerp(directionalLight.color, targetLightColor, Time.deltaTime * 2f);
        }
    }

    private void HandleScoreChanged(int currentScore)
    {
        if (currentScore >= 50)
        {
            targetSkyColor = tier5Sky;
            targetLightColor = tier5Light;
        }
        else if (currentScore >= 40)
        {
            targetSkyColor = tier4Sky;
            targetLightColor = tier4Light;
        }
        else if (currentScore >= 25)
        {
            targetSkyColor = tier3Sky;
            targetLightColor = tier3Light;
        }
        else if (currentScore >= 10)
        {
            targetSkyColor = tier2Sky;
            targetLightColor = tier2Light;
        }
        else
        {
            targetSkyColor = tier1Sky;
            targetLightColor = tier1Light;
        }
    }

    private void ResetAmbience()
    {
        targetSkyColor = tier1Sky;
        targetLightColor = tier1Light;
    }
}
