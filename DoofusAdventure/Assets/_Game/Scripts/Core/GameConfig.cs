using System;
using System.IO;
using UnityEngine;

#region Data Transfer Objects (Matching JSON Structure)
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
public class GameDataRoot
{
    public PlayerData player_data = new PlayerData();
    public PulpitData pulpit_data = new PulpitData();
}
#endregion

public class GameConfig : MonoBehaviour
{
    public static GameConfig Instance { get; private set; }

    [Header("Loaded Configuration")]
    [SerializeField] private GameDataRoot configData = new GameDataRoot();

    public float PlayerSpeed => configData.player_data.speed;
    public float MinDestroyTime => configData.pulpit_data.min_pulpit_destroy_time;
    public float MaxDestroyTime => configData.pulpit_data.max_pulpit_destroy_time;
    public float SpawnTime => configData.pulpit_data.pulpit_spawn_time;

    private const string JSON_FILENAME = "game_data.json";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadConfig();
    }

    public void LoadConfig()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, JSON_FILENAME);

        if (File.Exists(filePath))
        {
            try
            {
                string jsonContent = File.ReadAllText(filePath);
                GameDataRoot loadedData = JsonUtility.FromJson<GameDataRoot>(jsonContent);

                if (loadedData != null && loadedData.player_data != null && loadedData.pulpit_data != null)
                {
                    configData = loadedData;
                    SanitizeData();
                    Debug.Log($"[GameConfig] Successfully loaded config from: {filePath}");
                    return;
                }
                else
                {
                    Debug.LogWarning("[GameConfig] JSON parsed but contained null fields. Using fallback defaults.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameConfig] Failed to parse JSON file! Exception: {ex.Message}. Using fallback defaults.");
            }
        }
        else
        {
            Debug.LogWarning($"[GameConfig] Config file not found at '{filePath}'. Using hardcoded fallback defaults.");
        }

        configData = new GameDataRoot();
        SanitizeData();
    }

    private void SanitizeData()
    {
        if (configData.player_data.speed <= 0f)
        {
            Debug.LogWarning($"[GameConfig] Invalid speed ({configData.player_data.speed}). Clamping to minimum 1.0.");
            configData.player_data.speed = 1f;
        }

        if (configData.pulpit_data.min_pulpit_destroy_time <= 0f)
            configData.pulpit_data.min_pulpit_destroy_time = 1f;

        if (configData.pulpit_data.max_pulpit_destroy_time <= 0f)
            configData.pulpit_data.max_pulpit_destroy_time = 2f;

        if (configData.pulpit_data.min_pulpit_destroy_time > configData.pulpit_data.max_pulpit_destroy_time)
        {
            Debug.LogWarning("[GameConfig] min_pulpit_destroy_time was greater than max. Swapping them.");
            float temp = configData.pulpit_data.min_pulpit_destroy_time;
            configData.pulpit_data.min_pulpit_destroy_time = configData.pulpit_data.max_pulpit_destroy_time;
            configData.pulpit_data.max_pulpit_destroy_time = temp;
        }

        if (configData.pulpit_data.pulpit_spawn_time >= configData.pulpit_data.min_pulpit_destroy_time)
        {
            float safeSpawnTime = configData.pulpit_data.min_pulpit_destroy_time * 0.75f;
            Debug.LogWarning($"[GameConfig] pulpit_spawn_time ({configData.pulpit_data.pulpit_spawn_time}) >= min destroy time ({configData.pulpit_data.min_pulpit_destroy_time}). Clamping to {safeSpawnTime}s.");
            configData.pulpit_data.pulpit_spawn_time = safeSpawnTime;
        }
    }
}
