using UnityEngine;

/// <summary>
/// Central Game State Machine and Coordinator.
/// Ensures all managers (ScoreManager, SoundManager, VFXManager, RewindManager, CuteEnvironmentManager) are active and synchronized.
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

    [Header("Game State")]
    [SerializeField] private GameState currentState = GameState.StartScreen;

    public GameState CurrentState => currentState;
    public bool IsPlaying => currentState == GameState.Playing;
    public bool IsStartScreen => currentState == GameState.StartScreen;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (FindAnyObjectByType<CuteEnvironmentManager>() == null)
        {
            GameObject envObj = new GameObject("CuteEnvironmentManager");
            envObj.AddComponent<CuteEnvironmentManager>();
        }

        if (FindAnyObjectByType<VFXManager>() == null)
        {
            GameObject vfxObj = new GameObject("VFXManager");
            vfxObj.AddComponent<VFXManager>();
        }

        if (FindAnyObjectByType<SoundManager>() == null)
        {
            GameObject soundObj = new GameObject("SoundManager");
            soundObj.AddComponent<SoundManager>();
        }
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
        GameEvents.OnGameRestart -= HandleGameRestart;
        GameEvents.OnReturnToLobby -= HandleReturnToLobby;
        GameEvents.OnDoofusFell -= HandleDoofusFell;
        GameEvents.OnRewindStart -= HandleRewindStart;
        GameEvents.OnRewindComplete -= HandleRewindComplete;
    }

    private void Start()
    {
        // Boot directly in StartScreen state
        SetState(GameState.StartScreen);
    }

    public void SetState(GameState newState)
    {
        currentState = newState;
        Debug.Log($"[GameManager] State changed to: {newState}");
    }

    private void HandleGameStart() => SetState(GameState.Playing);
    private void HandleGameOver(int finalScore) => SetState(GameState.GameOver);
    private void HandleGameRestart() => SetState(GameState.Playing);
    private void HandleReturnToLobby() => SetState(GameState.Lobby);
    private void HandleDoofusFell() => Debug.Log("[GameManager] Doofus fell into the abyss!");
    private void HandleRewindStart() => SetState(GameState.Rewinding);
    private void HandleRewindComplete() => SetState(GameState.Playing);
}
