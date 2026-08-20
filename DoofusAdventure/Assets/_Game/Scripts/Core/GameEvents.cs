using System;
using UnityEngine;

public static class GameEvents
{
    #region Game Flow Events
    public static event Action OnGameStart;
    public static event Action OnGameOver;
    public static event Action OnGameRestart;
    public static event Action OnReturnToLobby;
    #endregion

    #region Gameplay Events
    public static event Action<int> OnScoreChanged;
    public static event Action<int> OnMilestoneReached;
    public static event Action OnPulpitLanded;
    public static event Action<float> OnPulpitTimerTick;
    public static event Action<Vector3> OnPulpitDestroyed;
    public static event Action<Vector3> OnPulpitSpawned;
    #endregion

    #region Player Events
    public static event Action OnDoofusFell;
    #endregion

    #region Rewind Time Events
    public static event Action OnRewindStart;
    public static event Action OnRewindComplete;
    public static event Action<int, int> OnRewindChargesChanged; // (currentCharges, maxCharges)
    #endregion

    #region Helper Trigger Methods
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
    public static void TriggerRewindChargesChanged(int current, int max) => OnRewindChargesChanged?.Invoke(current, max);
    #endregion
}
