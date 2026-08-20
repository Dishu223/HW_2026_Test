using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles Start Screen inputs: Press Space to enter Lobby / Start Game.
/// Includes title floating animation and prompt pulsing.
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
        // Animate title float
        if (titleText != null)
        {
            float newY = initialTitleY + Mathf.Sin(Time.unscaledTime * 3f) * 12f;
            titleText.rectTransform.anchoredPosition = new Vector2(titleText.rectTransform.anchoredPosition.x, newY);
        }

        // Animate press space prompt pulse
        if (pressSpacePrompt != null)
        {
            float alpha = 0.4f + Mathf.PingPong(Time.unscaledTime * 2f, 0.6f);
            pressSpacePrompt.alpha = alpha;
        }

        // Detect Space / Enter to open Lobby
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame))
        {
            OnStartPressed();
        }
    }

    public void OnStartPressed()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetState(GameManager.GameState.Lobby);
        }
    }
}
