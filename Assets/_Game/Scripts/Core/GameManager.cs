using UnityEngine;

public enum GameState
{
    StartScreen,
    Lobby,
    Playing,
    Rewinding,
    GameOver
}

// Controls the core gameplay loop and state transitions.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Current State")]
    [SerializeField] private GameState currentState = GameState.StartScreen;
    public GameState CurrentState => currentState;

    [Header("Rewind Settings")]
    [SerializeField] private int maxRewindsPerRun = 2;
    private int rewindsRemaining;
    public int RewindsRemaining => rewindsRemaining;

    // Prevents double-triggers while transitions or animations are happening
    private bool isTransitioning = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        rewindsRemaining = maxRewindsPerRun;
    }

    private void OnEnable()
    {
        GameEvents.OnDoofusFell += HandlePlayerFell;
        GameEvents.OnRewindComplete += HandleRewindFinished;
        GameEvents.OnGameRestart += RestartGame;
    }

    private void OnDisable()
    {
        GameEvents.OnDoofusFell -= HandlePlayerFell;
        GameEvents.OnRewindComplete -= HandleRewindFinished;
        GameEvents.OnGameRestart -= RestartGame;
    }

    private void Start()
    {
        SetState(GameState.StartScreen);
    }

    private void Update()
    {
        // Global keyboard shortcuts for quick testing and accessibility
        if (currentState == GameState.StartScreen && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)))
        {
            OpenLobby();
        }
        else if (currentState == GameState.GameOver && Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
        }
    }

    public void OpenLobby()
    {
        if (isTransitioning) return;
        SetState(GameState.Lobby);
    }

    public void StartGame()
    {
        if (isTransitioning) return;
        rewindsRemaining = maxRewindsPerRun;
        SetState(GameState.Playing);
        GameEvents.TriggerGameStart();
    }

    public void RestartGame()
    {
        rewindsRemaining = maxRewindsPerRun;
        SetState(GameState.Playing);
        GameEvents.TriggerGameRestart();
        GameEvents.TriggerGameStart();
    }

    public void ReturnToStart()
    {
        SetState(GameState.StartScreen);
        GameEvents.TriggerReturnToMenu();
    }

    private void HandlePlayerFell()
    {
        if (currentState != GameState.Playing) return;

        // Check if player has rewind charges available
        if (rewindsRemaining > 0)
        {
            rewindsRemaining--;
            SetState(GameState.Rewinding);
            GameEvents.TriggerRewindStart();
        }
        else
        {
            SetState(GameState.GameOver);
            GameEvents.TriggerGameOver();
        }
    }

    private void HandleRewindFinished()
    {
        if (currentState == GameState.Rewinding)
        {
            SetState(GameState.Playing);
        }
    }

    private void SetState(GameState newState)
    {
        currentState = newState;
        Debug.Log($"<color=cyan>[GameManager]</color> State changed to: <b>{newState}</b>");
    }
}
