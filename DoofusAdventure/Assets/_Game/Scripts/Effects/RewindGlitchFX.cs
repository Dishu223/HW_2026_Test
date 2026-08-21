using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// Retro VHS Tape Rewind & RGB Glitch Screen FX Engine:
/// - Dense CRT scanlines spanning 100% full viewport
/// - Full-width RGB Split & Chromatic Glitch Strips extending edge-to-edge on any aspect ratio
/// - Full-width VHS Tape Tracking Noise Bar sweeping down the entire display
/// - Retro Top-Left VCR OSD: "<< REWIND -16X"
/// - Lens distortion & Chromatic Aberration via URP Volume
/// - Touchdown Flash Burst when resuming gameplay
/// </summary>
public class RewindGlitchFX : MonoBehaviour
{
    private Volume volume;
    private ChromaticAberration chromaticAberration;
    private LensDistortion lensDistortion;
    private Vignette vignette;

    private Canvas glitchCanvas;
    private RawImage scanlineImage;
    private Image flashOverlay;
    private RectTransform trackingBar;
    private RawImage trackingBarImage;
    private TextMeshProUGUI vcrText;
    private List<RectTransform> glitchStrips = new List<RectTransform>();
    private List<Image> glitchStripImages = new List<Image>();

    private Texture2D scanlineTexture;
    private Texture2D noiseTexture;

    private bool isRewinding = false;
    private float trackingBarY = 0f;
    private Coroutine glitchRoutine;

    private void Awake()
    {
        SetupURPVolumeOverrides();
        BuildGlitchCanvas();
    }

    private void OnEnable()
    {
        GameEvents.OnRewindStart += HandleRewindStart;
        GameEvents.OnRewindReadyToResume += HandleRewindReadyToResume;
        GameEvents.OnRewindComplete += HandleRewindComplete;
    }

    private void OnDisable()
    {
        GameEvents.OnRewindStart -= HandleRewindStart;
        GameEvents.OnRewindReadyToResume -= HandleRewindReadyToResume;
        GameEvents.OnRewindComplete -= HandleRewindComplete;
    }

    private void SetupURPVolumeOverrides()
    {
        volume = FindAnyObjectByType<Volume>();
        if (volume == null)
        {
            GameObject volObj = new GameObject("Rewind_PostProcess_Volume");
            volume = volObj.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10;
        }

        VolumeProfile profile = volume.profile != null ? volume.profile : volume.sharedProfile;
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.profile = profile;
        }

        if (!profile.TryGet(out chromaticAberration))
        {
            chromaticAberration = profile.Add<ChromaticAberration>(true);
        }
        chromaticAberration.intensity.overrideState = true;
        chromaticAberration.intensity.value = 0f;

        if (!profile.TryGet(out lensDistortion))
        {
            lensDistortion = profile.Add<LensDistortion>(true);
        }
        lensDistortion.intensity.overrideState = true;
        lensDistortion.intensity.value = 0f;

        if (!profile.TryGet(out vignette))
        {
            vignette = profile.Add<Vignette>(true);
        }
        vignette.intensity.overrideState = true;
        vignette.intensity.value = 0.25f;
    }

    private void BuildGlitchCanvas()
    {
        GameObject canvasObj = new GameObject("Rewind_Glitch_Canvas");
        canvasObj.transform.SetParent(transform);
        glitchCanvas = canvasObj.AddComponent<Canvas>();
        glitchCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        glitchCanvas.sortingOrder = 998;

        canvasObj.AddComponent<GraphicRaycaster>();

        scanlineTexture = GenerateScanlineTexture(64, 64);
        noiseTexture = GenerateNoiseTexture(128, 128);

        // 1. Edge-to-Edge CRT Scanlines
        GameObject scanlineObj = new GameObject("Scanline_Overlay");
        scanlineObj.transform.SetParent(canvasObj.transform, false);
        RectTransform scanRT = scanlineObj.AddComponent<RectTransform>();
        scanRT.anchorMin = Vector2.zero;
        scanRT.anchorMax = Vector2.one;
        scanRT.offsetMin = new Vector2(-100f, -100f);
        scanRT.offsetMax = new Vector2(100f, 100f);

        scanlineImage = scanlineObj.AddComponent<RawImage>();
        scanlineImage.texture = scanlineTexture;
        scanlineImage.uvRect = new Rect(0, 0, 1, 40);
        scanlineImage.color = Color.clear;
        scanlineImage.raycastTarget = false;

        // 2. Full-Width Edge-to-Edge VHS Tracking Noise Bar (Anchor Left=0 to Right=1)
        GameObject trackObj = new GameObject("VHS_Tracking_Bar");
        trackObj.transform.SetParent(canvasObj.transform, false);
        trackingBar = trackObj.AddComponent<RectTransform>();
        trackingBar.anchorMin = new Vector2(0f, 0.5f);
        trackingBar.anchorMax = new Vector2(1f, 0.5f);
        trackingBar.offsetMin = new Vector2(-200f, -45f);
        trackingBar.offsetMax = new Vector2(200f, 45f);

        trackingBarImage = trackObj.AddComponent<RawImage>();
        trackingBarImage.texture = noiseTexture;
        trackingBarImage.uvRect = new Rect(0, 0, 8, 1);
        trackingBarImage.color = Color.clear;
        trackingBarImage.raycastTarget = false;

        // 3. Dynamic Full-Width RGB Chromatic Tear Strips (Anchor Left=0 to Right=1)
        for (int i = 0; i < 8; i++)
        {
            GameObject stripObj = new GameObject($"RGB_Glitch_Strip_{i}");
            stripObj.transform.SetParent(canvasObj.transform, false);

            RectTransform stripRT = stripObj.AddComponent<RectTransform>();
            stripRT.anchorMin = new Vector2(0f, 0.5f);
            stripRT.anchorMax = new Vector2(1f, 0.5f);
            stripRT.offsetMin = new Vector2(-200f, -20f);
            stripRT.offsetMax = new Vector2(200f, 20f);

            Image stripImg = stripObj.AddComponent<Image>();
            stripImg.color = Color.clear;
            stripImg.raycastTarget = false;

            glitchStrips.Add(stripRT);
            glitchStripImages.Add(stripImg);
        }

        // 4. Retro VCR Top-Left OSD (<< REWIND -16X)
        GameObject osdObj = new GameObject("VCR_OSD_Text");
        osdObj.transform.SetParent(canvasObj.transform, false);
        RectTransform osdRT = osdObj.AddComponent<RectTransform>();
        osdRT.anchorMin = new Vector2(0f, 1f);
        osdRT.anchorMax = new Vector2(0f, 1f);
        osdRT.pivot = new Vector2(0f, 1f);
        osdRT.anchoredPosition = new Vector2(40f, -40f);
        osdRT.sizeDelta = new Vector2(400f, 50f);

        vcrText = osdObj.AddComponent<TextMeshProUGUI>();
        vcrText.text = "<b><< REWIND  -16X</b>";
        vcrText.fontSize = 28f;
        vcrText.color = Color.clear;
        vcrText.raycastTarget = false;

        // 5. Full-Screen Flash Overlay
        GameObject flashObj = new GameObject("Flash_Overlay");
        flashObj.transform.SetParent(canvasObj.transform, false);
        RectTransform flashRT = flashObj.AddComponent<RectTransform>();
        flashRT.anchorMin = Vector2.zero;
        flashRT.anchorMax = Vector2.one;
        flashRT.offsetMin = new Vector2(-100f, -100f);
        flashRT.offsetMax = new Vector2(100f, 100f);

        flashOverlay = flashObj.AddComponent<Image>();
        flashOverlay.color = Color.clear;
        flashOverlay.raycastTarget = false;
    }

    private Texture2D GenerateScanlineTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Point;

        Color[] pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            bool isDarkStripe = (y % 3) == 0;
            Color col = isDarkStripe ? new Color(0.0f, 0.05f, 0.15f, 0.65f) : new Color(0f, 0f, 0f, 0f);
            for (int x = 0; x < width; x++)
            {
                pixels[y * width + x] = col;
            }
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
            pixels[i] = new Color(val, val, val, val * 0.55f);
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private void Update()
    {
        if (!isRewinding) return;

        // 1. Roll CRT Scanlines vertically
        if (scanlineImage != null)
        {
            Rect uv = scanlineImage.uvRect;
            uv.y -= Time.unscaledDeltaTime * 15f;
            scanlineImage.uvRect = uv;
        }

        // 2. Move VHS Tracking Noise Bar downwards across the entire screen
        float screenHalfH = Screen.height * 0.5f;
        trackingBarY -= Time.unscaledDeltaTime * 450f;
        if (trackingBarY < -screenHalfH - 60f) trackingBarY = screenHalfH + 60f;

        if (trackingBar != null)
        {
            trackingBar.offsetMin = new Vector2(-200f, trackingBarY - 45f);
            trackingBar.offsetMax = new Vector2(200f, trackingBarY + 45f);
        }

        // 3. Jitter RGB Glitch Strips across the ENTIRE full screen height (Vivid Red & Electric Cyan)
        for (int i = 0; i < glitchStrips.Count; i++)
        {
            if (glitchStrips[i] != null && glitchStripImages[i] != null)
            {
                if (Random.value < 0.35f)
                {
                    float y = Random.Range(-screenHalfH * 0.9f, screenHalfH * 0.9f);
                    float h = Random.Range(18f, 65f);
                    float xJitter = Random.Range(-40f, 40f);

                    glitchStrips[i].offsetMin = new Vector2(-200f + xJitter, y - h * 0.5f);
                    glitchStrips[i].offsetMax = new Vector2(200f + xJitter, y + h * 0.5f);

                    Color glitchCol = (i % 2 == 0)
                        ? new Color(1f, 0f, 0.35f, Random.Range(0.35f, 0.75f))  // Vivid Red
                        : new Color(0f, 0.95f, 1f, Random.Range(0.35f, 0.75f)); // Electric Cyan

                    glitchStripImages[i].color = glitchCol;
                }
            }
        }

        // 4. VCR OSD Blink
        if (vcrText != null)
        {
            float alpha = Mathf.PingPong(Time.unscaledTime * 4f, 1f);
            vcrText.color = new Color(0f, 1f, 0.75f, alpha > 0.2f ? 1f : 0f);
        }
    }

    private void HandleRewindStart()
    {
        isRewinding = true;
        trackingBarY = Screen.height * 0.5f;
        if (glitchRoutine != null) StopCoroutine(glitchRoutine);
        glitchRoutine = StartCoroutine(RewindGlitchInCoroutine());
    }

    private void HandleRewindReadyToResume()
    {
        if (chromaticAberration != null) chromaticAberration.intensity.value = 0.55f;
        if (lensDistortion != null) lensDistortion.intensity.value = -0.20f;
    }

    private void HandleRewindComplete()
    {
        isRewinding = false;
        if (glitchRoutine != null) StopCoroutine(glitchRoutine);
        glitchRoutine = StartCoroutine(RewindGlitchOutCoroutine());
    }

    private IEnumerator RewindGlitchInCoroutine()
    {
        float elapsed = 0f;
        float duration = 0.20f;

        if (scanlineImage != null) scanlineImage.color = new Color(0.9f, 0.95f, 1f, 0.85f);
        if (trackingBarImage != null) trackingBarImage.color = new Color(1f, 1f, 1f, 0.75f);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;

            if (chromaticAberration != null) chromaticAberration.intensity.value = Mathf.Lerp(0f, 1.0f, t);
            if (lensDistortion != null) lensDistortion.intensity.value = Mathf.Lerp(0f, -0.40f, t);
            if (vignette != null) vignette.intensity.value = Mathf.Lerp(0.25f, 0.60f, t);
            if (flashOverlay != null) flashOverlay.color = new Color(0f, 0.95f, 1f, Mathf.Lerp(0.55f, 0f, t));

            yield return null;
        }

        if (chromaticAberration != null) chromaticAberration.intensity.value = 1.0f;
        if (lensDistortion != null) lensDistortion.intensity.value = -0.40f;
    }

    private IEnumerator RewindGlitchOutCoroutine()
    {
        float elapsed = 0f;
        float duration = 0.35f;

        for (int i = 0; i < glitchStripImages.Count; i++)
        {
            if (glitchStripImages[i] != null) glitchStripImages[i].color = Color.clear;
        }
        if (vcrText != null) vcrText.color = Color.clear;
        if (trackingBarImage != null) trackingBarImage.color = Color.clear;

        if (flashOverlay != null) flashOverlay.color = new Color(1f, 1f, 1f, 0.85f);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;

            if (chromaticAberration != null) chromaticAberration.intensity.value = Mathf.Lerp(1.0f, 0f, t);
            if (lensDistortion != null) lensDistortion.intensity.value = Mathf.Lerp(-0.40f, 0f, t);
            if (vignette != null) vignette.intensity.value = Mathf.Lerp(0.60f, 0.25f, t);

            if (scanlineImage != null) scanlineImage.color = new Color(0.9f, 0.95f, 1f, Mathf.Lerp(0.85f, 0f, t));
            if (flashOverlay != null) flashOverlay.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.85f, 0f, t));

            yield return null;
        }

        if (chromaticAberration != null) chromaticAberration.intensity.value = 0f;
        if (lensDistortion != null) lensDistortion.intensity.value = 0f;
        if (vignette != null) vignette.intensity.value = 0.25f;
        if (scanlineImage != null) scanlineImage.color = Color.clear;
        if (flashOverlay != null) flashOverlay.color = Color.clear;
    }
}
