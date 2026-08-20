using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles Start Screen: press Space/Enter or Click to start.
/// Disables itself once the game begins so Space does not accidentally restart during gameplay.
/// </summary>
public class StartScreenUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI pressSpacePrompt;

    private float initialTitleY;
    private bool isGameActive = false;

    private void Start()
    {
        if (titleText != null)
            initialTitleY = titleText.rectTransform.anchoredPosition.y;
    }

    private void OnEnable()
    {
        isGameActive = false;
        GameEvents.OnGameStart += HandleGameStarted;
        GameEvents.OnGameOver += HandleGameOver;
    }

    private void OnDisable()
    {
        GameEvents.OnGameStart -= HandleGameStarted;
        GameEvents.OnGameOver -= HandleGameOver;
    }

    private void Update()
    {
        // Never process start input if game has already started
        if (isGameActive) return;

        // Animate title float
        if (titleText != null)
        {
            float newY = initialTitleY + Mathf.Sin(Time.unscaledTime * 3f) * 10f;
            titleText.rectTransform.anchoredPosition = new Vector2(titleText.rectTransform.anchoredPosition.x, newY);
        }

        // Animate prompt pulse
        if (pressSpacePrompt != null)
        {
            float alpha = 0.4f + Mathf.PingPong(Time.unscaledTime * 2.5f, 0.6f);
            pressSpacePrompt.alpha = alpha;
        }

        // Check Keyboard Space / Enter
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame))
        {
            StartGame();
            return;
        }

        // Check Mouse Left Click
        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            StartGame();
            return;
        }
    }

    private void StartGame()
    {
        if (isGameActive) return;
        isGameActive = true;

        Debug.Log("[StartScreenUI] Launching Game!");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetState(GameManager.GameState.Playing);
        }
        else
        {
            GameEvents.TriggerGameStart();
        }
    }

    private void HandleGameStarted()
    {
        isGameActive = true;
    }

    private void HandleGameOver()
    {
        isGameActive = false;
    }
}
