# 📜 Code Breakdown: `GameEvents.cs`

**Location:** `Assets/_Game/Scripts/Core/GameEvents.cs`  
**Pattern:** Observer Pattern (Event Bus)  
**Role in Architecture:** The global "Radio Station" that decouples all gameplay, UI, audio, and visual systems.

---

## 🎯 Why Did We Write This?
In small projects, beginners often make scripts call each other directly:
`Player -> calls -> ScoreManager -> calls -> UIManager -> calls -> AudioManager`

If any script is removed or modified, everything breaks with red compile errors.  
`GameEvents` solves this. When something happens in the game, the script simply broadcasts an event (e.g. `GameEvents.TriggerPulpitLanded()`). Any script that cares can listen to it. If nobody is listening, nothing breaks.

---

## 💻 Full Code

```csharp
using System;
using UnityEngine;

// This is our global radio station.
// Any script can broadcast an event, and any script can listen to it.
public static class GameEvents
{
    #region Game Flow Events
    // Fired when transitioning between game states
    public static event Action OnGameStart;
    public static event Action OnGameOver;
    public static event Action OnGameRestart;
    public static event Action OnReturnToLobby;
    #endregion

    #region Gameplay Events
    // Fired when the score updates (passes the new score integer)
    public static event Action<int> OnScoreChanged;

    // Fired when player hits milestones like 10, 25, 50
    public static event Action<int> OnMilestoneReached;

    // Fired when Doofus steps on a brand-new pulpit
    public static event Action OnPulpitLanded;

    // Fired every frame as the pulpit timer ticks down (0.0 = expired, 1.0 = full time)
    public static event Action<float> OnPulpitTimerTick;

    // Fired when a pulpit is destroyed or spawned (passes the world position for particle effects)
    public static event Action<Vector3> OnPulpitDestroyed;
    public static event Action<Vector3> OnPulpitSpawned;
    #endregion

    #region Player Events
    // Fired when Doofus falls off into the abyss
    public static event Action OnDoofusFell;
    #endregion

    #region Rewind Time Events
    // Fired when entering and exiting the time rewind sequence
    public static event Action OnRewindStart;
    public static event Action OnRewindComplete;
    #endregion

    #region Helper Trigger Methods
    // These helper methods make firing events clean and prevent null-reference errors.
    // Instead of doing "if (OnGameStart != null) OnGameStart();", we just call "GameEvents.TriggerGameStart();"

    public static void TriggerGameStart() => OnGameStart?.Invoke();
    public static void TriggerGameOver() => OnGameOver?.Invoke();
    public static void TriggerGameRestart() => OnGameRestart?.Invoke();
    public static void TriggerReturnToLobby() => OnReturnToLobby?.Invoke();

    public static void TriggerScoreChanged(int newScore) => OnScoreChanged?.Invoke(newScore);
    public static void TriggerMilestoneReached(int milestone) => OnMilestoneReached?.Invoke(milestone);
    public static void TriggerPulpitLanded() => OnPulpitLanded?.Invoke();
    public static void TriggerPulpitTimerTick(float normalizedTime) => OnPulpitTimerTick?.Invoke(normalizedTime);
    public static void TriggerPulpitDestroyed(Vector3 position) => OnPulpitDestroyed?.Invoke(position);
    public static void TriggerPulpitSpawned(Vector3 position) => OnPulpitSpawned?.Invoke(position);

    public static void TriggerDoofusFell() => OnDoofusFell?.Invoke();

    public static void TriggerRewindStart() => OnRewindStart?.Invoke();
    public static void TriggerRewindComplete() => OnRewindComplete?.Invoke();
    #endregion
}
```

---

## 🔍 Line-by-Line Explanation

### 1. The Namespaces
```csharp
using System;
using UnityEngine;
```
- `using System;` — Gives us access to core C# types, specifically `Action` and `Action<T>` delegates.
- `using UnityEngine;` — Gives us Unity-specific types, such as `Vector3` (3D coordinates for positions).

---

### 2. The Class Declaration
```csharp
public static class GameEvents
```
- `public` — Any script in our entire project can see and access this class.
- `static` — You **do not** attach this script to any GameObject in Unity. It lives purely in memory as a single global utility. You never need `FindObjectOfType` or `GetComponent`. You just type `GameEvents.Something`.

---

### 3. Event Declarations
```csharp
public static event Action OnGameStart;
```
- `event` — A C# keyword that restricts outside scripts from accidentally overwriting or resetting the listener list. Outside scripts can only *subscribe* (`+=`) or *unsubscribe* (`-=`).
- `Action` — A built-in C# delegate that represents a method that takes **no parameters** and returns **void**.
- `OnGameStart` — The name of the event signal.

```csharp
public static event Action<int> OnScoreChanged;
```
- `Action<int>` — A delegate that passes an integer parameter along with the signal (e.g. the new score: `5`, `12`, etc.). Whoever is listening will receive this integer.

```csharp
public static event Action<Vector3> OnPulpitDestroyed;
```
- `Action<Vector3>` — Sends a 3D position vector (`x, y, z`) where a platform just collapsed. The Particle/VFX manager listens to this and spawns explosion sparks at that exact spot.

---

### 4. Helper Trigger Methods
```csharp
public static void TriggerGameStart() => OnGameStart?.Invoke();
```
- `=>` — C# lambda / expression-bodied syntax. It is a shorthand for writing a one-line function:
  ```csharp
  public static void TriggerGameStart()
  {
      if (OnGameStart != null)
      {
          OnGameStart.Invoke();
      }
  }
  ```
- `?.` — The **null-conditional operator**. If no script is currently listening to `OnGameStart`, `OnGameStart` is `null`. Without `?.`, calling `.Invoke()` would crash Unity with a `NullReferenceException`. With `?.`, it safely does nothing!

---

## 💡 How Other Scripts Will Use This

### How a script Listens (Subscribes):
```csharp
void OnEnable()
{
    // Tune in to the radio station
    GameEvents.OnScoreChanged += HandleScoreUpdated;
}

void OnDisable()
{
    // ALWAYS unsubscribe when disabled to prevent memory leaks!
    GameEvents.OnScoreChanged -= HandleScoreUpdated;
}

void HandleScoreUpdated(int newScore)
{
    Debug.Log("Score is now: " + newScore);
}
```

### How a script Broadcasts (Triggers):
```csharp
// When Doofus steps on a pulpit:
GameEvents.TriggerPulpitLanded();
```
