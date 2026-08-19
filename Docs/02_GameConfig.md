# 📜 Code Breakdown: `GameConfig.cs`

**Location:** `Assets/_Game/Scripts/Core/GameConfig.cs`  
**Pattern:** Singleton Pattern + Data Transfer Object (DTO) + Defensive Sanitization  
**Role in Architecture:** Loads, parses, validates, and globally provides gameplay values from `game_data.json`.

---

## 🎯 Why Did We Write This?
Level 1 of the assignment requires the player speed, pulpit destroy times, and spawn times to be configured via a JSON file.  
Instead of hardcoding values directly into `Player` or `PulpitManager`, we encapsulate all data loading in `GameConfig`.

### Key Benefits:
1. **Separation of Concerns**: Gameplay scripts only ask "what is the speed?" without worrying about file I/O or JSON parsing.
2. **Defensive Resilience**: If the JSON file is deleted, has typos, or invalid numbers (e.g. negative speed), the game **never crashes**—it sanitizes the numbers and logs informative warnings.
3. **Inspector Inspection**: Because we marked fields with `[SerializeField]`, you can see the loaded values live in Unity's Inspector window while playing!

---

## 🔍 Line-by-Line Explanation

### 1. Data Structures (DTOs)
```csharp
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
```
- `[Serializable]`: Tells Unity's serialization engine that this class can be converted between C# objects and JSON text.
- Variable names (`player_data`, `speed`, `min_pulpit_destroy_time`, etc.) match the keys in `game_data.json` **character-for-character**. Unity's `JsonUtility` is case-sensitive!

---

### 2. The Singleton Instance
```csharp
public static GameConfig Instance { get; private set; }
```
- `public static GameConfig Instance`: Allows any other script to write `GameConfig.Instance.PlayerSpeed` without finding GameObjects.
- `{ get; private set; }`: Anyone can read the instance, but only this script can assign it.

---

### 3. The `Awake()` Lifecycle Method
```csharp
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
```
- `Awake()`: Unity runs this before `Start()`, ensuring our configuration is loaded before any other script tries to read it.
- `DontDestroyOnLoad(gameObject)`: If we reload scenes, this object stays alive in memory.

---

### 4. File Loading & JSON Parsing
```csharp
string filePath = Path.Combine(Application.streamingAssetsPath, JSON_FILENAME);
```
- `Application.streamingAssetsPath`: A special Unity path that points to `Assets/StreamingAssets` in the editor and copies raw files directly into the final build (Windows .exe, etc.).
- `Path.Combine`: Safely combines directory paths across Windows (`\`) and Mac/Linux (`/`).
- `File.ReadAllText(filePath)`: Reads the entire text of the JSON file into a string.
- `JsonUtility.FromJson<GameDataRoot>(jsonContent)`: Converts the raw JSON string into strongly-typed C# objects.

---

### 5. Defensive Data Sanitization (`SanitizeData()`)
```csharp
if (configData.player_data.speed <= 0f)
{
    configData.player_data.speed = 1f;
}
```
- Guarantees speed is never zero or negative (otherwise Doofus would be stuck or walk backwards).
- Swaps `min` and `max` if someone accidentally set `min: 10, max: 4`.
- Clamps `spawn_time` so it's always less than `min_destroy_time`. (If a pulpit died in 4s but a new one didn't spawn until 5s, the player would be guaranteed to fall!).
