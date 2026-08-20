using System.Collections;
using UnityEngine;

/// <summary>
/// Master UI Router managing panel switching (Start Screen, Lobby, HUD, Game Over)
/// with rock-solid visibility management.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Panels (CanvasGroups)")]
    [SerializeField] private CanvasGroup startScreenPanel;
    [SerializeField] private CanvasGroup lobbyPanel;
    [SerializeField] private CanvasGroup hudPanel;
    [SerializeField] private CanvasGroup gameOverPanel;

    [Header("Transition Settings")]
    [SerializeField] private float fadeDuration = 0.15f;

    private CanvasGroup activePanel;
    private Coroutine transitionRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        // Show Start Screen initially
        ShowStartScreen();
    }

    private void OnEnable()
    {
        GameEvents.OnGameStart += HandleGameStart;
        GameEvents.OnGameOver += HandleGameOver;
        GameEvents.OnGameRestart += HandleGameStart;
        GameEvents.OnReturnToLobby += HandleReturnToLobby;
    }

    private void OnDisable()
    {
        GameEvents.OnGameStart -= HandleGameStart;
        GameEvents.OnGameOver -= HandleGameOver;
        GameEvents.OnGameRestart -= HandleGameStart;
        GameEvents.OnReturnToLobby -= HandleReturnToLobby;
    }

    public void ShowStartScreen()
    {
        SetPanelState(startScreenPanel, true);
        SetPanelState(lobbyPanel, false);
        SetPanelState(hudPanel, false);
        SetPanelState(gameOverPanel, false);
        activePanel = startScreenPanel;
    }

    public void ShowHUD()
    {
        SetPanelState(startScreenPanel, false);
        SetPanelState(lobbyPanel, false);
        SetPanelState(gameOverPanel, false);
        SetPanelState(hudPanel, true);
        activePanel = hudPanel;
    }

    public void ShowGameOver()
    {
        SetPanelState(startScreenPanel, false);
        SetPanelState(lobbyPanel, false);
        SetPanelState(hudPanel, false);
        SetPanelState(gameOverPanel, true);
        activePanel = gameOverPanel;
    }

    public void ShowLobby()
    {
        SetPanelState(startScreenPanel, false);
        SetPanelState(lobbyPanel, true);
        SetPanelState(hudPanel, false);
        SetPanelState(gameOverPanel, false);
        activePanel = lobbyPanel;
    }

    private void SetPanelState(CanvasGroup panel, bool visible)
    {
        if (panel == null) return;

        panel.gameObject.SetActive(true); // Ensure GameObject is active
        panel.alpha = visible ? 1f : 0f;
        panel.interactable = visible;
        panel.blocksRaycasts = visible;
    }

    #region Event Handlers
    private void HandleGameStart() => ShowHUD();
    private void HandleGameOver() => ShowGameOver();
    private void HandleReturnToLobby() => ShowLobby();
    #endregion
}
