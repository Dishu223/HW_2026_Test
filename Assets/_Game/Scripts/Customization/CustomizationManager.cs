using UnityEngine;

// Manages color customization for Doofus (Body, Head, Eyes).
// Saves choices to PlayerPrefs and updates materials dynamically.
public class CustomizationManager : MonoBehaviour
{
    public static CustomizationManager Instance { get; private set; }

    [Header("Doofus Renderer References")]
    [SerializeField] private MeshRenderer bodyRenderer;
    [SerializeField] private MeshRenderer headRenderer;
    [SerializeField] private MeshRenderer leftEyeRenderer;
    [SerializeField] private MeshRenderer rightEyeRenderer;

    [Header("Preset Color Palettes")]
    public static readonly Color[] BodyColors = new Color[]
    {
        new Color(0.95f, 0.95f, 0.98f), // Snow White (Default)
        new Color(0.20f, 0.80f, 0.95f), // Cyan Frost
        new Color(1.00f, 0.45f, 0.55f), // Bubblegum Pink
        new Color(0.35f, 0.90f, 0.45f), // Mint Green
        new Color(1.00f, 0.85f, 0.20f), // Sun Yellow
        new Color(0.70f, 0.40f, 0.95f), // Neon Purple
        new Color(1.00f, 0.50f, 0.20f), // Warm Coral
        new Color(0.25f, 0.28f, 0.35f)  // Slate Dark
    };

    public static readonly Color[] HeadColors = new Color[]
    {
        new Color(0.95f, 0.95f, 0.98f), // Snow White (Default)
        new Color(0.20f, 0.80f, 0.95f), // Cyan Frost
        new Color(1.00f, 0.45f, 0.55f), // Bubblegum Pink
        new Color(0.35f, 0.90f, 0.45f), // Mint Green
        new Color(1.00f, 0.85f, 0.20f), // Sun Yellow
        new Color(0.70f, 0.40f, 0.95f), // Neon Purple
        new Color(1.00f, 0.50f, 0.20f), // Warm Coral
        new Color(0.25f, 0.28f, 0.35f)  // Slate Dark
    };

    public static readonly Color[] EyeColors = new Color[]
    {
        new Color(0.08f, 0.08f, 0.10f), // Jet Black (Default)
        new Color(0.15f, 0.55f, 0.95f), // Ocean Blue
        new Color(0.15f, 0.85f, 0.35f), // Emerald Green
        new Color(0.95f, 0.20f, 0.20f), // Ruby Red
        new Color(1.00f, 0.80f, 0.10f), // Golden Amber
        new Color(0.95f, 0.95f, 0.98f)  // Pure White
    };

    private const string BODY_COLOR_KEY = "Custom_BodyColorIndex";
    private const string HEAD_COLOR_KEY = "Custom_HeadColorIndex";
    private const string EYE_COLOR_KEY = "Custom_EyeColorIndex";

    public int SelectedBodyIndex { get; private set; } = 0;
    public int SelectedHeadIndex { get; private set; } = 0;
    public int SelectedEyeIndex { get; private set; } = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        LoadSavedCustomization();
    }

    private void Start()
    {
        ApplyCustomization();
    }

    public void SetBodyColor(int index)
    {
        if (index < 0 || index >= BodyColors.Length) return;
        SelectedBodyIndex = index;
        SaveAndApply();
    }

    public void SetHeadColor(int index)
    {
        if (index < 0 || index >= HeadColors.Length) return;
        SelectedHeadIndex = index;
        SaveAndApply();
    }

    public void SetEyeColor(int index)
    {
        if (index < 0 || index >= EyeColors.Length) return;
        SelectedEyeIndex = index;
        SaveAndApply();
    }

    private void SaveAndApply()
    {
        PlayerPrefs.SetInt(BODY_COLOR_KEY, SelectedBodyIndex);
        PlayerPrefs.SetInt(HEAD_COLOR_KEY, SelectedHeadIndex);
        PlayerPrefs.SetInt(EYE_COLOR_KEY, SelectedEyeIndex);
        PlayerPrefs.Save();

        ApplyCustomization();
    }

    private void LoadSavedCustomization()
    {
        SelectedBodyIndex = PlayerPrefs.GetInt(BODY_COLOR_KEY, 0);
        SelectedHeadIndex = PlayerPrefs.GetInt(HEAD_COLOR_KEY, 0);
        SelectedEyeIndex = PlayerPrefs.GetInt(EYE_COLOR_KEY, 0);
    }

    public void ApplyCustomization()
    {
        if (bodyRenderer != null && SelectedBodyIndex < BodyColors.Length)
        {
            bodyRenderer.material.color = BodyColors[SelectedBodyIndex];
        }

        if (headRenderer != null && SelectedHeadIndex < HeadColors.Length)
        {
            headRenderer.material.color = HeadColors[SelectedHeadIndex];
        }

        Color eyeColor = EyeColors[Mathf.Clamp(SelectedEyeIndex, 0, EyeColors.Length - 1)];
        if (leftEyeRenderer != null) leftEyeRenderer.material.color = eyeColor;
        if (rightEyeRenderer != null) rightEyeRenderer.material.color = eyeColor;
    }
}
