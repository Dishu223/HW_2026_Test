using UnityEngine;

public enum GameState
{
    Menu,
    Playing,
    GameOver
}

// Manages game state lifecycle and coordinates session start, restart, and game over
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Current State")]
    [SerializeField] private GameState currentState = GameState.Menu;

    public GameState CurrentState => currentState;
    public bool IsPlaying => currentState == GameState.Playing;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        GameEvents.OnDoofusFell += HandlePlayerFell;
    }

    private void OnDisable()
    {
        GameEvents.OnDoofusFell -= HandlePlayerFell;
    }

    private void Start()
    {
        // Start directly in Playing mode for Level 1 testing, or through UI in later levels
        StartGame();
    }

    public void StartGame()
    {
        currentState = GameState.Playing;
        GameEvents.TriggerGameStart();
    }

    private void HandlePlayerFell()
    {
        if (currentState != GameState.Playing) return;

        currentState = GameState.GameOver;
        GameEvents.TriggerGameOver();
        Debug.Log("[GameManager] Doofus has fallen! Game Over.");
    }

    public void RestartGame()
    {
        currentState = GameState.Playing;
        GameEvents.TriggerGameRestart();
    }
}
