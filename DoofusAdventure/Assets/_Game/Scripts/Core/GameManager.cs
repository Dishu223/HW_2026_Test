using UnityEngine;

/// <summary>
/// Controls overall game lifecycle states:
/// StartScreen -> Lobby -> Playing -> Rewinding -> GameOver.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState
    {
        StartScreen,
        Lobby,
        Playing,
        Rewinding,
        GameOver
    }

    [Header("Current State")]
    [SerializeField] private GameState currentState = GameState.StartScreen;

    public GameState CurrentState => currentState;
    public bool IsPlaying => currentState == GameState.Playing;

    private bool isTransitioning = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Default directly to Playing during early development/testing
        SetState(GameState.Playing);
    }

    private void OnEnable()
    {
        GameEvents.OnGameStart += HandleGameStart;
        GameEvents.OnGameOver += HandleGameOver;
        GameEvents.OnGameRestart += HandleGameRestart;
        GameEvents.OnDoofusFell += HandleDoofusFell;
        GameEvents.OnRewindStart += HandleRewindStart;
        GameEvents.OnRewindComplete += HandleRewindComplete;
        GameEvents.OnReturnToLobby += HandleReturnToLobby;
    }

    private void OnDisable()
    {
        GameEvents.OnGameStart -= HandleGameStart;
        GameEvents.OnGameOver -= HandleGameOver;
        GameEvents.OnGameRestart -= HandleGameRestart;
        GameEvents.OnDoofusFell -= HandleDoofusFell;
        GameEvents.OnRewindStart -= HandleRewindStart;
        GameEvents.OnRewindComplete -= HandleRewindComplete;
        GameEvents.OnReturnToLobby -= HandleReturnToLobby;
    }

    public void SetState(GameState newState)
    {
        if (currentState == newState && !isTransitioning) return;

        currentState = newState;
        Debug.Log($"[GameManager] State changed to: {currentState}");

        switch (currentState)
        {
            case GameState.StartScreen:
                Time.timeScale = 1f;
                break;

            case GameState.Lobby:
                Time.timeScale = 1f;
                break;

            case GameState.Playing:
                Time.timeScale = 1f;
                GameEvents.TriggerGameStart();
                break;

            case GameState.Rewinding:
                // Time scale handled dynamically during rewind playback
                break;

            case GameState.GameOver:
                Time.timeScale = 1f;
                GameEvents.TriggerGameOver();
                break;
        }
    }

    #region Event Handlers
    private void HandleGameStart() => SetState(GameState.Playing);

    private void HandleGameOver() => SetState(GameState.GameOver);

    private void HandleGameRestart() => SetState(GameState.Playing);

    private void HandleReturnToLobby() => SetState(GameState.Lobby);

    private void HandleDoofusFell()
    {
        // When Doofus falls, if rewinds are available we rewind, else Game Over
        // For now without RewindManager active, transition directly to Game Over
        SetState(GameState.GameOver);
    }

    private void HandleRewindStart() => SetState(GameState.Rewinding);

    private void HandleRewindComplete() => SetState(GameState.Playing);
    #endregion
}
