using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Combined Start Screen & Character Customization Lobby UI:
/// - True Circular HSV Color Wheel (strict 1:1 aspect ratio)
/// - Target Selector (BODY / HEAD / EYES) with perfect click isolation
/// - Clean standard ASCII typography (no missing glyph boxes)
/// - Real-time 3D Doofus Turntable Rotation Preview
/// </summary>
public class StartScreenUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI pressSpacePrompt;

    private float initialTitleY;
    private GameObject customPanelObj;
    private DoofusController doofusPreview;
    private CustomizationManager.CustomPart selectedPart = CustomizationManager.CustomPart.Body;

    private Image bodyTabBg;
    private Image headTabBg;
    private Image eyeTabBg;
    private ColorWheelPicker colorWheel;
    private Image liveColorPreviewBox;

    private void Awake()
    {
        Time.timeScale = 0f;
    }

    private void Start()
    {
        Time.timeScale = 0f;
        doofusPreview = FindAnyObjectByType<DoofusController>();

        if (titleText != null)
        {
            initialTitleY = titleText.rectTransform.anchoredPosition.y;
            titleText.text = "DOOFUS ADVENTURE";
        }

        if (pressSpacePrompt != null)
        {
            pressSpacePrompt.text = ">> PRESS SPACE TO START <<";
        }

        BuildCustomizationLobbyPanel();
        RefreshActiveTabVisuals();
    }

    private void Update()
    {
        if (doofusPreview != null)
        {
            doofusPreview.transform.Rotate(Vector3.up, 35f * Time.unscaledDeltaTime, Space.World);
        }

        if (titleText != null)
        {
            float newY = initialTitleY + Mathf.Sin(Time.unscaledTime * 3f) * 8f;
            titleText.rectTransform.anchoredPosition = new Vector2(titleText.rectTransform.anchoredPosition.x, newY);
        }

        if (pressSpacePrompt != null)
        {
            float alpha = 0.55f + Mathf.PingPong(Time.unscaledTime * 2f, 0.45f);
            pressSpacePrompt.alpha = alpha;
        }

        bool spacePressed = Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame);
        if (spacePressed)
        {
            LaunchGame();
        }
    }

    private void BuildCustomizationLobbyPanel()
    {
        if (CustomizationManager.Instance == null) return;

        customPanelObj = new GameObject("Customization_Card");
        customPanelObj.transform.SetParent(transform, false);

        RectTransform panelRT = customPanelObj.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(1f, 0.5f);
        panelRT.anchorMax = new Vector2(1f, 0.5f);
        panelRT.pivot = new Vector2(1f, 0.5f);
        panelRT.anchoredPosition = new Vector2(-35f, 0f);
        panelRT.sizeDelta = new Vector2(360f, 520f);

        Image bgImage = customPanelObj.AddComponent<Image>();
        bgImage.color = new Color(0.04f, 0.07f, 0.12f, 0.94f);

        VerticalLayoutGroup vlg = customPanelObj.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(16, 16, 16, 16);
        vlg.spacing = 10f;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;

        // 1. Header Title
        CreateHeaderLabel(customPanelObj.transform, "CHARACTER STUDIO", 18f, FontStyles.Bold, new Color(0f, 0.90f, 1f));

        // 2. Part Selector Tabs [ BODY ] [ HEAD ] [ EYES ]
        Transform tabsRow = CreateRowContainer(customPanelObj.transform, 34f, 6f);
        CreateTabButton(tabsRow, "BODY", () => SwitchTab(CustomizationManager.CustomPart.Body), out bodyTabBg);
        CreateTabButton(tabsRow, "HEAD", () => SwitchTab(CustomizationManager.CustomPart.Head), out headTabBg);
        CreateTabButton(tabsRow, "EYES", () => SwitchTab(CustomizationManager.CustomPart.Eyes), out eyeTabBg);

        // 3. Circular Color Wheel Container (Fixed 150x150 square row to prevent oval distortion!)
        Transform wheelContainer = CreateRowContainer(customPanelObj.transform, 155f, 0f);
        GameObject wheelObj = new GameObject("Color_Wheel_Object");
        wheelObj.transform.SetParent(wheelContainer, false);
        colorWheel = wheelObj.AddComponent<ColorWheelPicker>();
        colorWheel.Initialize(150f);
        colorWheel.OnColorChanged = (color) => {
            CustomizationManager.Instance.SetCustomPartColor(selectedPart, color);
        };

        // 4. Brightness Slider Row + Live Preview Box
        Transform sliderRow = CreateRowContainer(customPanelObj.transform, 28f, 8f);

        GameObject prevObj = new GameObject("Color_Preview_Box");
        prevObj.transform.SetParent(sliderRow, false);
        RectTransform prevRT = prevObj.AddComponent<RectTransform>();
        prevRT.sizeDelta = new Vector2(28f, 28f);
        liveColorPreviewBox = prevObj.AddComponent<Image>();
        liveColorPreviewBox.color = Color.white;
        liveColorPreviewBox.raycastTarget = false;
        colorWheel.SetPreviewBox(liveColorPreviewBox);

        Slider brightnessSlider = CreateBrightnessSlider(sliderRow);
        colorWheel.SetBrightnessSlider(brightnessSlider);

        // 5. Quick Color Presets
        CreateHeaderLabel(customPanelObj.transform, "QUICK PALETTE PRESETS", 11f, FontStyles.Bold, new Color(0.70f, 0.78f, 0.88f));
        Transform presetRow = CreateRowContainer(customPanelObj.transform, 30f, 5f);
        for (int i = 0; i < CustomizationManager.Instance.bodyColors.Length; i++)
        {
            int idx = i;
            Color c = CustomizationManager.Instance.bodyColors[i];
            CreatePresetSwatchButton(presetRow, c, () => ApplyPreset(idx, c));
        }

        // 6. Play Button
        GameObject playBtnObj = new GameObject("Play_Button");
        playBtnObj.transform.SetParent(customPanelObj.transform, false);
        RectTransform playBtnRT = playBtnObj.AddComponent<RectTransform>();
        playBtnRT.sizeDelta = new Vector2(328f, 42f);

        Image playBtnImg = playBtnObj.AddComponent<Image>();
        playBtnImg.color = new Color(0f, 0.85f, 0.45f);

        Button playBtn = playBtnObj.AddComponent<Button>();
        playBtn.onClick.AddListener(LaunchGame);

        CreateButtonLabel(playBtnObj.transform, "START RUN  >>", 15f, FontStyles.Bold, Color.black);
    }

    private void CreateTabButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick, out Image tabBg)
    {
        GameObject btnObj = new GameObject("Tab_" + label);
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(100f, 32f);

        tabBg = btnObj.AddComponent<Image>();
        tabBg.color = new Color(0.14f, 0.18f, 0.25f);
        tabBg.raycastTarget = true;

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = tabBg;
        btn.onClick.AddListener(onClick);

        CreateButtonLabel(btnObj.transform, label, 12f, FontStyles.Bold, Color.white);
    }

    private void SwitchTab(CustomizationManager.CustomPart part)
    {
        selectedPart = part;
        RefreshActiveTabVisuals();
    }

    private void RefreshActiveTabVisuals()
    {
        Color activeCol = new Color(0f, 0.80f, 1f, 1f);
        Color inactiveCol = new Color(0.14f, 0.18f, 0.25f, 0.9f);

        if (bodyTabBg != null) bodyTabBg.color = (selectedPart == CustomizationManager.CustomPart.Body) ? activeCol : inactiveCol;
        if (headTabBg != null) headTabBg.color = (selectedPart == CustomizationManager.CustomPart.Head) ? activeCol : inactiveCol;
        if (eyeTabBg != null) eyeTabBg.color = (selectedPart == CustomizationManager.CustomPart.Eyes) ? activeCol : inactiveCol;

        if (colorWheel != null && CustomizationManager.Instance != null)
        {
            Color currentPartColor = selectedPart switch
            {
                CustomizationManager.CustomPart.Body => CustomizationManager.Instance.CurrentBodyColor,
                CustomizationManager.CustomPart.Head => CustomizationManager.Instance.CurrentHeadColor,
                CustomizationManager.CustomPart.Eyes => CustomizationManager.Instance.CurrentEyeColor,
                _ => Color.white
            };
            colorWheel.SetColor(currentPartColor);
        }
    }

    private void ApplyPreset(int idx, Color c)
    {
        if (CustomizationManager.Instance == null) return;

        switch (selectedPart)
        {
            case CustomizationManager.CustomPart.Body:
                CustomizationManager.Instance.SetBodyColor(idx);
                break;
            case CustomizationManager.CustomPart.Head:
                CustomizationManager.Instance.SetHeadColor(idx);
                break;
            case CustomizationManager.CustomPart.Eyes:
                CustomizationManager.Instance.SetEyeColor(idx % CustomizationManager.Instance.eyeColors.Length);
                break;
        }

        if (colorWheel != null) colorWheel.SetColor(c);
    }

    private Slider CreateBrightnessSlider(Transform parent)
    {
        GameObject sliderObj = new GameObject("Brightness_Slider");
        sliderObj.transform.SetParent(parent, false);
        RectTransform rt = sliderObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(240f, 20f);

        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = 0.05f;
        slider.maxValue = 1.0f;
        slider.value = 1.0f;

        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(sliderObj.transform, false);
        RectTransform bgRT = bgObj.AddComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0f, 0.25f);
        bgRT.anchorMax = new Vector2(1f, 0.75f);
        bgRT.sizeDelta = Vector2.zero;
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.25f, 0.35f);

        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRT = fillArea.AddComponent<RectTransform>();
        fillAreaRT.anchorMin = new Vector2(0f, 0.25f);
        fillAreaRT.anchorMax = new Vector2(1f, 0.75f);
        fillAreaRT.sizeDelta = new Vector2(-10f, 0f);

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRT = fill.AddComponent<RectTransform>();
        fillRT.sizeDelta = Vector2.zero;
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0f, 0.85f, 1f);

        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderObj.transform, false);
        RectTransform handleAreaRT = handleArea.AddComponent<RectTransform>();
        handleAreaRT.anchorMin = Vector2.zero;
        handleAreaRT.anchorMax = Vector2.one;
        handleAreaRT.sizeDelta = new Vector2(-10f, 0f);

        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform handleRT = handle.AddComponent<RectTransform>();
        handleRT.sizeDelta = new Vector2(16f, 16f);
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;

        slider.fillRect = fillRT;
        slider.handleRect = handleRT;
        slider.targetGraphic = handleImg;
        slider.direction = Slider.Direction.LeftToRight;

        return slider;
    }

    private void CreatePresetSwatchButton(Transform parent, Color color, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject("Swatch_Btn");
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(28f, 28f);

        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = color;
        btnImg.raycastTarget = true;

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        btn.onClick.AddListener(onClick);
    }

    private Transform CreateRowContainer(Transform parent, float height, float spacing)
    {
        GameObject row = new GameObject("Row");
        row.transform.SetParent(parent, false);
        RectTransform rt = row.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(328f, height);

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = spacing;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;

        return row.transform;
    }

    private void CreateHeaderLabel(Transform parent, string text, float size, FontStyles style, Color color)
    {
        GameObject txtObj = new GameObject("Header_" + text);
        txtObj.transform.SetParent(parent, false);
        RectTransform rt = txtObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(328f, size + 6f);

        TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
    }

    private void CreateButtonLabel(Transform parent, string text, float size, FontStyles style, Color color)
    {
        GameObject txtObj = new GameObject("Label_" + text);
        txtObj.transform.SetParent(parent, false);
        RectTransform rt = txtObj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
    }

    private void LaunchGame()
    {
        Debug.Log("[StartScreenUI] Starting Run with Customized Character!");

        if (doofusPreview != null)
        {
            doofusPreview.transform.rotation = Quaternion.identity;
        }

        Time.timeScale = 1f;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGame();
        }
        else
        {
            GameEvents.TriggerGameStart();
        }

        gameObject.SetActive(false);
    }
}
