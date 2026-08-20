using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Simple, self-contained Start Screen controller:
/// - Floats title and pulses prompt
/// - When player presses Space or clicks, launches game and hides itself
/// </summary>
public class StartScreenUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI pressSpacePrompt;

    private float initialTitleY;

    private void Start()
    {
        if (titleText != null)
            initialTitleY = titleText.rectTransform.anchoredPosition.y;
    }

    private void Update()
    {
        // Gentle title float
        if (titleText != null)
        {
            float newY = initialTitleY + Mathf.Sin(Time.unscaledTime * 3f) * 10f;
            titleText.rectTransform.anchoredPosition = new Vector2(titleText.rectTransform.anchoredPosition.x, newY);
        }

        // Pulse prompt
        if (pressSpacePrompt != null)
        {
            float alpha = 0.4f + Mathf.PingPong(Time.unscaledTime * 2.5f, 0.6f);
            pressSpacePrompt.alpha = alpha;
        }

        // Check Space, Enter, or Click
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

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetState(GameManager.GameState.Playing);
        }
        else
        {
            GameEvents.TriggerGameStart();
        }

        // Hide this panel immediately
        gameObject.SetActive(false);
    }
}
