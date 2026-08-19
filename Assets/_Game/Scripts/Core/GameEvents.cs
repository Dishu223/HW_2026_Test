using System;
using UnityEngine;

// Central nervous system of the game.
// Scripts talk through these static events so nothing needs tight references to each other.
public static class GameEvents
{
    #region Game Flow
    public static event Action OnGameStart;
    public static event Action OnGameOver;
    public static event Action OnGameRestart;
    public static event Action OnReturnToMenu;

    public static void TriggerGameStart() => OnGameStart?.Invoke();
    public static void TriggerGameOver() => OnGameOver?.Invoke();
    public static void TriggerGameRestart() => OnGameRestart?.Invoke();
    public static void TriggerReturnToMenu() => OnReturnToMenu?.Invoke();
    #endregion

    #region Score & Milestones
    public static event Action<int> OnScoreChanged;
    public static event Action<int> OnMilestoneReached;

    public static void TriggerScoreChanged(int newScore) => OnScoreChanged?.Invoke(newScore);
    public static void TriggerMilestoneReached(int milestone) => OnMilestoneReached?.Invoke(milestone);
    #endregion

    #region Platform / Pulpit Lifecycle
    public static event Action OnPulpitLanded;
    public static event Action<float> OnPulpitTimerTick; // normalized time remaining (1.0 = full, 0.0 = expired)
    public static event Action<Vector3> OnPulpitSpawned;
    public static event Action<Vector3> OnPulpitDestroyed;

    public static void TriggerPulpitLanded() => OnPulpitLanded?.Invoke();
    public static void TriggerPulpitTimerTick(float normalizedTime) => OnPulpitTimerTick?.Invoke(normalizedTime);
    public static void TriggerPulpitSpawned(Vector3 position) => OnPulpitSpawned?.Invoke(position);
    public static void TriggerPulpitDestroyed(Vector3 position) => OnPulpitDestroyed?.Invoke(position);
    #endregion

    #region Player Actions & States
    public static event Action OnDoofusFell;
    public static void TriggerDoofusFell() => OnDoofusFell?.Invoke();
    #endregion

    #region Rewind Mechanic
    public static event Action OnRewindStart;
    public static event Action OnRewindComplete;

    public static void TriggerRewindStart() => OnRewindStart?.Invoke();
    public static void TriggerRewindComplete() => OnRewindComplete?.Invoke();
    #endregion
}
