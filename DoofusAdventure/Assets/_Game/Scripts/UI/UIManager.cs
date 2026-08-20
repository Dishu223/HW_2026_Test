using UnityEngine;

/// <summary>
/// Master UI Router managing screen switching.
/// Directly toggles panel GameObjects (Start Screen, HUD, Game Over)
/// so inactive screens cannot intercept input or run background Update loops.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Panels")]
    [SerializeField] private GameObject startScreenPanel;
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject gameOverPanel;

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
        SetPanelActive(startScreenPanel, true);
        SetPanelActive(lobbyPanel, false);
        SetPanelActive(hudPanel, false);
        SetPanelActive(gameOverPanel, false);
    }

    public void ShowHUD()
    {
        SetPanelActive(startScreenPanel, false);
        SetPanelActive(lobbyPanel, false);
        SetPanelActive(gameOverPanel, false);
        SetPanelActive(hudPanel, true);
    }

    public void ShowGameOver()
    {
        SetPanelActive(startScreenPanel, false);
        SetPanelActive(lobbyPanel, false);
        SetPanelActive(hudPanel, false);
        SetPanelActive(gameOverPanel, true);
    }

    public void ShowLobby()
    {
        SetPanelActive(startScreenPanel, false);
        SetPanelActive(lobbyPanel, true);
        SetPanelActive(hudPanel, false);
        SetPanelActive(gameOverPanel, false);
    }

    private void SetPanelActive(GameObject panel, bool active)
    {
        if (panel == null) return;

        panel.SetActive(active);

        // If it has a CanvasGroup, ensure it is fully opaque and interactive
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = active ? 1f : 0f;
            cg.interactable = active;
            cg.blocksRaycasts = active;
        }
    }

    #region Event Handlers
    private void HandleGameStart() => ShowHUD();
    private void HandleGameOver() => ShowGameOver();
    private void HandleReturnToLobby() => ShowLobby();
    #endregion
}
