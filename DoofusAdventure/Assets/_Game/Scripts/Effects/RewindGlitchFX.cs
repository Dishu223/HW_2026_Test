using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// Retro VHS Tape Rewind & RGB Glitch Screen FX Engine:
/// - Dense CRT scanlines covering the entire viewport
/// - Full-width RGB Split / Horizontal Glitch Tear Slices (Red / Cyan chromatic tearing across full screen)
/// - VCR Tracking Noise Bar rolling down the center of the screen
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
    private TextMeshProUGUI vcrText;
    private List<RectTransform> glitchStrips = new List<RectTransform>();
    private List<Image> glitchStripImages = new List<Image>();

    private Texture2D scanlineTexture;
    private Texture2D noiseTexture;

    private bool isRewinding = false;
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

        CanvasScaler cs = canvasObj.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        scanlineTexture = GenerateScanlineTexture(64, 64);
        noiseTexture = GenerateNoiseTexture(128, 128);

        // 1. Full-Screen CRT Scanlines
        GameObject scanlineObj = new GameObject("Scanline_Overlay");
        scanlineObj.transform.SetParent(canvasObj.transform, false);
        RectTransform scanRT = scanlineObj.AddComponent<RectTransform>();
        scanRT.anchorMin = Vector2.zero;
        scanRT.anchorMax = Vector2.one;
        scanRT.offsetMin = Vector2.zero;
        scanRT.offsetMax = Vector2.zero;

        scanlineImage = scanlineObj.AddComponent<RawImage>();
        scanlineImage.texture = scanlineTexture;
        scanlineImage.uvRect = new Rect(0, 0, 1, 40);
        scanlineImage.color = Color.clear;
        scanlineImage.raycastTarget = false;

        // 2. Full-Width VHS Tape Tracking Noise Bar (2600px wide across full screen!)
        GameObject trackObj = new GameObject("VHS_Tracking_Bar");
        trackObj.transform.SetParent(canvasObj.transform, false);
        trackingBar = trackObj.AddComponent<RectTransform>();
        trackingBar.anchorMin = new Vector2(0.5f, 0.5f);
        trackingBar.anchorMax = new Vector2(0.5f, 0.5f);
        trackingBar.pivot = new Vector2(0.5f, 0.5f);
        trackingBar.sizeDelta = new Vector2(2600f, 90f);
        trackingBar.anchoredPosition = new Vector2(0f, 0f);

        RawImage trackImg = trackObj.AddComponent<RawImage>();
        trackImg.texture = noiseTexture;
        trackImg.color = Color.clear;
        trackImg.raycastTarget = false;

        // 3. Dynamic Full-Width RGB Chromatic Tear Strips (2600px wide)
        for (int i = 0; i < 6; i++)
        {
            GameObject stripObj = new GameObject($"RGB_Glitch_Strip_{i}");
            stripObj.transform.SetParent(canvasObj.transform, false);

            RectTransform stripRT = stripObj.AddComponent<RectTransform>();
            stripRT.anchorMin = new Vector2(0.5f, 0.5f);
            stripRT.anchorMax = new Vector2(0.5f, 0.5f);
            stripRT.pivot = new Vector2(0.5f, 0.5f);
            stripRT.sizeDelta = new Vector2(2600f, Random.Range(20f, 60f));
            stripRT.anchoredPosition = new Vector2(0f, Random.Range(-400f, 400f));

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
        flashRT.offsetMin = Vector2.zero;
        flashRT.offsetMax = Vector2.zero;

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
            float val = Random.value > 0.40f ? Random.Range(0.6f, 1f) : 0f;
            pixels[i] = new Color(val, val, val, val * 0.45f);
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

        // 2. Move VHS Tracking Noise Bar downwards across the full screen
        if (trackingBar != null)
        {
            float barY = trackingBar.anchoredPosition.y - Time.unscaledDeltaTime * 420f;
            if (barY < -580f) barY = 580f;
            trackingBar.anchoredPosition = new Vector2(0f, barY);
        }

        // 3. Jitter RGB Glitch Strips across full screen (Vivid Red / Electric Cyan)
        for (int i = 0; i < glitchStrips.Count; i++)
        {
            if (glitchStrips[i] != null && glitchStripImages[i] != null)
            {
                if (Random.value < 0.30f)
                {
                    glitchStrips[i].anchoredPosition = new Vector2(Random.Range(-40f, 40f), Random.Range(-480f, 480f));
                    glitchStrips[i].sizeDelta = new Vector2(2600f, Random.Range(15f, 65f));

                    Color glitchCol = (i % 2 == 0)
                        ? new Color(1f, 0f, 0.35f, Random.Range(0.35f, 0.75f))  // Vivid Red Chromatic Split
                        : new Color(0f, 0.95f, 1f, Random.Range(0.35f, 0.75f)); // Electric Cyan Chromatic Split

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
        if (trackingBar != null)
        {
            RawImage ri = trackingBar.GetComponent<RawImage>();
            if (ri != null) ri.color = new Color(1f, 1f, 1f, 0.75f);
        }

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
        if (trackingBar != null)
        {
            RawImage ri = trackingBar.GetComponent<RawImage>();
            if (ri != null) ri.color = Color.clear;
        }

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
