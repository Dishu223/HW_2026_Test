using UnityEngine;

/// <summary>
/// Manages player color customization for Body, Head, and Eyes:
/// - Supports URP Lit materials (_BaseColor)
/// - Direct runtime renderer recoloring for instant feedback
/// - Persistent saving via PlayerPrefs
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
        Color.white,                     // Classic Snow White
        new Color(0.98f, 0.45f, 0.55f),  // Vibrant Pink
        new Color(0.25f, 0.70f, 1.00f),  // Sky Cyan
        new Color(0.30f, 0.90f, 0.50f),  // Bright Emerald
        new Color(0.95f, 0.75f, 0.15f),  // Gold Yellow
        new Color(0.70f, 0.40f, 0.95f),  // Mystic Purple
        new Color(1.00f, 0.50f, 0.20f),  // Sunset Orange
        new Color(0.35f, 0.40f, 0.45f)   // Dark Slate
    };

    public Color[] eyeColors = new Color[]
    {
        new Color(0.08f, 0.08f, 0.08f),  // Jet Black (Default)
        new Color(0.10f, 0.60f, 1.00f),  // Neon Blue
        new Color(0.15f, 0.85f, 0.40f),  // Emerald Green
        new Color(0.95f, 0.20f, 0.20f),  // Crimson Red
        new Color(1.00f, 0.80f, 0.10f),  // Electric Yellow
        new Color(0.90f, 0.35f, 0.90f),  // Magenta
        Color.white                      // Pure White
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
        // 1. Tint Shared Material Assets
        if (bodyMaterial != null && CurrentBodyIndex < bodyColors.Length)
        {
            Color c = bodyColors[CurrentBodyIndex];
            if (bodyMaterial.HasProperty("_BaseColor")) bodyMaterial.SetColor("_BaseColor", c);
            else bodyMaterial.color = c;
        }

        if (headMaterial != null && CurrentHeadIndex < bodyColors.Length)
        {
            Color c = bodyColors[CurrentHeadIndex];
            if (headMaterial.HasProperty("_BaseColor")) headMaterial.SetColor("_BaseColor", c);
            else headMaterial.color = c;
        }

        if (eyesMaterial != null && CurrentEyeIndex < eyeColors.Length)
        {
            Color c = eyeColors[CurrentEyeIndex];
            if (eyesMaterial.HasProperty("_BaseColor")) eyesMaterial.SetColor("_BaseColor", c);
            else eyesMaterial.color = c;
        }

        // 2. Direct Runtime Renderer Tinting on Doofus in Scene
        DoofusController doofus = FindAnyObjectByType<DoofusController>();
        if (doofus != null)
        {
            Transform bodyObj = doofus.transform.Find("Body");
            Transform headObj = doofus.transform.Find("Head");

            if (bodyObj != null)
            {
                MeshRenderer mr = bodyObj.GetComponent<MeshRenderer>();
                if (mr != null) SetRendererColor(mr, bodyColors[CurrentBodyIndex]);
            }

            if (headObj != null)
            {
                MeshRenderer mr = headObj.GetComponent<MeshRenderer>();
                if (mr != null) SetRendererColor(mr, bodyColors[CurrentHeadIndex]);

                Transform leftEye = headObj.Find("LeftEye");
                Transform rightEye = headObj.Find("RightEye");

                if (leftEye != null)
                {
                    MeshRenderer mrEye = leftEye.GetComponent<MeshRenderer>();
                    if (mrEye != null) SetRendererColor(mrEye, eyeColors[CurrentEyeIndex]);
                }
                if (rightEye != null)
                {
                    MeshRenderer mrEye = rightEye.GetComponent<MeshRenderer>();
                    if (mrEye != null) SetRendererColor(mrEye, eyeColors[CurrentEyeIndex]);
                }
            }
        }
    }

    private void SetRendererColor(Renderer r, Color c)
    {
        if (r == null) return;
        if (r.material.HasProperty("_BaseColor"))
            r.material.SetColor("_BaseColor", c);
        else
            r.material.color = c;
    }

    private void LoadSavedColors()
    {
        CurrentBodyIndex = PlayerPrefs.GetInt(BODY_COLOR_KEY, 0);
        CurrentHeadIndex = PlayerPrefs.GetInt(HEAD_COLOR_KEY, 0);
        CurrentEyeIndex = PlayerPrefs.GetInt(EYE_COLOR_KEY, 0);
    }
}
