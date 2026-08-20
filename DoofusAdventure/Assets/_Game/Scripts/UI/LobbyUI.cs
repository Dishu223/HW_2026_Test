using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Customization Lobby UI where players click color buttons for Body, Head, and Eyes,
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

    private void Start()
    {
        PopulateColorSwatches();
    }

    /// <summary>
    /// Dynamically creates color swatch buttons if containers are assigned.
    /// </summary>
    public void PopulateColorSwatches()
    {
        if (CustomizationManager.Instance == null || colorButtonPrefab == null) return;

        // Body Color Swatches
        if (bodyColorButtonsContainer != null && bodyColorButtonsContainer.childCount <= 1)
        {
            for (int i = 0; i < CustomizationManager.Instance.bodyColors.Length; i++)
            {
                int index = i;
                Button btn = Instantiate(colorButtonPrefab, bodyColorButtonsContainer);
                btn.gameObject.SetActive(true);
                btn.image.color = CustomizationManager.Instance.bodyColors[i];
                btn.onClick.AddListener(() => CustomizationManager.Instance.SetBodyColor(index));
            }
        }

        // Head Color Swatches
        if (headColorButtonsContainer != null && headColorButtonsContainer.childCount <= 1)
        {
            for (int i = 0; i < CustomizationManager.Instance.bodyColors.Length; i++)
            {
                int index = i;
                Button btn = Instantiate(colorButtonPrefab, headColorButtonsContainer);
                btn.gameObject.SetActive(true);
                btn.image.color = CustomizationManager.Instance.bodyColors[i];
                btn.onClick.AddListener(() => CustomizationManager.Instance.SetHeadColor(index));
            }
        }

        // Eye Color Swatches
        if (eyeColorButtonsContainer != null && eyeColorButtonsContainer.childCount <= 1)
        {
            for (int i = 0; i < CustomizationManager.Instance.eyeColors.Length; i++)
            {
                int index = i;
                Button btn = Instantiate(colorButtonPrefab, eyeColorButtonsContainer);
                btn.gameObject.SetActive(true);
                btn.image.color = CustomizationManager.Instance.eyeColors[i];
                btn.onClick.AddListener(() => CustomizationManager.Instance.SetEyeColor(index));
            }
        }
    }

    public void OnPlayButtonPressed()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetState(GameManager.GameState.Playing);
        }
    }
}
