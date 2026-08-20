using UnityEngine;

/// <summary>
/// Manages player color customization for Body, Head, and Eyes:
/// - Supports preset palettes AND custom color wheel / spectrum selection
/// - Direct URP Lit material (_BaseColor) and runtime renderer recoloring
/// - Persistent saving via PlayerPrefs
/// </summary>
public class CustomizationManager : MonoBehaviour
{
    public static CustomizationManager Instance { get; private set; }

    public enum CustomPart { Body, Head, Eyes }

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

    // Full 360-Degree Rainbow Color Wheel Swatches
    public Color[] rainbowWheelColors = new Color[]
    {
        new Color(1f, 0.15f, 0.15f),    // 0° Pure Red
        new Color(1f, 0.50f, 0.10f),    // 30° Orange
        new Color(1f, 0.88f, 0.10f),    // 60° Yellow
        new Color(0.65f, 0.95f, 0.15f), // 90° Lime Green
        new Color(0.15f, 0.85f, 0.35f), // 120° Green
        new Color(0.10f, 0.85f, 0.85f), // 180° Cyan
        new Color(0.20f, 0.45f, 1f),    // 240° Blue
        new Color(0.60f, 0.20f, 1f),    // 280° Purple
        new Color(0.95f, 0.25f, 0.85f), // 310° Magenta
        new Color(1f, 0.40f, 0.65f),    // 340° Bubblegum Pink
        Color.white,                     // Pure White
        new Color(0.12f, 0.12f, 0.12f)  // Jet Black
    };

    private const string BODY_COLOR_KEY = "Doofus_BodyColorHex";
    private const string HEAD_COLOR_KEY = "Doofus_HeadColorHex";
    private const string EYE_COLOR_KEY = "Doofus_EyeColorHex";

    public Color CurrentBodyColor { get; private set; } = Color.white;
    public Color CurrentHeadColor { get; private set; } = Color.white;
    public Color CurrentEyeColor { get; private set; } = new Color(0.08f, 0.08f, 0.08f);

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
            SetCustomPartColor(CustomPart.Body, bodyColors[colorIndex]);
        }
    }

    public void SetHeadColor(int colorIndex)
    {
        if (colorIndex >= 0 && colorIndex < bodyColors.Length)
        {
            CurrentHeadIndex = colorIndex;
            SetCustomPartColor(CustomPart.Head, bodyColors[colorIndex]);
        }
    }

    public void SetEyeColor(int colorIndex)
    {
        if (colorIndex >= 0 && colorIndex < eyeColors.Length)
        {
            CurrentEyeIndex = colorIndex;
            SetCustomPartColor(CustomPart.Eyes, eyeColors[colorIndex]);
        }
    }

    public void SetCustomPartColor(CustomPart part, Color color)
    {
        switch (part)
        {
            case CustomPart.Body:
                CurrentBodyColor = color;
                PlayerPrefs.SetString(BODY_COLOR_KEY, "#" + ColorUtility.ToHtmlStringRGBA(color));
                break;
            case CustomPart.Head:
                CurrentHeadColor = color;
                PlayerPrefs.SetString(HEAD_COLOR_KEY, "#" + ColorUtility.ToHtmlStringRGBA(color));
                break;
            case CustomPart.Eyes:
                CurrentEyeColor = color;
                PlayerPrefs.SetString(EYE_COLOR_KEY, "#" + ColorUtility.ToHtmlStringRGBA(color));
                break;
        }

        PlayerPrefs.Save();
        ApplyCurrentColors();
    }

    public void ApplyCurrentColors()
    {
        // 1. Tint Materials
        if (bodyMaterial != null)
        {
            if (bodyMaterial.HasProperty("_BaseColor")) bodyMaterial.SetColor("_BaseColor", CurrentBodyColor);
            else bodyMaterial.color = CurrentBodyColor;
        }

        if (headMaterial != null)
        {
            if (headMaterial.HasProperty("_BaseColor")) headMaterial.SetColor("_BaseColor", CurrentHeadColor);
            else headMaterial.color = CurrentHeadColor;
        }

        if (eyesMaterial != null)
        {
            if (eyesMaterial.HasProperty("_BaseColor")) eyesMaterial.SetColor("_BaseColor", CurrentEyeColor);
            else eyesMaterial.color = CurrentEyeColor;
        }

        // 2. Direct Runtime Renderer Tinting on Character in Scene
        DoofusController doofus = FindAnyObjectByType<DoofusController>();
        if (doofus != null)
        {
            Transform bodyObj = doofus.transform.Find("Body");
            Transform headObj = doofus.transform.Find("Head");

            if (bodyObj != null)
            {
                MeshRenderer mr = bodyObj.GetComponent<MeshRenderer>();
                if (mr != null) SetRendererColor(mr, CurrentBodyColor);
            }

            if (headObj != null)
            {
                MeshRenderer mr = headObj.GetComponent<MeshRenderer>();
                if (mr != null) SetRendererColor(mr, CurrentHeadColor);

                Transform leftEye = headObj.Find("LeftEye");
                Transform rightEye = headObj.Find("RightEye");

                if (leftEye != null)
                {
                    MeshRenderer mrEye = leftEye.GetComponent<MeshRenderer>();
                    if (mrEye != null) SetRendererColor(mrEye, CurrentEyeColor);
                }
                if (rightEye != null)
                {
                    MeshRenderer mrEye = rightEye.GetComponent<MeshRenderer>();
                    if (mrEye != null) SetRendererColor(mrEye, CurrentEyeColor);
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
        string bodyHex = PlayerPrefs.GetString(BODY_COLOR_KEY, "");
        if (!string.IsNullOrEmpty(bodyHex) && ColorUtility.TryParseHtmlString(bodyHex, out Color bCol))
            CurrentBodyColor = bCol;
        else
            CurrentBodyColor = Color.white;

        string headHex = PlayerPrefs.GetString(HEAD_COLOR_KEY, "");
        if (!string.IsNullOrEmpty(headHex) && ColorUtility.TryParseHtmlString(headHex, out Color hCol))
            CurrentHeadColor = hCol;
        else
            CurrentHeadColor = Color.white;

        string eyeHex = PlayerPrefs.GetString(EYE_COLOR_KEY, "");
        if (!string.IsNullOrEmpty(eyeHex) && ColorUtility.TryParseHtmlString(eyeHex, out Color eCol))
            CurrentEyeColor = eCol;
        else
            CurrentEyeColor = new Color(0.08f, 0.08f, 0.08f);
    }
}
