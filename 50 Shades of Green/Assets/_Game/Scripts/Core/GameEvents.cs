using System;
using UnityEngine;

// Central event hub for the game.
// Keeps scripts decoupled so they don't need direct references to each other.
public static class GameEvents
{
    // Game lifecycle events
    public static event Action OnGameStart;
    public static event Action OnGameOver;
    public static event Action OnGameRestart;

    // Platform events
    public static event Action<Vector3> OnPulpitSpawned;
    public static event Action<Vector3> OnPulpitDestroyed;
    public static event Action<float> OnPulpitTimerTick; // 0.0 to 1.0 remaining
    public static event Action OnPulpitLanded;

    // Player events
    public static event Action OnDoofusFell;

    // Score events
    public static event Action<int> OnScoreChanged;
    public static event Action<int> OnMilestoneReached;

    // Helper triggers to avoid repetitive null checks
    public static void TriggerGameStart() => OnGameStart?.Invoke();
    public static void TriggerGameOver() => OnGameOver?.Invoke();
    public static void TriggerGameRestart() => OnGameRestart?.Invoke();
    public static void TriggerPulpitSpawned(Vector3 pos) => OnPulpitSpawned?.Invoke(pos);
    public static void TriggerPulpitDestroyed(Vector3 pos) => OnPulpitDestroyed?.Invoke(pos);
    public static void TriggerPulpitTimerTick(float normalized) => OnPulpitTimerTick?.Invoke(normalized);
    public static void TriggerPulpitLanded() => OnPulpitLanded?.Invoke();
    public static void TriggerDoofusFell() => OnDoofusFell?.Invoke();
    public static void TriggerScoreChanged(int score) => OnScoreChanged?.Invoke(score);
    public static void TriggerMilestoneReached(int milestone) => OnMilestoneReached?.Invoke(milestone);
}
