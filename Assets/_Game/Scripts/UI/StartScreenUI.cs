using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Handles animations and user interactions on the Title / Start Screen.
public class StartScreenUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private Button startButton;
    [SerializeField] private Button customizeButton;

    private Vector3 initialTitlePos;

    private void Awake()
    {
        if (titleText != null) initialTitlePos = titleText.transform.localPosition;

        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartClicked);
        }

        if (customizeButton != null)
        {
            customizeButton.onClick.AddListener(OnCustomizeClicked);
        }
    }

    private void Update()
    {
        // 1. Floating title animation
        if (titleText != null)
        {
            float yOffset = Mathf.Sin(Time.unscaledTime * 2.5f) * 8f;
            titleText.transform.localPosition = initialTitlePos + new Vector3(0f, yOffset, 0f);
        }

        // 2. Pulsing prompt alpha
        if (promptText != null)
        {
            float alpha = 0.5f + (Mathf.Sin(Time.unscaledTime * 4f) * 0.5f);
            promptText.color = new Color(promptText.color.r, promptText.color.g, promptText.color.b, alpha);
        }
    }

    private void OnStartClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGame();
        }
    }

    private void OnCustomizeClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OpenLobby();
        }
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowLobby();
        }
    }
}
