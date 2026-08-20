using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Combined Start Screen & Character Customization Lobby UI:
/// - Title & Animated Prompt (>> PRESS SPACE OR CLICK PLAY <<)
/// - Preset Palettes + 360-Degree Rainbow Color Wheel Swatches
/// - Target Selector (BODY / HEAD / EYES)
/// - Real-time 3D Doofus Turntable Rotation Preview
/// - Saves colors to PlayerPrefs and starts game seamlessly
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

    private Button bodyTabBtn;
    private Button headTabBtn;
    private Button eyeTabBtn;
    private Image bodyTabImg;
    private Image headTabImg;
    private Image eyeTabImg;

    private readonly List<Image> wheelSwatchOutlines = new List<Image>();

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
        RefreshTabHighlights();
    }

    private void Update()
    {
        // 1. Gently rotate Doofus in 3D for character preview
        if (doofusPreview != null)
        {
            doofusPreview.transform.Rotate(Vector3.up, 35f * Time.unscaledDeltaTime, Space.World);
        }

        // 2. Animated UI bounce & pulse
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

        // 3. Space / Enter keyboard launch
        bool spacePressed = Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame);
        if (spacePressed)
        {
            LaunchGame();
        }
    }

    private void BuildCustomizationLobbyPanel()
    {
        if (CustomizationManager.Instance == null) return;

        // Create sleek Dark-Glass Customization Card on the Right Side
        customPanelObj = new GameObject("Customization_Card");
        customPanelObj.transform.SetParent(transform, false);

        RectTransform panelRT = customPanelObj.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(1f, 0.5f);
        panelRT.anchorMax = new Vector2(1f, 0.5f);
        panelRT.pivot = new Vector2(1f, 0.5f);
        panelRT.anchoredPosition = new Vector2(-40f, 0f);
        panelRT.sizeDelta = new Vector2(380f, 460f);

        Image bgImage = customPanelObj.AddComponent<Image>();
        bgImage.color = new Color(0.04f, 0.07f, 0.11f, 0.90f);

        VerticalLayoutGroup vlg = customPanelObj.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(18, 18, 18, 18);
        vlg.spacing = 12f;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;

        // Card Header Title
        CreateTextLabel(customPanelObj.transform, "🎨 CHARACTER STUDIO", 18f, FontStyles.Bold, new Color(0f, 0.90f, 1f));

        // Part Selector Tabs [ BODY ] [ HEAD ] [ EYES ]
        Transform tabsRow = CreateRowContainer(customPanelObj.transform, 36f);
        bodyTabBtn = CreateTabButton(tabsRow, "BODY", () => SetActiveTab(CustomizationManager.CustomPart.Body), out bodyTabImg);
        headTabBtn = CreateTabButton(tabsRow, "HEAD", () => SetActiveTab(CustomizationManager.CustomPart.Head), out headTabImg);
        eyeTabBtn = CreateTabButton(tabsRow, "EYES", () => SetActiveTab(CustomizationManager.CustomPart.Eyes), out eyeTabImg);

        // Section Title: Rainbow Spectrum & Color Wheel
        CreateTextLabel(customPanelObj.transform, "🌈 360° COLOR WHEEL SPECTRUM", 12f, FontStyles.Bold, new Color(0.85f, 0.90f, 0.95f));

        // 360-Degree Rainbow Color Wheel Grid (2 Rows of 6 swatches)
        Transform wheelRow1 = CreateRowContainer(customPanelObj.transform, 34f);
        for (int i = 0; i < 6; i++)
        {
            int idx = i;
            Color c = CustomizationManager.Instance.rainbowWheelColors[i];
            CreateSwatchButton(wheelRow1, c, wheelSwatchOutlines, () => ApplyWheelColor(c));
        }

        Transform wheelRow2 = CreateRowContainer(customPanelObj.transform, 34f);
        for (int i = 6; i < CustomizationManager.Instance.rainbowWheelColors.Length; i++)
        {
            int idx = i;
            Color c = CustomizationManager.Instance.rainbowWheelColors[i];
            CreateSwatchButton(wheelRow2, c, wheelSwatchOutlines, () => ApplyWheelColor(c));
        }

        // Section Title: Quick Classic Presets
        CreateTextLabel(customPanelObj.transform, "QUICK PALETTE PRESETS", 12f, FontStyles.Bold, new Color(0.75f, 0.80f, 0.85f));
        Transform presetRow = CreateRowContainer(customPanelObj.transform, 32f);
        for (int i = 0; i < CustomizationManager.Instance.bodyColors.Length; i++)
        {
            int idx = i;
            Color c = CustomizationManager.Instance.bodyColors[i];
            CreateSwatchButton(presetRow, c, null, () => ApplyPresetColor(idx));
        }

        // Play Button
        GameObject playBtnObj = new GameObject("Play_Button");
        playBtnObj.transform.SetParent(customPanelObj.transform, false);
        RectTransform playBtnRT = playBtnObj.AddComponent<RectTransform>();
        playBtnRT.sizeDelta = new Vector2(340f, 44f);

        Image playBtnImg = playBtnObj.AddComponent<Image>();
        playBtnImg.color = new Color(0f, 0.85f, 0.45f);

        Button playBtn = playBtnObj.AddComponent<Button>();
        playBtn.onClick.AddListener(LaunchGame);

        CreateTextLabel(playBtnObj.transform, "START RUN  ▶", 16f, FontStyles.Bold, Color.black);
    }

    private Button CreateTabButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick, out Image tabBg)
    {
        GameObject btnObj = new GameObject("Tab_" + label);
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(105f, 32f);

        tabBg = btnObj.AddComponent<Image>();
        tabBg.color = new Color(0.15f, 0.20f, 0.28f);

        Button btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(onClick);

        CreateTextLabel(btnObj.transform, label, 12f, FontStyles.Bold, Color.white);
        return btn;
    }

    private void SetActiveTab(CustomizationManager.CustomPart part)
    {
        selectedPart = part;
        RefreshTabHighlights();
    }

    private void RefreshTabHighlights()
    {
        Color activeCol = new Color(0f, 0.80f, 1f, 1f);
        Color inactiveCol = new Color(0.15f, 0.20f, 0.28f, 0.9f);

        if (bodyTabImg != null) bodyTabImg.color = (selectedPart == CustomizationManager.CustomPart.Body) ? activeCol : inactiveCol;
        if (headTabImg != null) headTabImg.color = (selectedPart == CustomizationManager.CustomPart.Head) ? activeCol : inactiveCol;
        if (eyeTabImg != null) eyeTabImg.color = (selectedPart == CustomizationManager.CustomPart.Eyes) ? activeCol : inactiveCol;
    }

    private void ApplyWheelColor(Color c)
    {
        if (CustomizationManager.Instance == null) return;
        CustomizationManager.Instance.SetCustomPartColor(selectedPart, c);
    }

    private void ApplyPresetColor(int idx)
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
    }

    private Transform CreateRowContainer(Transform parent, float height)
    {
        GameObject row = new GameObject("Row");
        row.transform.SetParent(parent, false);
        RectTransform rt = row.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(340f, height);

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 6f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;

        return row.transform;
    }

    private void CreateSwatchButton(Transform parent, Color color, List<Image> outlineList, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject("Swatch_Btn");
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(28f, 28f);

        Image outlineImg = btnObj.AddComponent<Image>();
        outlineImg.color = new Color(1f, 1f, 1f, 0.2f);
        if (outlineList != null) outlineList.Add(outlineImg);

        GameObject inner = new GameObject("Color_Inner");
        inner.transform.SetParent(btnObj.transform, false);
        RectTransform innerRT = inner.AddComponent<RectTransform>();
        innerRT.anchorMin = Vector2.zero;
        innerRT.anchorMax = Vector2.one;
        innerRT.sizeDelta = new Vector2(-4f, -4f);

        Image innerImg = inner.AddComponent<Image>();
        innerImg.color = color;

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = innerImg;
        btn.onClick.AddListener(onClick);
    }

    private void CreateTextLabel(Transform parent, string text, float size, FontStyles style, Color color)
    {
        GameObject txtObj = new GameObject("Label_" + text);
        txtObj.transform.SetParent(parent, false);
        RectTransform rt = txtObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(340f, size + 6f);

        TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
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
