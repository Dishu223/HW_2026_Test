using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Combined Start Screen & Character Customization Lobby UI:
/// - Title & Animated Prompt (>> PRESS SPACE OR CLICK PLAY <<)
/// - Interactive Color Swatch Palettes for Body, Head, and Eyes
/// - Real-time 3D Doofus Turntable Rotation Preview in the Lobby
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
    private readonly List<Image> bodySwatchOutlines = new List<Image>();
    private readonly List<Image> headSwatchOutlines = new List<Image>();
    private readonly List<Image> eyeSwatchOutlines = new List<Image>();

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
        RefreshSelectionOutlines();
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
        panelRT.sizeDelta = new Vector2(360f, 420f);

        Image bgImage = customPanelObj.AddComponent<Image>();
        bgImage.color = new Color(0.05f, 0.08f, 0.12f, 0.88f); // Sleek dark glass

        VerticalLayoutGroup vlg = customPanelObj.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(20, 20, 20, 20);
        vlg.spacing = 14f;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;

        // Card Header Title
        CreateTextLabel(customPanelObj.transform, "🎨 CUSTOMIZE DOOFUS", 20f, FontStyles.Bold, new Color(0f, 0.9f, 1f));

        // 1. Body Color Palette
        CreateTextLabel(customPanelObj.transform, "BODY COLOR", 13f, FontStyles.SemiBold, new Color(0.8f, 0.85f, 0.9f));
        Transform bodyRow = CreateRowContainer(customPanelObj.transform);
        for (int i = 0; i < CustomizationManager.Instance.bodyColors.Length; i++)
        {
            int idx = i;
            CreateSwatchButton(bodyRow, CustomizationManager.Instance.bodyColors[i], bodySwatchOutlines, () => {
                CustomizationManager.Instance.SetBodyColor(idx);
                RefreshSelectionOutlines();
            });
        }

        // 2. Head Color Palette
        CreateTextLabel(customPanelObj.transform, "HEAD COLOR", 13f, FontStyles.SemiBold, new Color(0.8f, 0.85f, 0.9f));
        Transform headRow = CreateRowContainer(customPanelObj.transform);
        for (int i = 0; i < CustomizationManager.Instance.bodyColors.Length; i++)
        {
            int idx = i;
            CreateSwatchButton(headRow, CustomizationManager.Instance.bodyColors[i], headSwatchOutlines, () => {
                CustomizationManager.Instance.SetHeadColor(idx);
                RefreshSelectionOutlines();
            });
        }

        // 3. Eye Color Palette
        CreateTextLabel(customPanelObj.transform, "EYE COLOR", 13f, FontStyles.SemiBold, new Color(0.8f, 0.85f, 0.9f));
        Transform eyeRow = CreateRowContainer(customPanelObj.transform);
        for (int i = 0; i < CustomizationManager.Instance.eyeColors.Length; i++)
        {
            int idx = i;
            CreateSwatchButton(eyeRow, CustomizationManager.Instance.eyeColors[i], eyeSwatchOutlines, () => {
                CustomizationManager.Instance.SetEyeColor(idx);
                RefreshSelectionOutlines();
            });
        }

        // 4. Play Button
        GameObject playBtnObj = new GameObject("Play_Button");
        playBtnObj.transform.SetParent(customPanelObj.transform, false);
        RectTransform playBtnRT = playBtnObj.AddComponent<RectTransform>();
        playBtnRT.sizeDelta = new Vector2(320f, 44f);

        Image playBtnImg = playBtnObj.AddComponent<Image>();
        playBtnImg.color = new Color(0f, 0.85f, 0.45f); // Vibrant Emerald Green

        Button playBtn = playBtnObj.AddComponent<Button>();
        playBtn.onClick.AddListener(LaunchGame);

        CreateTextLabel(playBtnObj.transform, "START RUN  ▶", 16f, FontStyles.Bold, Color.black);
    }

    private Transform CreateRowContainer(Transform parent)
    {
        GameObject row = new GameObject("Swatch_Row");
        row.transform.SetParent(parent, false);
        RectTransform rt = row.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(320f, 34f);

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
        rt.sizeDelta = new Vector2(32f, 32f);

        // Selection Border / Outline
        Image outlineImg = btnObj.AddComponent<Image>();
        outlineImg.color = new Color(1f, 1f, 1f, 0.2f);
        outlineList.Add(outlineImg);

        // Inner Color Circle
        GameObject inner = new GameObject("Color_Inner");
        inner.transform.SetParent(btnObj.transform, false);
        RectTransform innerRT = inner.AddComponent<RectTransform>();
        innerRT.anchorMin = Vector2.zero;
        innerRT.anchorMax = Vector2.one;
        innerRT.sizeDelta = new Vector2(-4f, -4f); // 2px margin

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
        rt.sizeDelta = new Vector2(320f, size + 8f);

        TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
    }

    private void RefreshSelectionOutlines()
    {
        if (CustomizationManager.Instance == null) return;

        // Highlight active body swatch
        for (int i = 0; i < bodySwatchOutlines.Count; i++)
        {
            bool isSelected = (i == CustomizationManager.Instance.CurrentBodyIndex);
            bodySwatchOutlines[i].color = isSelected ? new Color(0f, 0.9f, 1f, 1f) : new Color(1f, 1f, 1f, 0.15f);
        }

        // Highlight active head swatch
        for (int i = 0; i < headSwatchOutlines.Count; i++)
        {
            bool isSelected = (i == CustomizationManager.Instance.CurrentHeadIndex);
            headSwatchOutlines[i].color = isSelected ? new Color(0f, 0.9f, 1f, 1f) : new Color(1f, 1f, 1f, 0.15f);
        }

        // Highlight active eye swatch
        for (int i = 0; i < eyeSwatchOutlines.Count; i++)
        {
            bool isSelected = (i == CustomizationManager.Instance.CurrentEyeIndex);
            eyeSwatchOutlines[i].color = isSelected ? new Color(0f, 0.9f, 1f, 1f) : new Color(1f, 1f, 1f, 0.15f);
        }
    }

    private void LaunchGame()
    {
        Debug.Log("[StartScreenUI] Starting Run with Customized Character!");

        // Reset Doofus rotation forward before starting
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
