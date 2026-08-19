using UnityEngine;

// Tracks the player score, checks for milestone celebrations (10, 25, 50),
// and manages high score persistence with PlayerPrefs.
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Score Stats")]
    [SerializeField] private int currentScore = 0;
    [SerializeField] private int highScore = 0;

    private const string HIGH_SCORE_KEY = "Doofus_HighScore";
    private readonly int[] milestones = new int[] { 10, 25, 50 };

    public int CurrentScore => currentScore;
    public int HighScore => highScore;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        LoadHighScore();
    }

    private void OnEnable()
    {
        GameEvents.OnPulpitLanded += IncrementScore;
        GameEvents.OnGameStart += ResetCurrentScore;
        GameEvents.OnGameRestart += ResetCurrentScore;
    }

    private void OnDisable()
    {
        GameEvents.OnPulpitLanded -= IncrementScore;
        GameEvents.OnGameStart -= ResetCurrentScore;
        GameEvents.OnGameRestart -= ResetCurrentScore;
    }

    private void IncrementScore()
    {
        currentScore++;
        GameEvents.TriggerScoreChanged(currentScore);

        // Check if we hit any exciting milestone targets
        foreach (int m in milestones)
        {
            if (currentScore == m)
            {
                GameEvents.TriggerMilestoneReached(m);
                Debug.Log($"<color=orange>[ScoreManager]</color> <b>Milestone reached: {m} Pulpits!</b>");
                break;
            }
        }

        // Update high score if beaten
        if (currentScore > highScore)
        {
            highScore = currentScore;
            SaveHighScore();
        }
    }

    private void ResetCurrentScore()
    {
        currentScore = 0;
        GameEvents.TriggerScoreChanged(currentScore);
    }

    public void SetScoreDirectly(int score)
    {
        currentScore = Mathf.Max(0, score);
        GameEvents.TriggerScoreChanged(currentScore);
    }

    private void LoadHighScore()
    {
        highScore = PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);
    }

    private void SaveHighScore()
    {
        PlayerPrefs.SetInt(HIGH_SCORE_KEY, highScore);
        PlayerPrefs.Save();
    }
}
