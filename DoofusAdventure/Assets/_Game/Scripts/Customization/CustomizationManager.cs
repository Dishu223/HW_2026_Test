using UnityEngine;

/// <summary>
/// Manages player color customization for Body, Head, and Eyes.
/// Persists selected colors across sessions using PlayerPrefs and updates materials dynamically.
/// </summary>
public class CustomizationManager : MonoBehaviour
{
    public static CustomizationManager Instance { get; private set; }

    [Header("Material References to Tint")]
    [SerializeField] private Material bodyMaterial;
    [SerializeField] private Material headMaterial;
    [SerializeField] private Material eyesMaterial;

    [Header("Preset Color Palettes")]
    public Color[] bodyColors = new Color[]
    {
        Color.white,
        new Color(0.95f, 0.77f, 0.8f),  // Soft Pink
        new Color(0.6f, 0.85f, 0.98f),  // Sky Blue
        new Color(0.65f, 0.95f, 0.75f), // Mint Green
        new Color(0.85f, 0.75f, 0.95f), // Lavender
        new Color(1.0f, 0.92f, 0.6f),   // Pastel Yellow
        new Color(1.0f, 0.65f, 0.55f),  // Coral
        new Color(0.4f, 0.45f, 0.5f)    // Slate
    };

    public Color[] eyeColors = new Color[]
    {
        new Color(0.1f, 0.1f, 0.1f),    // Jet Black (Default)
        new Color(0.15f, 0.45f, 0.9f),  // Ocean Blue
        new Color(0.15f, 0.75f, 0.35f), // Emerald Green
        new Color(0.85f, 0.2f, 0.2f),   // Ruby Red
        new Color(0.95f, 0.75f, 0.1f),  // Amber Gold
        Color.white
    };

    private const string BODY_COLOR_KEY = "Doofus_BodyColorIndex";
    private const string HEAD_COLOR_KEY = "Doofus_HeadColorIndex";
    private const string EYE_COLOR_KEY = "Doofus_EyeColorIndex";

    public int CurrentBodyIndex { get; private set; } = 0;
    public int CurrentHeadIndex { get; private set; } = 0;
    public int CurrentEyeIndex { get; private set; } = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadSavedColors();
    }

    private void Start()
    {
        ApplyCurrentColors();
    }

    public void SetBodyColor(int colorIndex)
    {
        if (colorIndex >= 0 && colorIndex < bodyColors.Length)
        {
            CurrentBodyIndex = colorIndex;
            PlayerPrefs.SetInt(BODY_COLOR_KEY, CurrentBodyIndex);
            PlayerPrefs.Save();
            ApplyCurrentColors();
        }
    }

    public void SetHeadColor(int colorIndex)
    {
        if (colorIndex >= 0 && colorIndex < bodyColors.Length)
        {
            CurrentHeadIndex = colorIndex;
            PlayerPrefs.SetInt(HEAD_COLOR_KEY, CurrentHeadIndex);
            PlayerPrefs.Save();
            ApplyCurrentColors();
        }
    }

    public void SetEyeColor(int colorIndex)
    {
        if (colorIndex >= 0 && colorIndex < eyeColors.Length)
        {
            CurrentEyeIndex = colorIndex;
            PlayerPrefs.SetInt(EYE_COLOR_KEY, CurrentEyeIndex);
            PlayerPrefs.Save();
            ApplyCurrentColors();
        }
    }

    public void ApplyCurrentColors()
    {
        if (bodyMaterial != null && CurrentBodyIndex < bodyColors.Length)
            bodyMaterial.color = bodyColors[CurrentBodyIndex];

        if (headMaterial != null && CurrentHeadIndex < bodyColors.Length)
            headMaterial.color = bodyColors[CurrentHeadIndex];

        if (eyesMaterial != null && CurrentEyeIndex < eyeColors.Length)
            eyesMaterial.color = eyeColors[CurrentEyeIndex];
    }

    private void LoadSavedColors()
    {
        CurrentBodyIndex = PlayerPrefs.GetInt(BODY_COLOR_KEY, 0);
        CurrentHeadIndex = PlayerPrefs.GetInt(HEAD_COLOR_KEY, 0);
        CurrentEyeIndex = PlayerPrefs.GetInt(EYE_COLOR_KEY, 0);
    }
}
