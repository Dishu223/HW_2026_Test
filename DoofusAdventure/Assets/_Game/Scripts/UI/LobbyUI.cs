using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Customization Lobby UI where players click color swatches for Body, Head, and Eyes,
/// previewing Doofus in real-time, then press PLAY to start the run.
/// </summary>
public class LobbyUI : MonoBehaviour
{
    [Header("Color Swatch Containers (Parent of Buttons)")]
    [SerializeField] private Transform bodyColorButtonsContainer;
    [SerializeField] private Transform headColorButtonsContainer;
    [SerializeField] private Transform eyeColorButtonsContainer;

    [Header("Color Button Prefab / Template")]
    [SerializeField] private Button colorButtonPrefab;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void Start()
    {
        PopulateColorSwatches();
    }

    /// <summary>
    /// Dynamically creates color swatch buttons if containers are assigned.
    /// </summary>
    public void PopulateColorSwatches()
    {
        if (CustomizationManager.Instance == null) return;

        // Auto-generate swatch buttons if empty
        if (bodyColorButtonsContainer != null && bodyColorButtonsContainer.childCount <= 1)
        {
            BuildSwatchesFor(bodyColorButtonsContainer, CustomizationManager.Instance.bodyColors, (idx) => CustomizationManager.Instance.SetBodyColor(idx));
        }

        if (headColorButtonsContainer != null && headColorButtonsContainer.childCount <= 1)
        {
            BuildSwatchesFor(headColorButtonsContainer, CustomizationManager.Instance.bodyColors, (idx) => CustomizationManager.Instance.SetHeadColor(idx));
        }

        if (eyeColorButtonsContainer != null && eyeColorButtonsContainer.childCount <= 1)
        {
            BuildSwatchesFor(eyeColorButtonsContainer, CustomizationManager.Instance.eyeColors, (idx) => CustomizationManager.Instance.SetEyeColor(idx));
        }
    }

    private void BuildSwatchesFor(Transform container, Color[] colors, System.Action<int> onSelect)
    {
        for (int i = 0; i < colors.Length; i++)
        {
            int index = i;
            GameObject btnObj;

            if (colorButtonPrefab != null)
            {
                btnObj = Instantiate(colorButtonPrefab.gameObject, container);
            }
            else
            {
                // Procedural button creation
                btnObj = new GameObject($"Swatch_{index}");
                btnObj.transform.SetParent(container, false);
                Image img = btnObj.AddComponent<Image>();
                Button btn = btnObj.AddComponent<Button>();
                RectTransform rt = btnObj.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(40f, 40f);
            }

            btnObj.SetActive(true);
            Image btnImage = btnObj.GetComponent<Image>();
            if (btnImage != null) btnImage.color = colors[i];

            Button buttonComp = btnObj.GetComponent<Button>();
            if (buttonComp != null)
            {
                buttonComp.onClick.AddListener(() => onSelect(index));
            }
        }
    }

    public void OnPlayButtonPressed()
    {
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
