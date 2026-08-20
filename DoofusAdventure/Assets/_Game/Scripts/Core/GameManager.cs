using UnityEngine;

/// <summary>
/// Central Game State Machine and Coordinator.
/// Ensures all managers (ScoreManager, SoundManager, VFXManager, RewindManager) are active and synchronized.
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

    public void SetState(GameState newState)
    {
        if (currentState == newState) return;

        currentState = newState;
        Debug.Log($"[GameManager] State changed to: {newState}");

        switch (newState)
        {
            case GameState.StartScreen:
                break;
            case GameState.Playing:
                break;
            case GameState.Rewinding:
                break;
            case GameState.GameOver:
                GameEvents.TriggerGameOver();
                break;
        }
    }

    private void HandleGameStart() => SetState(GameState.Playing);
    private void HandleGameOver() => SetState(GameState.GameOver);
    private void HandleGameRestart() => SetState(GameState.Playing);
    private void HandleReturnToLobby() => SetState(GameState.StartScreen);
    private void HandleDoofusFell() => SetState(GameState.GameOver);
    private void HandleRewindStart() => SetState(GameState.Rewinding);
    private void HandleRewindComplete() => SetState(GameState.Playing);
}
