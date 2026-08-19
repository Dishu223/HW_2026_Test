using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Manages the lifecycle and adjacent spawning of 9x9 Pulpit platforms (max 2 simultaneously)
public class PulpitManager : MonoBehaviour
{
    [Header("Prefab & Spacing")]
    [SerializeField] private GameObject pulpitPrefab;
    [SerializeField] private float platformSize = 9f;

    private List<Pulpit> activePulpits = new List<Pulpit>();
    private Vector3 lastSpawnedPosition = Vector3.zero;
    private Coroutine spawnRoutine;

    private readonly Vector3[] adjacentDirections = new Vector3[]
    {
        Vector3.forward,
        Vector3.back,
        Vector3.left,
        Vector3.right
    };

    private void OnEnable()
    {
        GameEvents.OnGameStart += StartPlatformCycle;
        GameEvents.OnGameRestart += RestartPlatformCycle;
        GameEvents.OnGameOver += StopPlatformCycle;
    }

    private void OnDisable()
    {
        GameEvents.OnGameStart -= StartPlatformCycle;
        GameEvents.OnGameRestart -= RestartPlatformCycle;
        GameEvents.OnGameOver -= StopPlatformCycle;
    }

    public void StartPlatformCycle()
    {
        ClearAllPulpits();
        SpawnInitialPulpit();
    }

    public void RestartPlatformCycle()
    {
        ClearAllPulpits();
        SpawnInitialPulpit();
    }

    public void StopPlatformCycle()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
    }

    private void ClearAllPulpits()
    {
        StopPlatformCycle();

        for (int i = activePulpits.Count - 1; i >= 0; i--)
        {
            if (activePulpits[i] != null)
            {
                Destroy(activePulpits[i].gameObject);
            }
        }
        activePulpits.Clear();
    }

    private void SpawnInitialPulpit()
    {
        Vector3 spawnPos = Vector3.zero;
        SpawnPulpitAt(spawnPos);
    }

    private void SpawnPulpitAt(Vector3 position)
    {
        if (pulpitPrefab == null)
        {
            Debug.LogError("[PulpitManager] Pulpit Prefab is not assigned in the Inspector!");
            return;
        }

        GameObject obj = Instantiate(pulpitPrefab, position, Quaternion.identity, transform);
        Pulpit pulpit = obj.GetComponent<Pulpit>();

        if (pulpit == null)
        {
            pulpit = obj.AddComponent<Pulpit>();
        }

        float minTime = GameConfig.Instance != null ? GameConfig.Instance.MinDestroyTime : 4f;
        float maxTime = GameConfig.Instance != null ? GameConfig.Instance.MaxDestroyTime : 5f;
        float randomLifetime = Random.Range(minTime, maxTime);

        pulpit.Initialize(randomLifetime, OnPulpitDestroyed);
        activePulpits.Add(pulpit);
        lastSpawnedPosition = position;

        GameEvents.TriggerPulpitSpawned(position);

        // Schedule the next pulpit appearance based on pulpit_spawn_time
        float spawnDelay = GameConfig.Instance != null ? GameConfig.Instance.PulpitSpawnTime : 2.5f;
        spawnRoutine = StartCoroutine(ScheduleNextPulpit(spawnDelay));
    }

    private IEnumerator ScheduleNextPulpit(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) yield break;

        // Ensure maximum 2 pulpits at any given time
        while (activePulpits.Count >= 2)
        {
            yield return null;
        }

        Vector3 nextPosition = GetValidAdjacentPosition(lastSpawnedPosition);
        SpawnPulpitAt(nextPosition);
    }

    private Vector3 GetValidAdjacentPosition(Vector3 origin)
    {
        List<Vector3> candidates = new List<Vector3>();

        foreach (var dir in adjacentDirections)
        {
            Vector3 candidatePos = origin + dir * platformSize;

            // Check that the candidate spot is not already occupied by an active pulpit
            bool isOccupied = false;
            foreach (var active in activePulpits)
            {
                if (active != null && Vector3.Distance(active.Position, candidatePos) < 1f)
                {
                    isOccupied = true;
                    break;
                }
            }

            if (!isOccupied)
            {
                candidates.Add(candidatePos);
            }
        }

        // Fallback safety if all adjacent spots were occupied
        if (candidates.Count == 0)
        {
            int fallbackIndex = Random.Range(0, adjacentDirections.Length);
            return origin + adjacentDirections[fallbackIndex] * platformSize;
        }

        int randomIndex = Random.Range(0, candidates.Count);
        return candidates[randomIndex];
    }

    private void OnPulpitDestroyed(Pulpit pulpit)
    {
        if (activePulpits.Contains(pulpit))
        {
            activePulpits.Remove(pulpit);
        }
    }
}
