using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// True Interactive Circular Color Wheel Picker:
/// - Guarantees strict 1:1 circular aspect ratio with zero oval stretching
/// - Full 360-degree interactive dragging to the outer circle rim
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class ColorWheelPicker : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    public System.Action<Color> OnColorChanged;

    private Image wheelImage;
    private RectTransform wheelRect;
    private RectTransform cursorRect;
    private Slider brightnessSlider;
    private Image previewBox;

    private float currentHue = 0f;
    private float currentSaturation = 0f;
    private float currentBrightness = 1f;
    private float wheelRadius = 75f;

    public Color CurrentColor => Color.HSVToRGB(currentHue, currentSaturation, currentBrightness);

    public void Initialize(float size = 150f)
    {
        wheelRect = GetComponent<RectTransform>();
        wheelRect.sizeDelta = new Vector2(size, size);
        wheelRadius = size * 0.5f;

        // Force strict 1:1 layout sizing
        LayoutElement le = GetComponent<LayoutElement>();
        if (le == null) le = gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = size;
        le.preferredHeight = size;
        le.minWidth = size;
        le.minHeight = size;
        le.flexibleWidth = 0;
        le.flexibleHeight = 0;

        wheelImage = GetComponent<Image>();
        if (wheelImage == null) wheelImage = gameObject.AddComponent<Image>();

        Texture2D wheelTex = GenerateColorWheelTexture(256);
        Sprite wheelSprite = Sprite.Create(wheelTex, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f));
        wheelImage.sprite = wheelSprite;
        wheelImage.preserveAspect = true;
        wheelImage.raycastTarget = true;

        // Cursor Indicator
        GameObject cursorObj = new GameObject("Wheel_Cursor");
        cursorObj.transform.SetParent(transform, false);
        cursorRect = cursorObj.AddComponent<RectTransform>();
        cursorRect.sizeDelta = new Vector2(16f, 16f);
        cursorRect.anchoredPosition = Vector2.zero;

        Image cursorOutline = cursorObj.AddComponent<Image>();
        cursorOutline.color = Color.white;
        cursorOutline.raycastTarget = false;

        GameObject innerDot = new GameObject("Cursor_Inner");
        innerDot.transform.SetParent(cursorObj.transform, false);
        RectTransform innerRT = innerDot.AddComponent<RectTransform>();
        innerRT.anchorMin = Vector2.zero;
        innerRT.anchorMax = Vector2.one;
        innerRT.sizeDelta = new Vector2(-4f, -4f);

        Image innerImg = innerDot.AddComponent<Image>();
        innerImg.color = Color.black;
        innerImg.raycastTarget = false;
    }

    public void SetBrightnessSlider(Slider slider)
    {
        brightnessSlider = slider;
        if (brightnessSlider != null)
        {
            brightnessSlider.onValueChanged.AddListener((val) => {
                currentBrightness = val;
                NotifyColorChanged();
            });
        }
    }

    public void SetPreviewBox(Image preview)
    {
        previewBox = preview;
    }

    public void SetColor(Color color)
    {
        Color.RGBToHSV(color, out currentHue, out currentSaturation, out currentBrightness);
        if (brightnessSlider != null) brightnessSlider.value = currentBrightness;

        UpdateCursorPosition();
        UpdatePreviewBox();
    }

    public void OnPointerDown(PointerEventData eventData) => HandleWheelInput(eventData);
    public void OnDrag(PointerEventData eventData) => HandleWheelInput(eventData);

    private void HandleWheelInput(PointerEventData eventData)
    {
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(wheelRect, eventData.position, eventData.pressEventCamera, out localPoint))
        {
            float dist = localPoint.magnitude;
            float sat = Mathf.Clamp01(dist / wheelRadius);

            float angle = Mathf.Atan2(localPoint.y, localPoint.x) * Mathf.Rad2Deg;
            if (angle < 0f) angle += 360f;
            float hue = angle / 360f;

            currentHue = hue;
            currentSaturation = sat;

            Vector2 clampedPos = localPoint;
            if (dist > wheelRadius) clampedPos = localPoint.normalized * wheelRadius;
            if (cursorRect != null) cursorRect.anchoredPosition = clampedPos;

            NotifyColorChanged();
        }
    }

    private void UpdateCursorPosition()
    {
        if (cursorRect == null) return;

        float angle = currentHue * 360f * Mathf.Deg2Rad;
        float dist = currentSaturation * wheelRadius;
        cursorRect.anchoredPosition = new Vector2(Mathf.Cos(angle) * dist, Mathf.Sin(angle) * dist);
    }

    private void NotifyColorChanged()
    {
        Color c = CurrentColor;
        UpdatePreviewBox();
        OnColorChanged?.Invoke(c);
    }

    private void UpdatePreviewBox()
    {
        if (previewBox != null) previewBox.color = CurrentColor;
    }

    private Texture2D GenerateColorWheelTexture(int resolution)
    {
        Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float center = resolution * 0.5f;
        float radius = center - 2f;

        Color[] pixels = new Color[resolution * resolution];

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist <= radius)
                {
                    float sat = dist / radius;
                    float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                    if (angle < 0f) angle += 360f;
                    float hue = angle / 360f;

                    float alpha = 1f;
                    if (dist > radius - 1.5f)
                    {
                        alpha = Mathf.Clamp01((radius - dist) / 1.5f);
                    }

                    Color c = Color.HSVToRGB(hue, sat, 1f);
                    c.a = alpha;
                    pixels[y * resolution + x] = c;
                }
                else
                {
                    pixels[y * resolution + x] = Color.clear;
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }
}
