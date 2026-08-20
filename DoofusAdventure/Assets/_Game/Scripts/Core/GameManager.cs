using UnityEngine;

/// <summary>
/// Controls overall game lifecycle states:
/// StartScreen (paused) -> Playing (unpaused) -> GameOver.
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
        SetState(GameState.StartScreen);
    }

    private void OnEnable()
    {
        GameEvents.OnDoofusFell += HandleDoofusFell;
        GameEvents.OnRewindStart += HandleRewindStart;
        GameEvents.OnRewindComplete += HandleRewindComplete;
    }

    private void OnDisable()
    {
        GameEvents.OnDoofusFell -= HandleDoofusFell;
        GameEvents.OnRewindStart -= HandleRewindStart;
        GameEvents.OnRewindComplete -= HandleRewindComplete;
    }

    public void SetState(GameState newState)
    {
        currentState = newState;
        Debug.Log($"[GameManager] State changed to: {currentState}");

        switch (currentState)
        {
            case GameState.StartScreen:
                Time.timeScale = 0f; // Freeze game when on Start Screen!
                break;

            case GameState.Lobby:
                Time.timeScale = 0f;
                GameEvents.TriggerReturnToLobby();
                break;

            case GameState.Playing:
                Time.timeScale = 1f; // Unpause for active gameplay!
                GameEvents.TriggerGameStart();
                break;

            case GameState.Rewinding:
                break;

            case GameState.GameOver:
                Time.timeScale = 1f;
                GameEvents.TriggerGameOver();
                break;
        }
    }

    public void StartGame()
    {
        SetState(GameState.Playing);
    }

    public void RestartGame()
    {
        SetState(GameState.Playing);
    }

    private void HandleDoofusFell()
    {
        SetState(GameState.GameOver);
    }

    private void HandleRewindStart() => SetState(GameState.Rewinding);
    private void HandleRewindComplete() => SetState(GameState.Playing);
}
