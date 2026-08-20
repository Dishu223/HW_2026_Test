using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles Start Screen: press Space/Enter or Click anywhere to start the game.
/// Includes title floating animation and prompt pulsing.
/// </summary>
public class StartScreenUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI pressSpacePrompt;

    private float initialTitleY;
    private bool hasTriggeredStart = false;

    private void Start()
    {
        if (titleText != null)
            initialTitleY = titleText.rectTransform.anchoredPosition.y;
    }

    private void OnEnable()
    {
        hasTriggeredStart = false;
    }

    private void Update()
    {
        // Animate title float
        if (titleText != null)
        {
            float newY = initialTitleY + Mathf.Sin(Time.unscaledTime * 3f) * 10f;
            titleText.rectTransform.anchoredPosition = new Vector2(titleText.rectTransform.anchoredPosition.x, newY);
        }

        // Animate press space prompt pulse
        if (pressSpacePrompt != null)
        {
            float alpha = 0.4f + Mathf.PingPong(Time.unscaledTime * 2.5f, 0.6f);
            pressSpacePrompt.alpha = alpha;
        }

        if (hasTriggeredStart) return;

        // Check Keyboard Space / Enter
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame))
        {
            OnStartPressed();
            return;
        }

        // Check Mouse Left Click / Touch on screen
        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            OnStartPressed();
            return;
        }

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            OnStartPressed();
            return;
        }
    }

    public void OnStartPressed()
    {
        if (hasTriggeredStart) return;
        hasTriggeredStart = true;

        Debug.Log("[StartScreenUI] Starting Game!");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetState(GameManager.GameState.Playing);
        }
        else
        {
            // Direct event fallback if GameManager instance wasn't found
            GameEvents.TriggerGameStart();
        }

        // Direct panel switch fallback
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowPanelImmediate(null); // Clear start screen
        }
    }
}
