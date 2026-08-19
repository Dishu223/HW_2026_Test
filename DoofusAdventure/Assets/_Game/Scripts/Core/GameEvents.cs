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
