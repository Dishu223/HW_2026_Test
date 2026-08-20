using UnityEngine;

/// <summary>
/// Controls overall game lifecycle states:
/// StartScreen (paused) -> Playing (unpaused) -> GameOver.
/// Ensures resuming from Rewind continues active run without resetting world state.
/// </summary>
public class GameManager : MonoBehaviour
{
    private static GameManager instance;
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<GameManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("GameManager");
                    instance = go.AddComponent<GameManager>();
                }
            }
            return instance;
        }
    }

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
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
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
                Time.timeScale = 0f;
                break;

            case GameState.Lobby:
                Time.timeScale = 0f;
                GameEvents.TriggerReturnToLobby();
                break;

            case GameState.Playing:
                Time.timeScale = 1f;
                break;

            case GameState.Rewinding:
                Time.timeScale = 1f;
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
        GameEvents.TriggerGameStart(); // Only trigger fresh start on user play/restart!
    }

    public void RestartGame()
    {
        SetState(GameState.Playing);
        GameEvents.TriggerGameStart();
    }

    private void HandleDoofusFell()
    {
        SetState(GameState.GameOver);
    }

    private void HandleRewindStart()
    {
        SetState(GameState.Rewinding);
    }

    private void HandleRewindComplete()
    {
        // Resume active gameplay state WITHOUT restarting the game or resetting platforms/score!
        currentState = GameState.Playing;
        Time.timeScale = 1f;
    }
}
