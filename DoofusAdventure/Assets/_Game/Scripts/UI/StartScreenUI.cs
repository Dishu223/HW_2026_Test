using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles Start Screen UI: triggers game start on Space, Enter, or Click.
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
        // Float title
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

        // Keyboard Space / Enter
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame))
        {
            StartGame();
            return;
        }

        // Mouse Left Click
        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            StartGame();
            return;
        }
    }

    public void StartGame()
    {
        Debug.Log("[StartScreenUI] Starting Game!");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetState(GameManager.GameState.Playing);
        }
        else
        {
            GameEvents.TriggerGameStart();
        }
    }
}
