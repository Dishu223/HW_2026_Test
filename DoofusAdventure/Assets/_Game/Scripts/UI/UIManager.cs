using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Master UI Router managing screen switching.
/// Auto-detects panels if Inspector links are broken.
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

        // Auto-Recovery: If Unity cleared the fields when we changed types, automatically find them!
        if (startScreenPanel == null)
        {
            Transform found = transform.Find("StartScreen_Panel");
            if (found != null) startScreenPanel = found.gameObject;
        }
        
        if (hudPanel == null)
        {
            Transform found = transform.Find("HUD_Panel");
            if (found != null) hudPanel = found.gameObject;
        }
        
        if (gameOverPanel == null)
        {
            Transform found = transform.Find("GameOver_Panel");
            if (found != null) gameOverPanel = found.gameObject;
        }

        // Failsafe: Ensure Root CanvasGroup is fully visible if it exists
        CanvasGroup rootCG = GetComponent<CanvasGroup>();
        if (rootCG != null)
        {
            rootCG.alpha = 1f;
            rootCG.interactable = true;
            rootCG.blocksRaycasts = true;
        }

        // Failsafe: Ensure Canvas has correct Render Mode
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.enabled = true;
        }
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
        if (panel == null)
        {
            Debug.LogWarning("[UIManager] Tried to set a panel active, but it is NULL! Ensure names match perfectly.");
            return;
        }

        panel.SetActive(active);

        // Ensure canvas group states match
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
