using System.Collections;
using UnityEngine;

/// <summary>
/// Master UI Router managing panel switching (Start Screen, Lobby, HUD, Game Over)
/// with smooth alpha fade transitions.
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
    [SerializeField] private float fadeDuration = 0.2f;

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
        // Always guarantee Start Screen is active on boot
        ShowPanelImmediate(startScreenPanel);
    }

    private void OnEnable()
    {
        GameEvents.OnGameStart += HandleGameStart;
        GameEvents.OnGameOver += HandleGameOver;
        GameEvents.OnReturnToLobby += HandleReturnToLobby;
    }

    private void OnDisable()
    {
        GameEvents.OnGameStart -= HandleGameStart;
        GameEvents.OnGameOver -= HandleGameOver;
        GameEvents.OnReturnToLobby -= HandleReturnToLobby;
    }

    public void SwitchToPanel(CanvasGroup targetPanel)
    {
        if (targetPanel == null) return;
        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        transitionRoutine = StartCoroutine(TransitionPanelsCoroutine(targetPanel));
    }

    private IEnumerator TransitionPanelsCoroutine(CanvasGroup targetPanel)
    {
        if (activePanel != null && activePanel != targetPanel)
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                activePanel.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                yield return null;
            }
            SetPanelState(activePanel, false);
        }

        if (targetPanel != null)
        {
            SetPanelState(targetPanel, true);
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                targetPanel.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
                yield return null;
            }
            targetPanel.alpha = 1f;
        }

        activePanel = targetPanel;
    }

    public void ShowPanelImmediate(CanvasGroup panel)
    {
        SetPanelState(startScreenPanel, false);
        SetPanelState(lobbyPanel, false);
        SetPanelState(hudPanel, false);
        SetPanelState(gameOverPanel, false);

        if (panel != null)
        {
            SetPanelState(panel, true);
            activePanel = panel;
        }
    }

    private void SetPanelState(CanvasGroup panel, bool visible)
    {
        if (panel == null) return;
        panel.alpha = visible ? 1f : 0f;
        panel.interactable = visible;
        panel.blocksRaycasts = visible;
    }

    #region Event Handlers
    private void HandleGameStart() => SwitchToPanel(hudPanel);
    private void HandleGameOver() => SwitchToPanel(gameOverPanel);
    private void HandleReturnToLobby() => SwitchToPanel(lobbyPanel);
    #endregion
}
