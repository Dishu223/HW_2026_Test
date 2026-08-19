using System.IO;
using UnityEngine;

// Holds player-specific configuration values from JSON
[System.Serializable]
public class PlayerData
{
    public float speed = 3f;
}

// Holds platform-specific configuration values from JSON
[System.Serializable]
public class PulpitData
{
    public float min_pulpit_destroy_time = 4f;
    public float max_pulpit_destroy_time = 5f;
    public float pulpit_spawn_time = 2.5f;
}

// Root container mapping to game_data.json
[System.Serializable]
public class GameData
{
    public PlayerData player_data = new PlayerData();
    public PulpitData pulpit_data = new PulpitData();
}

// Reads and validates game_data.json from StreamingAssets.
// Falls back to safe defaults if the file is missing, empty, or has bad numbers.
public class GameConfig : MonoBehaviour
{
    public static GameConfig Instance { get; private set; }

    [Header("Configured Game Data")]
    [SerializeField] private GameData gameData = new GameData();

    // Convenience accessors
    public float PlayerSpeed => gameData.player_data.speed;
    public float MinDestroyTime => gameData.pulpit_data.min_pulpit_destroy_time;
    public float MaxDestroyTime => gameData.pulpit_data.max_pulpit_destroy_time;
    public float PulpitSpawnTime => gameData.pulpit_data.pulpit_spawn_time;

    private void Awake()
    {
        // Simple singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadConfiguration();
    }

    // Loads JSON from StreamingAssets and validates values
    public void LoadConfiguration()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, "game_data.json");

        if (File.Exists(filePath))
        {
            try
            {
                string jsonText = File.ReadAllText(filePath);
                GameData loadedData = JsonUtility.FromJson<GameData>(jsonText);

                if (loadedData != null && loadedData.player_data != null && loadedData.pulpit_data != null)
                {
                    gameData = loadedData;
                    Debug.Log("<color=green>[GameConfig]</color> Loaded game_data.json successfully.");
                }
                else
                {
                    Debug.LogWarning("[GameConfig] JSON was missing sections. Using fallback defaults.");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[GameConfig] Failed to parse JSON: {ex.Message}. Using fallback defaults.");
            }
        }
        else
        {
            Debug.LogWarning($"[GameConfig] game_data.json not found at '{filePath}'. Using built-in defaults.");
        }

        ValidateAndClampData();
    }

    // Protect against edge cases like negative speeds or impossible timers
    private void ValidateAndClampData()
    {
        // 1. Ensure player speed is at least reasonable
        if (gameData.player_data.speed <= 0.1f)
        {
            Debug.LogWarning("[GameConfig] Player speed was <= 0. Clamping to minimum 0.5.");
            gameData.player_data.speed = 0.5f;
        }

        // 2. Ensure min destroy time is positive
        if (gameData.pulpit_data.min_pulpit_destroy_time <= 0.5f)
        {
            gameData.pulpit_data.min_pulpit_destroy_time = 0.5f;
        }

        // 3. Ensure max destroy time is at least equal to min destroy time
        if (gameData.pulpit_data.max_pulpit_destroy_time < gameData.pulpit_data.min_pulpit_destroy_time)
        {
            Debug.LogWarning("[GameConfig] Max destroy time was smaller than Min destroy time. Swapping values.");
            float temp = gameData.pulpit_data.min_pulpit_destroy_time;
            gameData.pulpit_data.min_pulpit_destroy_time = gameData.pulpit_data.max_pulpit_destroy_time;
            gameData.pulpit_data.max_pulpit_destroy_time = temp;
        }

        // 4. Ensure spawn time allows a player to actually reach the next platform
        if (gameData.pulpit_data.pulpit_spawn_time <= 0.2f)
        {
            gameData.pulpit_data.pulpit_spawn_time = 0.5f;
        }
        else if (gameData.pulpit_data.pulpit_spawn_time > gameData.pulpit_data.min_pulpit_destroy_time)
        {
            Debug.LogWarning("[GameConfig] Pulpit spawn time was greater than destroy time! Clamping to 80% of min destroy time.");
            gameData.pulpit_data.pulpit_spawn_time = gameData.pulpit_data.min_pulpit_destroy_time * 0.8f;
        }
    }
}
