using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// Subtle Retro VHS Time Rewind Screen FX:
/// - Delicate, soft CRT scanlines across full viewport
/// - Thin, subtle RGB chromatic glitch micro-lines during reverse motion (soft 0.15 alpha)
/// - Fades out FAST the instant "PRESS WASD TO RESUME" appears for 100% crystal clear landing view!
/// - Gentle URP Chromatic Aberration & subtle lens warp
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class RewindScreenUI : MonoBehaviour
{
    [Header("UI Visual Elements")]
    [SerializeField] private Image vignetteOverlayImage;
    [SerializeField] private TextMeshProUGUI rewindBannerText;

    private CanvasGroup canvasGroup;
    private bool isOverlayActive = false;
    private bool isRewindingRush = false;
    private float fadeSpeed = 9f;

    // Post-Processing
    private Volume volume;
    private ChromaticAberration chromaticAberration;
    private LensDistortion lensDistortion;
    private Vignette vignette;

    // Glitch Elements
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

        // 1. Full-Screen Delicate CRT Scanlines
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
            scanlineImage.uvRect = new Rect(0, 0, 1, 40);
            scanlineImage.color = new Color(0f, 0.90f, 1f, 0.08f); // Soft, subtle scanlines
            scanlineImage.raycastTarget = false;
        }

        // 2. Full-Width VHS Tape Tracking Noise Bar (Soft translucent band)
        Transform existingTrack = transform.Find("VHS_Tracking_Bar");
        if (existingTrack == null)
        {
            GameObject trackObj = new GameObject("VHS_Tracking_Bar");
            trackObj.transform.SetParent(transform, false);
            trackObj.transform.SetSiblingIndex(1);

            trackingBarRT = trackObj.AddComponent<RectTransform>();
            trackingBarRT.anchorMin = new Vector2(0f, 0.5f);
            trackingBarRT.anchorMax = new Vector2(1f, 0.5f);
            trackingBarRT.offsetMin = new Vector2(0f, -25f);
            trackingBarRT.offsetMax = new Vector2(0f, 25f);

            trackingBarImage = trackObj.AddComponent<RawImage>();
            trackingBarImage.texture = noiseTexture;
            trackingBarImage.uvRect = new Rect(0, 0, 12, 1);
            trackingBarImage.color = new Color(1f, 1f, 1f, 0.12f); // Soft noise
            trackingBarImage.raycastTarget = false;
        }

        // 3. Delicate RGB Chromatic Micro-Strips
        for (int i = 0; i < 5; i++)
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
            stripRT.offsetMin = new Vector2(0f, -8f);
            stripRT.offsetMax = new Vector2(0f, 8f);

            Image stripImg = stripObj.AddComponent<Image>();
            stripImg.color = Color.clear;
            stripImg.raycastTarget = false;

            glitchStrips.Add(stripRT);
            glitchStripImages.Add(stripImg);
        }

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
            bool isStripe = (y % 4) == 0;
            Color col = isStripe ? new Color(0f, 0f, 0f, 0.25f) : new Color(0f, 0.85f, 1f, 0.05f);
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
            float val = Random.value > 0.45f ? Random.Range(0.4f, 0.8f) : 0f;
            pixels[i] = new Color(val, val, val, val * 0.25f);
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
        isRewindingRush = true;
        trackingBarY = 350f;

        if (rewindBannerText != null)
        {
            rewindBannerText.text = "<< TIME REWIND <<";
            rewindBannerText.color = new Color(0f, 0.95f, 1f, 1f);
        }

        if (scanlineImage != null) scanlineImage.color = new Color(0f, 0.90f, 1f, 0.08f);
        if (trackingBarImage != null) trackingBarImage.color = new Color(1f, 1f, 1f, 0.12f);

        if (chromaticAberration != null) chromaticAberration.intensity.value = 0.45f;
        if (lensDistortion != null) lensDistortion.intensity.value = -0.12f;
    }

    private void HandleReadyToResume()
    {
        isOverlayActive = true;
        isRewindingRush = false; // Fast clear glitch elements immediately!

        ClearGlitchElements();

        if (rewindBannerText != null)
        {
            rewindBannerText.text = ">> PRESS WASD OR SPACE TO RESUME <<";
            rewindBannerText.color = Color.white;
        }

        if (chromaticAberration != null) chromaticAberration.intensity.value = 0f;
        if (lensDistortion != null) lensDistortion.intensity.value = 0f;
    }

    private void HandleRewindComplete()
    {
        isOverlayActive = false;
        isRewindingRush = false;
        ClearGlitchElements();
        if (chromaticAberration != null) chromaticAberration.intensity.value = 0f;
        if (lensDistortion != null) lensDistortion.intensity.value = 0f;
    }

    private void ClearGlitchElements()
    {
        for (int i = 0; i < glitchStripImages.Count; i++)
        {
            if (glitchStripImages[i] != null) glitchStripImages[i].color = Color.clear;
        }
        if (scanlineImage != null) scanlineImage.color = Color.clear;
        if (trackingBarImage != null) trackingBarImage.color = Color.clear;
    }

    public void HideImmediate()
    {
        isOverlayActive = false;
        isRewindingRush = false;
        ClearGlitchElements();

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

        if (!isOverlayActive || !isRewindingRush) return;

        // 1. Roll Scanlines softly
        if (scanlineImage != null)
        {
            Rect uv = scanlineImage.uvRect;
            uv.y -= Time.unscaledDeltaTime * 10f;
            scanlineImage.uvRect = uv;
        }

        // 2. Move Soft Tracking Bar
        trackingBarY -= Time.unscaledDeltaTime * 320f;
        if (trackingBarY < -450f) trackingBarY = 450f;

        if (trackingBarRT != null)
        {
            trackingBarRT.offsetMin = new Vector2(0f, trackingBarY - 25f);
            trackingBarRT.offsetMax = new Vector2(0f, trackingBarY + 25f);
        }

        // 3. Subtle RGB Micro-Strips (Light, delicate 0.12-0.22 alpha)
        for (int i = 0; i < glitchStrips.Count; i++)
        {
            if (glitchStrips[i] != null && glitchStripImages[i] != null)
            {
                if (Random.value < 0.25f)
                {
                    float y = Random.Range(-380f, 380f);
                    float h = Random.Range(4f, 14f); // Thin delicate lines
                    float xJitter = Random.Range(-15f, 15f);

                    glitchStrips[i].offsetMin = new Vector2(xJitter, y - h * 0.5f);
                    glitchStrips[i].offsetMax = new Vector2(xJitter, y + h * 0.5f);

                    Color col = (i % 2 == 0)
                        ? new Color(1f, 0.05f, 0.40f, Random.Range(0.10f, 0.22f))  // Subtle Red
                        : new Color(0f, 0.95f, 1f, Random.Range(0.10f, 0.22f)); // Subtle Cyan

                    glitchStripImages[i].color = col;
                }
            }
        }
    }
}
