using UnityEngine;

/// <summary>
/// Manages player score progression, milestone triggers (10, 25, 50),
/// and high score persistence via PlayerPrefs.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Score Configuration")]
    [SerializeField] private int targetGoal = 50;

    private int currentScore = 0;
    private int highScore = 0;
    private const string HIGH_SCORE_KEY = "DoofusHighScore";

    public int CurrentScore => currentScore;
    public int HighScore => highScore;
    public int TargetGoal => targetGoal;
    public bool IsGoalCompleted => currentScore >= targetGoal;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Load saved high score from local storage
        highScore = PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);
    }

    private void OnEnable()
    {
        GameEvents.OnPulpitLanded += HandlePulpitLanded;
        GameEvents.OnGameStart += ResetScore;
        GameEvents.OnGameRestart += ResetScore;
        GameEvents.OnGameOver += HandleGameOver;
    }

    private void OnDisable()
    {
        GameEvents.OnPulpitLanded -= HandlePulpitLanded;
        GameEvents.OnGameStart -= ResetScore;
        GameEvents.OnGameRestart -= ResetScore;
        GameEvents.OnGameOver -= HandleGameOver;
    }

    private void HandlePulpitLanded()
    {
        currentScore++;
        GameEvents.TriggerScoreChanged(currentScore);

        // Check milestones
        if (currentScore == 10 || currentScore == 25 || currentScore == 50)
        {
            GameEvents.TriggerMilestoneReached(currentScore);
        }

        // Update high score in real-time if beaten
        if (currentScore > highScore)
        {
            highScore = currentScore;
        }
    }

    public void ResetScore()
    {
        currentScore = 0;
        GameEvents.TriggerScoreChanged(currentScore);
    }

    private void HandleGameOver()
    {
        // Persist best score to PlayerPrefs
        if (currentScore >= highScore)
        {
            highScore = currentScore;
            PlayerPrefs.SetInt(HIGH_SCORE_KEY, highScore);
            PlayerPrefs.Save();
        }
    }
}
