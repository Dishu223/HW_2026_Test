using System;
using System.IO;
using UnityEngine;

// Data structures matching the assignment JSON specification
[Serializable]
public class PlayerData
{
    public float speed = 3f;
}

[Serializable]
public class PulpitData
{
    public float min_pulpit_destroy_time = 4f;
    public float max_pulpit_destroy_time = 5f;
    public float pulpit_spawn_time = 2.5f;
}

[Serializable]
public class GameConfigData
{
    public PlayerData player_data = new PlayerData();
    public PulpitData pulpit_data = new PulpitData();
}

// Reads and parses game_data.json at runtime
public class GameConfig : MonoBehaviour
{
    public static GameConfig Instance { get; private set; }

    [Header("Configuration Data")]
    [SerializeField] private GameConfigData configData = new GameConfigData();

    public float PlayerSpeed => configData.player_data.speed;
    public float MinDestroyTime => configData.pulpit_data.min_pulpit_destroy_time;
    public float MaxDestroyTime => configData.pulpit_data.max_pulpit_destroy_time;
    public float PulpitSpawnTime => configData.pulpit_data.pulpit_spawn_time;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            LoadConfig();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadConfig()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, "game_data.json");

        if (File.Exists(filePath))
        {
            try
            {
                string jsonText = File.ReadAllText(filePath);
                GameConfigData loaded = JsonUtility.FromJson<GameConfigData>(jsonText);

                if (loaded != null && loaded.player_data != null && loaded.pulpit_data != null)
                {
                    configData = loaded;
                    ValidateAndSanitizeData();
                    Debug.Log($"[GameConfig] Successfully loaded configuration from {filePath}");
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GameConfig] Error parsing {filePath}: {ex.Message}. Using default values.");
            }
        }
        else
        {
            Debug.LogWarning($"[GameConfig] Config file not found at {filePath}. Using default values.");
        }

        // Fallback safety
        ValidateAndSanitizeData();
    }

    private void ValidateAndSanitizeData()
    {
        // Prevent zero or negative speeds
        if (configData.player_data.speed <= 0) configData.player_data.speed = 3f;

        // Ensure min destroy time is positive and doesn't exceed max
        if (configData.pulpit_data.min_pulpit_destroy_time <= 0) configData.pulpit_data.min_pulpit_destroy_time = 4f;
        if (configData.pulpit_data.max_pulpit_destroy_time < configData.pulpit_data.min_pulpit_destroy_time)
        {
            configData.pulpit_data.max_pulpit_destroy_time = configData.pulpit_data.min_pulpit_destroy_time + 1f;
        }

        // Spawn time safety
        if (configData.pulpit_data.pulpit_spawn_time <= 0) configData.pulpit_data.pulpit_spawn_time = 2.5f;
    }
}
