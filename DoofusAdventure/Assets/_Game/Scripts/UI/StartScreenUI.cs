using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Start Screen / Character Studio UI:
/// - 100% respects your custom Inspector Colors, Fonts, and Layouts!
/// - Animated elastic title bounce and pulsing prompt
/// - Interactive 360° Circular Color Wheel Customization Studio
/// </summary>
public class StartScreenUI : MonoBehaviour
{
    public static StartScreenUI Instance { get; private set; }

    [Header("UI Text References")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI pressSpacePrompt;

    private float initialTitleY;
    private float initialPromptX;
    private float initialPromptY;
    private Vector3 initialPromptScale = Vector3.one;

    private GameObject customPanelObj;
    private Image bodyTabBg, headTabBg, eyeTabBg;
    private Image liveColorPreviewBox;
    private ColorWheelPicker colorWheel;
    private Slider brightnessSlider;
    private CustomizationManager.CustomPart selectedPart = CustomizationManager.CustomPart.Body;

    private DoofusController doofusPreview;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
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
            initialPromptX = pressSpacePrompt.rectTransform.anchoredPosition.x;
            initialPromptY = pressSpacePrompt.rectTransform.anchoredPosition.y;
            initialPromptScale = pressSpacePrompt.rectTransform.localScale;
            pressSpacePrompt.text = ">> PRESS SPACE TO START <<";
        }

        BuildCustomizationLobbyPanel();
        RefreshActiveTabVisuals();
    }

    private void Update()
    {
        // 1. Rotate Doofus preview
        if (doofusPreview != null)
        {
            doofusPreview.transform.Rotate(Vector3.up, 35f * Time.unscaledDeltaTime, Space.World);
        }

        // 2. Animated Title Bounce
        if (titleText != null)
        {
            float newY = initialTitleY + Mathf.Sin(Time.unscaledTime * 3f) * 8f;
            titleText.rectTransform.anchoredPosition = new Vector2(titleText.rectTransform.anchoredPosition.x, newY);
        }

        // 3. Elastic Bouncing & Soft Pulsing "PRESS SPACE" Prompt
        if (pressSpacePrompt != null)
        {
            float bounceX = initialPromptX + Mathf.Sin(Time.unscaledTime * 4.5f) * 10f;
            pressSpacePrompt.rectTransform.anchoredPosition = new Vector2(bounceX, initialPromptY);

            float scalePulse = 1.0f + Mathf.Sin(Time.unscaledTime * 5.0f) * 0.08f;
            pressSpacePrompt.rectTransform.localScale = initialPromptScale * scalePulse;

            pressSpacePrompt.alpha = 0.65f + Mathf.PingPong(Time.unscaledTime * 2.5f, 0.35f);
        }

        // 4. Space / Enter launch
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
        vlg.spacing = 12f;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;

        CreateCardTitle(customPanelObj.transform);
        CreateCategoryTabs(customPanelObj.transform);
        CreateColorWheelContainer(customPanelObj.transform);
        CreateBrightnessSliderContainer(customPanelObj.transform);
        CreatePresetSwatches(customPanelObj.transform);
        CreatePlayButton(customPanelObj.transform);
    }

    private void CreateCardTitle(Transform parent)
    {
        GameObject titleObj = new GameObject("Card_Title");
        titleObj.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = titleObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "CHARACTER STUDIO";
        tmp.fontSize = 20f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.38f, 0.85f, 1f);

        LayoutElement le = titleObj.AddComponent<LayoutElement>();
        le.preferredHeight = 28f;
    }

    private void CreateColorWheelContainer(Transform parent)
    {
        GameObject wheelObj = new GameObject("ColorWheel_Picker");
        wheelObj.transform.SetParent(parent, false);

        RectTransform rt = wheelObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(180f, 180f);

        LayoutElement le = wheelObj.AddComponent<LayoutElement>();
        le.preferredWidth = 180f;
        le.preferredHeight = 180f;

        colorWheel = wheelObj.AddComponent<ColorWheelPicker>();
        colorWheel.Initialize(180f);
        colorWheel.OnColorChanged += HandleColorWheelChanged;
    }

    private void CreateBrightnessSliderContainer(Transform parent)
    {
        GameObject sliderRow = new GameObject("Slider_Row");
        sliderRow.transform.SetParent(parent, false);

        RectTransform rt = sliderRow.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(320f, 32f);

        LayoutElement le = sliderRow.AddComponent<LayoutElement>();
        le.preferredHeight = 32f;

        HorizontalLayoutGroup hlg = sliderRow.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10f;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;

        GameObject previewObj = new GameObject("Preview_Swatch");
        previewObj.transform.SetParent(sliderRow.transform, false);
        liveColorPreviewBox = previewObj.AddComponent<Image>();
        liveColorPreviewBox.color = Color.white;
        LayoutElement previewLE = previewObj.AddComponent<LayoutElement>();
        previewLE.preferredWidth = 32f;

        GameObject sliderObj = new GameObject("Brightness_Slider");
        sliderObj.transform.SetParent(sliderRow.transform, false);
        brightnessSlider = sliderObj.AddComponent<Slider>();
        brightnessSlider.minValue = 0f;
        brightnessSlider.maxValue = 1f;
        brightnessSlider.value = 1f;
        brightnessSlider.onValueChanged.AddListener(HandleBrightnessChanged);

        LayoutElement sliderLE = sliderObj.AddComponent<LayoutElement>();
        sliderLE.preferredWidth = 260f;

        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(sliderObj.transform, false);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.25f, 0.35f);
        RectTransform bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0, 0.25f);
        bgRT.anchorMax = new Vector2(1, 0.75f);
        bgRT.sizeDelta = Vector2.zero;

        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRT = fillArea.AddComponent<RectTransform>();
        fillAreaRT.anchorMin = new Vector2(0, 0.25f);
        fillAreaRT.anchorMax = new Vector2(1, 0.75f);
        fillAreaRT.sizeDelta = Vector2.zero;

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0f, 0.85f, 1f);
        RectTransform fillRT = fill.GetComponent<RectTransform>();
        fillRT.sizeDelta = Vector2.zero;

        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderObj.transform, false);
        RectTransform handleAreaRT = handleArea.AddComponent<RectTransform>();
        handleAreaRT.anchorMin = Vector2.zero;
        handleAreaRT.anchorMax = Vector2.one;
        handleAreaRT.sizeDelta = Vector2.zero;

        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;
        RectTransform handleRT = handle.GetComponent<RectTransform>();
        handleRT.sizeDelta = new Vector2(20f, 20f);

        brightnessSlider.targetGraphic = handleImg;
        brightnessSlider.fillRect = fillRT;
        brightnessSlider.handleRect = handleRT;
        brightnessSlider.direction = Slider.Direction.LeftToRight;
    }

    private void CreatePlayButton(Transform parent)
    {
        GameObject btnObj = new GameObject("StartRun_Button");
        btnObj.transform.SetParent(parent, false);

        Image playBtnImg = btnObj.AddComponent<Image>();
        playBtnImg.color = new Color(0f, 0.85f, 0.45f);

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = playBtnImg;
        btn.onClick.AddListener(LaunchGame);

        LayoutElement le = btnObj.AddComponent<LayoutElement>();
        le.preferredHeight = 44f;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "START RUN >>";
        tmp.fontSize = 18f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.04f, 0.12f, 0.08f);

        RectTransform textRT = textObj.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.sizeDelta = Vector2.zero;
    }

    private void CreateCategoryTabs(Transform parent)
    {
        GameObject tabRow = new GameObject("Tab_Row");
        tabRow.transform.SetParent(parent, false);

        RectTransform rt = tabRow.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(320f, 34f);

        LayoutElement le = tabRow.AddComponent<LayoutElement>();
        le.preferredHeight = 34f;

        HorizontalLayoutGroup hlg = tabRow.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8f;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;

        bodyTabBg = CreateSingleTab(tabRow.transform, "BODY", () => SelectCategory(CustomizationManager.CustomPart.Body));
        headTabBg = CreateSingleTab(tabRow.transform, "HEAD", () => SelectCategory(CustomizationManager.CustomPart.Head));
        eyeTabBg = CreateSingleTab(tabRow.transform, "EYES", () => SelectCategory(CustomizationManager.CustomPart.Eyes));
    }

    private Image CreateSingleTab(Transform parent, string label, System.Action onClick)
    {
        GameObject tabObj = new GameObject($"Tab_{label}");
        tabObj.transform.SetParent(parent, false);

        Image tabBg = tabObj.AddComponent<Image>();
        tabBg.color = new Color(0.14f, 0.18f, 0.25f);

        Button btn = tabObj.AddComponent<Button>();
        btn.targetGraphic = tabBg;
        btn.onClick.AddListener(() => onClick?.Invoke());

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(tabObj.transform, false);
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 14f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        RectTransform textRT = textObj.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.sizeDelta = Vector2.zero;

        return tabBg;
    }

    private void CreatePresetSwatches(Transform parent)
    {
        GameObject swatchRow = new GameObject("Preset_Swatches");
        swatchRow.transform.SetParent(parent, false);

        RectTransform rt = swatchRow.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(320f, 28f);

        LayoutElement le = swatchRow.AddComponent<LayoutElement>();
        le.preferredHeight = 28f;

        HorizontalLayoutGroup hlg = swatchRow.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 6f;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;

        Color[] presets = new Color[]
        {
            Color.white,
            new Color(1f, 0.45f, 0.45f), // Coral
            new Color(0.45f, 1f, 0.55f), // Mint
            new Color(0.4f, 0.75f, 1f),  // Sky
            new Color(1f, 0.85f, 0.35f), // Lemon
            new Color(0.85f, 0.55f, 1f), // Lilac
            new Color(0.15f, 0.15f, 0.15f) // Noir
        };

        foreach (Color c in presets)
        {
            GameObject btnObj = new GameObject("Swatch_Btn");
            btnObj.transform.SetParent(swatchRow.transform, false);

            Image btnImg = btnObj.AddComponent<Image>();
            btnImg.color = c;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            btn.onClick.AddListener(() => ApplyPresetColor(c));
        }
    }

    private void SelectCategory(CustomizationManager.CustomPart part)
    {
        selectedPart = part;
        RefreshActiveTabVisuals();
    }

    private void RefreshActiveTabVisuals()
    {
        Color activeCol = new Color(0f, 0.85f, 1f);
        Color inactiveCol = new Color(0.12f, 0.16f, 0.22f);

        if (bodyTabBg != null) bodyTabBg.color = (selectedPart == CustomizationManager.CustomPart.Body) ? activeCol : inactiveCol;
        if (headTabBg != null) headTabBg.color = (selectedPart == CustomizationManager.CustomPart.Head) ? activeCol : inactiveCol;
        if (eyeTabBg != null) eyeTabBg.color = (selectedPart == CustomizationManager.CustomPart.Eyes) ? activeCol : inactiveCol;
    }

    private void HandleColorWheelChanged(Color newColor)
    {
        if (CustomizationManager.Instance != null)
        {
            CustomizationManager.Instance.ApplyColorToPart(selectedPart, newColor);
        }
        if (liveColorPreviewBox != null) liveColorPreviewBox.color = newColor;
    }

    private void HandleBrightnessChanged(float val)
    {
        if (colorWheel != null)
        {
            colorWheel.SetBrightnessMultiplier(val);
        }
    }

    private void ApplyPresetColor(Color c)
    {
        if (CustomizationManager.Instance != null)
        {
            CustomizationManager.Instance.ApplyColorToPart(selectedPart, c);
        }
        if (liveColorPreviewBox != null) liveColorPreviewBox.color = c;
    }

    public void LaunchGame()
    {
        Time.timeScale = 1f;
        gameObject.SetActive(false);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGame();
        }
        else
        {
            GameEvents.TriggerGameStart();
        }
    }
}
