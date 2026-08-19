using System.Collections;
using UnityEngine;

// Central UI router. Smoothly fades and transitions between UI panels
// based on game states (Start Screen, Lobby, HUD, Rewind, and GameOver).
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Canvas Panels")]
    [SerializeField] private CanvasGroup startScreenPanel;
    [SerializeField] private CanvasGroup lobbyPanel;
    [SerializeField] private CanvasGroup hudPanel;
    [SerializeField] private CanvasGroup rewindPanel;
    [SerializeField] private CanvasGroup gameOverPanel;

    [Header("Transition Settings")]
    [SerializeField] private float fadeDuration = 0.25f;

    private CanvasGroup activePanel;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        GameEvents.OnGameStart += ShowHUD;
        GameEvents.OnGameOver += ShowGameOver;
        GameEvents.OnRewindStart += ShowRewind;
        GameEvents.OnRewindComplete += ShowHUD;
        GameEvents.OnReturnToMenu += ShowStartScreen;
    }

    private void OnDisable()
    {
        GameEvents.OnGameStart -= ShowHUD;
        GameEvents.OnGameOver -= ShowGameOver;
        GameEvents.OnRewindStart -= ShowRewind;
        GameEvents.OnRewindComplete -= ShowHUD;
        GameEvents.OnReturnToMenu -= ShowStartScreen;
    }

    private void Start()
    {
        // Default to Start Screen on launch
        ShowStartScreen();
    }

    public void ShowStartScreen()
    {
        SwitchToPanel(startScreenPanel);
    }

    public void ShowLobby()
    {
        SwitchToPanel(lobbyPanel);
    }

    public void ShowHUD()
    {
        SwitchToPanel(hudPanel);
    }

    public void ShowRewind()
    {
        SwitchToPanel(rewindPanel);
    }

    public void ShowGameOver()
    {
        SwitchToPanel(gameOverPanel);
    }

    private void SwitchToPanel(CanvasGroup targetPanel)
    {
        if (targetPanel == null) return;

        // Hide all other panels
        HideInstant(startScreenPanel);
        HideInstant(lobbyPanel);
        HideInstant(hudPanel);
        HideInstant(rewindPanel);
        HideInstant(gameOverPanel);

        // Fade in target panel
        StartCoroutine(FadeIn(targetPanel));
        activePanel = targetPanel;
    }

    private IEnumerator FadeIn(CanvasGroup panel)
    {
        if (panel == null) yield break;

        panel.gameObject.SetActive(true);
        panel.interactable = true;
        panel.blocksRaycasts = true;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime; // Unscaled so fades work even in slow-mo/pause
            panel.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }
        panel.alpha = 1f;
    }

    private void HideInstant(CanvasGroup panel)
    {
        if (panel == null) return;
        panel.alpha = 0f;
        panel.interactable = false;
        panel.blocksRaycasts = false;
        panel.gameObject.SetActive(false);
    }
}
