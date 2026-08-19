using UnityEngine;
using UnityEngine.UI;

// Handles user interactions in the Character Customization Lobby.
// Dynamically creates or assigns color swatch buttons for Body, Head, and Eyes.
public class LobbyUI : MonoBehaviour
{
    [Header("Color Swatch Containers")]
    [SerializeField] private Transform bodyColorContainer;
    [SerializeField] private Transform headColorContainer;
    [SerializeField] private Transform eyeColorContainer;

    [Header("Prefab for Swatch Button")]
    [SerializeField] private GameObject swatchButtonPrefab;

    [Header("Navigation Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button backButton;

    private void Awake()
    {
        if (playButton != null) playButton.onClick.AddListener(OnPlayClicked);
        if (backButton != null) backButton.onClick.AddListener(OnBackClicked);
    }

    private void Start()
    {
        GenerateSwatches();
    }

    private void OnEnable()
    {
        // Refresh preview when lobby opens
        if (CustomizationManager.Instance != null)
        {
            CustomizationManager.Instance.ApplyCustomization();
        }
    }

    private void GenerateSwatches()
    {
        if (swatchButtonPrefab == null) return;

        // 1. Generate Body Swatches
        if (bodyColorContainer != null && bodyColorContainer.childCount == 0)
        {
            for (int i = 0; i < CustomizationManager.BodyColors.Length; i++)
            {
                int index = i;
                GameObject btnObj = Instantiate(swatchButtonPrefab, bodyColorContainer);
                Image img = btnObj.GetComponent<Image>();
                Button btn = btnObj.GetComponent<Button>();
                if (img != null) img.color = CustomizationManager.BodyColors[i];
                if (btn != null) btn.onClick.AddListener(() => OnBodyColorSelected(index));
            }
        }

        // 2. Generate Head Swatches
        if (headColorContainer != null && headColorContainer.childCount == 0)
        {
            for (int i = 0; i < CustomizationManager.HeadColors.Length; i++)
            {
                int index = i;
                GameObject btnObj = Instantiate(swatchButtonPrefab, headColorContainer);
                Image img = btnObj.GetComponent<Image>();
                Button btn = btnObj.GetComponent<Button>();
                if (img != null) img.color = CustomizationManager.HeadColors[i];
                if (btn != null) btn.onClick.AddListener(() => OnHeadColorSelected(index));
            }
        }

        // 3. Generate Eye Swatches
        if (eyeColorContainer != null && eyeColorContainer.childCount == 0)
        {
            for (int i = 0; i < CustomizationManager.EyeColors.Length; i++)
            {
                int index = i;
                GameObject btnObj = Instantiate(swatchButtonPrefab, eyeColorContainer);
                Image img = btnObj.GetComponent<Image>();
                Button btn = btnObj.GetComponent<Button>();
                if (img != null) img.color = CustomizationManager.EyeColors[i];
                if (btn != null) btn.onClick.AddListener(() => OnEyeColorSelected(index));
            }
        }
    }

    private void OnBodyColorSelected(int index)
    {
        if (CustomizationManager.Instance != null)
        {
            CustomizationManager.Instance.SetBodyColor(index);
        }
    }

    private void OnHeadColorSelected(int index)
    {
        if (CustomizationManager.Instance != null)
        {
            CustomizationManager.Instance.SetHeadColor(index);
        }
    }

    private void OnEyeColorSelected(int index)
    {
        if (CustomizationManager.Instance != null)
        {
            CustomizationManager.Instance.SetEyeColor(index);
        }
    }

    private void OnPlayClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGame();
        }
    }

    private void OnBackClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReturnToStart();
        }
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowStartScreen();
        }
    }
}
