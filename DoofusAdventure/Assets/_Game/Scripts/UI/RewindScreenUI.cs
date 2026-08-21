using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// Retro VHS Time Rewind & Glitch Screen FX:
/// - Full-Screen CRT scanlines across entire UI Canvas
/// - Full-Width RGB Glitch Slices (Vivid Red / Electric Cyan) spanning 100% edge-to-edge
/// - Full-Width VHS Tape Tracking Noise Bar rolling down the screen
/// - URP Post-Processing Chromatic Aberration & Lens Distortion
/// - Centered Fredoka Time Rewind / Resume prompts
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class RewindScreenUI : MonoBehaviour
{
    [Header("UI Visual Elements")]
    [SerializeField] private Image vignetteOverlayImage;
    [SerializeField] private TextMeshProUGUI rewindBannerText;

    private CanvasGroup canvasGroup;
    private bool isOverlayActive = false;
    private float fadeSpeed = 8f;

    // Post-Processing
    private Volume volume;
    private ChromaticAberration chromaticAberration;
    private LensDistortion lensDistortion;
    private Vignette vignette;

    // Glitch Elements (Built inside Rewind_Panel for 100% true full-screen scaling)
    private RawImage scanlineImage;
    private RectTransform trackingBarRT;
    private RawImage trackingBarImage;
    private List<RectTransform> glitchStrips = new List<RectTransform>();
    private List<Image> glitchStripImages = new List<Image>();

    private Texture2D scanlineTexture;
    private Texture2D noiseTexture;
    private float trackingBarY = 0f;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        SetupURPVolume();
        BuildFullScreenGlitchElements();
        HideImmediate();
    }

    private void SetupURPVolume()
    {
        volume = FindAnyObjectByType<Volume>();
        if (volume != null)
        {
            VolumeProfile profile = volume.profile != null ? volume.profile : volume.sharedProfile;
            if (profile != null)
            {
                if (!profile.TryGet(out chromaticAberration)) chromaticAberration = profile.Add<ChromaticAberration>(true);
                if (chromaticAberration != null)
                {
                    chromaticAberration.intensity.overrideState = true;
                    chromaticAberration.intensity.value = 0f;
                }

                if (!profile.TryGet(out lensDistortion)) lensDistortion = profile.Add<LensDistortion>(true);
                if (lensDistortion != null)
                {
                    lensDistortion.intensity.overrideState = true;
                    lensDistortion.intensity.value = 0f;
                }

                if (!profile.TryGet(out vignette)) vignette = profile.Add<Vignette>(true);
            }
        }
    }

    private void BuildFullScreenGlitchElements()
    {
        scanlineTexture = GenerateScanlineTexture(64, 64);
        noiseTexture = GenerateNoiseTexture(128, 128);

        // 1. Full-Screen CRT Scanlines
        Transform existingScan = transform.Find("CRT_Scanlines");
        if (existingScan == null)
        {
            GameObject scanObj = new GameObject("CRT_Scanlines");
            scanObj.transform.SetParent(transform, false);
            scanObj.transform.SetAsFirstSibling();

            RectTransform scanRT = scanObj.AddComponent<RectTransform>();
            scanRT.anchorMin = Vector2.zero;
            scanRT.anchorMax = Vector2.one;
            scanRT.offsetMin = Vector2.zero;
            scanRT.offsetMax = Vector2.zero;

            scanlineImage = scanObj.AddComponent<RawImage>();
            scanlineImage.texture = scanlineTexture;
            scanlineImage.uvRect = new Rect(0, 0, 1, 35);
            scanlineImage.color = new Color(0f, 0.95f, 1f, 0.25f);
            scanlineImage.raycastTarget = false;
        }

        // 2. Full-Width VHS Tape Tracking Noise Bar (100% edge-to-edge)
        Transform existingTrack = transform.Find("VHS_Tracking_Bar");
        if (existingTrack == null)
        {
            GameObject trackObj = new GameObject("VHS_Tracking_Bar");
            trackObj.transform.SetParent(transform, false);
            trackObj.transform.SetSiblingIndex(1);

            trackingBarRT = trackObj.AddComponent<RectTransform>();
            trackingBarRT.anchorMin = new Vector2(0f, 0.5f);
            trackingBarRT.anchorMax = new Vector2(1f, 0.5f);
            trackingBarRT.offsetMin = new Vector2(0f, -40f);
            trackingBarRT.offsetMax = new Vector2(0f, 40f);

            trackingBarImage = trackObj.AddComponent<RawImage>();
            trackingBarImage.texture = noiseTexture;
            trackingBarImage.uvRect = new Rect(0, 0, 12, 1);
            trackingBarImage.color = new Color(1f, 1f, 1f, 0.55f);
            trackingBarImage.raycastTarget = false;
        }

        // 3. Dynamic Full-Width RGB Chromatic Tear Strips (100% edge-to-edge)
        for (int i = 0; i < 6; i++)
        {
            string stripName = $"RGB_Glitch_Strip_{i}";
            Transform existingStrip = transform.Find(stripName);
            if (existingStrip != null) Destroy(existingStrip.gameObject);

            GameObject stripObj = new GameObject(stripName);
            stripObj.transform.SetParent(transform, false);
            stripObj.transform.SetSiblingIndex(2 + i);

            RectTransform stripRT = stripObj.AddComponent<RectTransform>();
            stripRT.anchorMin = new Vector2(0f, 0.5f);
            stripRT.anchorMax = new Vector2(1f, 0.5f);
            stripRT.offsetMin = new Vector2(0f, -20f);
            stripRT.offsetMax = new Vector2(0f, 20f);

            Image stripImg = stripObj.AddComponent<Image>();
            stripImg.color = Color.clear;
            stripImg.raycastTarget = false;

            glitchStrips.Add(stripRT);
            glitchStripImages.Add(stripImg);
        }

        // Ensure text is on top of all glitch layers
        if (rewindBannerText != null)
        {
            rewindBannerText.transform.SetAsLastSibling();
        }
    }

    private Texture2D GenerateScanlineTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Point;

        Color[] pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            bool isStripe = (y % 3) == 0;
            Color col = isStripe ? new Color(0f, 0f, 0f, 0.55f) : new Color(0f, 0.85f, 1f, 0.12f);
            for (int x = 0; x < width; x++) pixels[y * width + x] = col;
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private Texture2D GenerateNoiseTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Point;

        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            float val = Random.value > 0.35f ? Random.Range(0.6f, 1f) : 0f;
            pixels[i] = new Color(val, val, val, val * 0.65f);
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private void OnEnable()
    {
        GameEvents.OnRewindStart += HandleRewindStart;
        GameEvents.OnRewindReadyToResume += HandleReadyToResume;
        GameEvents.OnRewindComplete += HandleRewindComplete;
        GameEvents.OnGameStart += HideImmediate;
        GameEvents.OnGameOver += HideImmediate;
    }

    private void OnDisable()
    {
        GameEvents.OnRewindStart -= HandleRewindStart;
        GameEvents.OnRewindReadyToResume -= HandleReadyToResume;
        GameEvents.OnRewindComplete -= HandleRewindComplete;
        GameEvents.OnGameStart -= HideImmediate;
        GameEvents.OnGameOver -= HideImmediate;
    }

    private void HandleRewindStart()
    {
        isOverlayActive = true;
        trackingBarY = 400f;

        if (rewindBannerText != null)
        {
            rewindBannerText.text = "<< TIME REWIND <<";
            rewindBannerText.color = new Color(0f, 0.95f, 1f, 1f);
        }

        if (chromaticAberration != null) chromaticAberration.intensity.value = 1.0f;
        if (lensDistortion != null) lensDistortion.intensity.value = -0.30f;
    }

    private void HandleReadyToResume()
    {
        isOverlayActive = true;
        if (rewindBannerText != null)
        {
            rewindBannerText.text = ">> PRESS WASD OR SPACE TO RESUME <<";
            rewindBannerText.color = Color.white;
        }

        if (chromaticAberration != null) chromaticAberration.intensity.value = 0.40f;
        if (lensDistortion != null) lensDistortion.intensity.value = -0.10f;
    }

    private void HandleRewindComplete()
    {
        isOverlayActive = false;
        if (chromaticAberration != null) chromaticAberration.intensity.value = 0f;
        if (lensDistortion != null) lensDistortion.intensity.value = 0f;
    }

    public void HideImmediate()
    {
        isOverlayActive = false;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }
        if (chromaticAberration != null) chromaticAberration.intensity.value = 0f;
        if (lensDistortion != null) lensDistortion.intensity.value = 0f;
    }

    private void Update()
    {
        if (canvasGroup == null) return;

        float targetAlpha = isOverlayActive ? 1f : 0f;
        canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, Time.unscaledDeltaTime * fadeSpeed);
        canvasGroup.blocksRaycasts = false;

        if (!isOverlayActive) return;

        // 1. Roll Scanlines
        if (scanlineImage != null)
        {
            Rect uv = scanlineImage.uvRect;
            uv.y -= Time.unscaledDeltaTime * 14f;
            scanlineImage.uvRect = uv;
        }

        // 2. Move Full-Width Tracking Bar down the screen
        trackingBarY -= Time.unscaledDeltaTime * 380f;
        if (trackingBarY < -500f) trackingBarY = 500f;

        if (trackingBarRT != null)
        {
            trackingBarRT.offsetMin = new Vector2(0f, trackingBarY - 40f);
            trackingBarRT.offsetMax = new Vector2(0f, trackingBarY + 40f);
        }

        // 3. Jitter Full-Width Edge-to-Edge RGB Glitch Slices (Vivid Red & Vivid Cyan)
        for (int i = 0; i < glitchStrips.Count; i++)
        {
            if (glitchStrips[i] != null && glitchStripImages[i] != null)
            {
                if (Random.value < 0.35f)
                {
                    float y = Random.Range(-400f, 400f);
                    float h = Random.Range(18f, 55f);
                    float xJitter = Random.Range(-30f, 30f);

                    glitchStrips[i].offsetMin = new Vector2(xJitter, y - h * 0.5f);
                    glitchStrips[i].offsetMax = new Vector2(xJitter, y + h * 0.5f);

                    Color col = (i % 2 == 0)
                        ? new Color(1f, 0.05f, 0.40f, Random.Range(0.35f, 0.75f))  // Vivid Red
                        : new Color(0f, 0.95f, 1f, Random.Range(0.35f, 0.75f)); // Vivid Cyan

                    glitchStripImages[i].color = col;
                }
            }
        }
    }
}
