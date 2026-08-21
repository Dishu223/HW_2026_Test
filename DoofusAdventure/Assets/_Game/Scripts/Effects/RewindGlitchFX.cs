using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// Retro Rewind Glitch & Chromatic Aberration Screen FX:
/// - Ramps Chromatic Aberration & Lens Distortion on the URP Volume during Time Rewind
/// - Displays full-screen VHS scanlines and static glitch pulse
/// - Shakes viewport subtly during rewind acceleration
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
    private Texture2D scanlineTexture;

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
        glitchCanvas.sortingOrder = 999;

        CanvasScaler cs = canvasObj.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        // Scanline Texture (Horizontal VHS lines)
        scanlineTexture = GenerateScanlineTexture(64, 64);

        GameObject scanlineObj = new GameObject("Scanline_Overlay");
        scanlineObj.transform.SetParent(canvasObj.transform, false);
        RectTransform scanRT = scanlineObj.AddComponent<RectTransform>();
        scanRT.anchorMin = Vector2.zero;
        scanRT.anchorMax = Vector2.one;
        scanRT.sizeDelta = Vector2.zero;

        scanlineImage = scanlineObj.AddComponent<RawImage>();
        scanlineImage.texture = scanlineTexture;
        scanlineImage.uvRect = new Rect(0, 0, 1, 30);
        scanlineImage.color = new Color(0f, 0.95f, 1f, 0f); // Cyan scanlines, hidden initially
        scanlineImage.raycastTarget = false;

        // Flash Overlay
        GameObject flashObj = new GameObject("Flash_Overlay");
        flashObj.transform.SetParent(canvasObj.transform, false);
        RectTransform flashRT = flashObj.AddComponent<RectTransform>();
        flashRT.anchorMin = Vector2.zero;
        flashRT.anchorMax = Vector2.one;
        flashRT.sizeDelta = Vector2.zero;

        flashOverlay = flashObj.AddComponent<Image>();
        flashOverlay.color = new Color(0f, 0.85f, 1f, 0f);
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
            bool isStripe = (y % 4) == 0;
            Color col = isStripe ? new Color(0.1f, 0.2f, 0.35f, 0.45f) : new Color(0f, 0f, 0f, 0f);
            for (int x = 0; x < width; x++)
            {
                pixels[y * width + x] = col;
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private void Update()
    {
        if (isRewinding && scanlineImage != null)
        {
            // Rapid vertical scanline roll
            Rect uv = scanlineImage.uvRect;
            uv.y -= Time.unscaledDeltaTime * 12f;
            scanlineImage.uvRect = uv;
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
        if (chromaticAberration != null) chromaticAberration.intensity.value = 0.45f;
        if (lensDistortion != null) lensDistortion.intensity.value = -0.15f;
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
        float duration = 0.25f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;

            if (chromaticAberration != null) chromaticAberration.intensity.value = Mathf.Lerp(0f, 1.0f, t);
            if (lensDistortion != null) lensDistortion.intensity.value = Mathf.Lerp(0f, -0.35f, t);
            if (vignette != null) vignette.intensity.value = Mathf.Lerp(0.25f, 0.55f, t);

            if (scanlineImage != null) scanlineImage.color = new Color(0f, 0.95f, 1f, Mathf.Lerp(0f, 0.35f, t));
            if (flashOverlay != null) flashOverlay.color = new Color(0f, 0.85f, 1f, Mathf.Lerp(0.40f, 0f, t));

            yield return null;
        }

        if (chromaticAberration != null) chromaticAberration.intensity.value = 1.0f;
        if (lensDistortion != null) lensDistortion.intensity.value = -0.35f;
    }

    private IEnumerator RewindGlitchOutCoroutine()
    {
        float elapsed = 0f;
        float duration = 0.30f;

        float startCA = chromaticAberration != null ? chromaticAberration.intensity.value : 0f;
        float startLD = lensDistortion != null ? lensDistortion.intensity.value : 0f;
        float startVig = vignette != null ? vignette.intensity.value : 0.25f;

        if (flashOverlay != null) flashOverlay.color = new Color(0f, 1f, 0.8f, 0.50f);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;

            if (chromaticAberration != null) chromaticAberration.intensity.value = Mathf.Lerp(startCA, 0f, t);
            if (lensDistortion != null) lensDistortion.intensity.value = Mathf.Lerp(startLD, 0f, t);
            if (vignette != null) vignette.intensity.value = Mathf.Lerp(startVig, 0.25f, t);

            if (scanlineImage != null) scanlineImage.color = new Color(0f, 0.95f, 1f, Mathf.Lerp(0.35f, 0f, t));
            if (flashOverlay != null) flashOverlay.color = new Color(0f, 1f, 0.8f, Mathf.Lerp(0.50f, 0f, t));

            yield return null;
        }

        if (chromaticAberration != null) chromaticAberration.intensity.value = 0f;
        if (lensDistortion != null) lensDistortion.intensity.value = 0f;
        if (vignette != null) vignette.intensity.value = 0.25f;
        if (scanlineImage != null) scanlineImage.color = Color.clear;
        if (flashOverlay != null) flashOverlay.color = Color.clear;
    }
}
