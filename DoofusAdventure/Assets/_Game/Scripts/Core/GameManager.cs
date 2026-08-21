using UnityEngine;

/// <summary>
/// Central game coordinator managing states: StartScreen, Playing, GameOver, Rewinding, Lobby.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState
    {
        StartScreen,
        Playing,
        GameOver,
        Rewinding,
        Lobby
    }

    [Header("Current State (Read-Only)")]
    [SerializeField] private GameState currentState = GameState.StartScreen;

    public GameState CurrentState => currentState;
    public bool IsPlaying => currentState == GameState.Playing;
    public bool IsGameOver => currentState == GameState.GameOver;
    public bool IsRewinding => currentState == GameState.Rewinding;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureGameManagersExist();
    }

    private void EnsureGameManagersExist()
    {
        if (FindFirstObjectByType<ScoreManager>() == null)
            new GameObject("ScoreManager").AddComponent<ScoreManager>();

        if (FindFirstObjectByType<VFXManager>() == null)
            new GameObject("VFXManager").AddComponent<VFXManager>();

        if (FindFirstObjectByType<SoundManager>() == null)
            new GameObject("SoundManager").AddComponent<SoundManager>();

        if (FindFirstObjectByType<CuteEnvironmentManager>() == null)
            new GameObject("CuteEnvironmentManager").AddComponent<CuteEnvironmentManager>();
    }

    private void OnEnable()
    {
        GameEvents.OnGameStart += HandleGameStart;
        GameEvents.OnGameOver += HandleGameOver;
        GameEvents.OnGameRestart += HandleGameRestart;
        GameEvents.OnReturnToLobby += HandleReturnToLobby;
        GameEvents.OnDoofusFell += HandleDoofusFell;
        GameEvents.OnRewindStart += HandleRewindStart;
        GameEvents.OnRewindComplete += HandleRewindComplete;
    }

    private void OnDisable()
    {
        GameEvents.OnGameStart -= HandleGameStart;
        GameEvents.OnGameOver -= HandleGameOver;
        GameEvents.OnGameRestart -= HandleGameStart;
        GameEvents.OnReturnToLobby -= HandleReturnToLobby;
        GameEvents.OnDoofusFell -= HandleDoofusFell;
        GameEvents.OnRewindStart -= HandleRewindStart;
        GameEvents.OnRewindComplete -= HandleRewindComplete;
    }

    private void Start()
    {
        SetState(GameState.StartScreen);
    }

    public void SetState(GameState newState)
    {
        currentState = newState;
        Debug.Log($"[GameManager] State changed to: {newState}");
    }

    public void StartGame()
    {
        SetState(GameState.Playing);
        GameEvents.TriggerGameStart();
    }

    public void RestartGame()
    {
        SetState(GameState.Playing);
        GameEvents.TriggerGameRestart();
    }

    private void HandleGameStart() => SetState(GameState.Playing);
    private void HandleGameOver() => SetState(GameState.GameOver);
    private void HandleGameRestart() => SetState(GameState.Playing);
    private void HandleReturnToLobby() => SetState(GameState.Lobby);

    private void HandleDoofusFell()
    {
        Debug.Log("[GameManager] Doofus fell into the abyss! Triggering Game Over.");
        SetState(GameState.GameOver);
        GameEvents.TriggerGameOver();
    }

    private void HandleRewindStart() => SetState(GameState.Rewinding);
    private void HandleRewindComplete() => SetState(GameState.Playing);
}
