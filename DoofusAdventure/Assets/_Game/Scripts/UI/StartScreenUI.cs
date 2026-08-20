using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Simple, self-contained Start Screen controller:
/// - Freezes game time on Awake (Time.timeScale = 0)
/// - Floats title and pulses prompt using unscaledTime
/// - When player presses Space or clicks, launches game, unpauses (Time.timeScale = 1), and hides itself
/// </summary>
public class StartScreenUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI pressSpacePrompt;

    private float initialTitleY;

    private void Awake()
    {
        Time.timeScale = 0f;
    }

    private void Start()
    {
        Time.timeScale = 0f;
        if (titleText != null)
            initialTitleY = titleText.rectTransform.anchoredPosition.y;
    }

    private void Update()
    {
        if (titleText != null)
        {
            float newY = initialTitleY + Mathf.Sin(Time.unscaledTime * 3f) * 10f;
            titleText.rectTransform.anchoredPosition = new Vector2(titleText.rectTransform.anchoredPosition.x, newY);
        }

        if (pressSpacePrompt != null)
        {
            float alpha = 0.4f + Mathf.PingPong(Time.unscaledTime * 2.5f, 0.6f);
            pressSpacePrompt.alpha = alpha;
        }

        bool spacePressed = Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame);
        bool mouseClicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

        if (spacePressed || mouseClicked)
        {
            LaunchGame();
        }
    }

    private void LaunchGame()
    {
        Debug.Log("[StartScreenUI] Launching Game!");

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
