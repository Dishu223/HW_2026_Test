using UnityEngine;

/// <summary>
/// Manages player score progression, milestone triggers (10, 25, 50 or custom targetGoal),
/// and high score persistence via PlayerPrefs.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Score Configuration")]
    [Tooltip("Target goal to trigger victory celebration. Set to 5 for quick testing or 50 for full challenge!")]
    [SerializeField] private int targetGoal = 5;

    private int currentScore = 0;
    private int highScore = 0;
    private const string HIGH_SCORE_KEY = "DoofusHighScore";

    public int CurrentScore => currentScore;
    public int HighScore => highScore;
    public int TargetGoal
    {
        get => targetGoal;
        set => targetGoal = value;
    }
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

        // Check milestones or target goal completion (e.g. 5, 10, 25, 50)
        if (currentScore == targetGoal || currentScore == 10 || currentScore == 25 || currentScore == 50)
        {
            Debug.Log($"[ScoreManager] Milestone reached: {currentScore}! Victory goal: {targetGoal}");
            GameEvents.TriggerMilestoneReached(currentScore);
        }

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
        if (currentScore >= highScore)
        {
            highScore = currentScore;
            PlayerPrefs.SetInt(HIGH_SCORE_KEY, highScore);
            PlayerPrefs.Save();
        }
    }
}
