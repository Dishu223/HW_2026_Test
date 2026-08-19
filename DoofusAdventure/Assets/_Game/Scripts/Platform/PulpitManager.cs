using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages spawning and lifecycle of Pulpit platforms.
/// Enforces the rule that a maximum of 2 Pulpits exist simultaneously,
/// positions them 9 units adjacently, and prevents duplicate/backtracking positions.
/// </summary>
public class PulpitManager : MonoBehaviour
{
    [Header("Platform Prefab")]
    [SerializeField] private GameObject pulpitPrefab;

    [Header("Grid Configuration")]
    [SerializeField] private float platformSize = 9f; // 9x9 platform dimension

    private readonly List<GameObject> activePulpits = new List<GameObject>();
    private Vector3 currentPulpitPosition = Vector3.zero;
    private Vector3 previousPulpitPosition = Vector3.zero;
    private Coroutine spawnRoutine;

    // The 4 adjacent cardinal directions
    private readonly Vector3[] adjacentDirections = new Vector3[]
    {
        Vector3.forward, // ( 0, 0,  1)
        Vector3.back,    // ( 0, 0, -1)
        Vector3.right,   // ( 1, 0,  0)
        Vector3.left     // (-1, 0,  0)
    };

    private void OnEnable()
    {
        GameEvents.OnGameStart += StartGameSpawning;
        GameEvents.OnGameRestart += RestartSpawning;
        GameEvents.OnGameOver += StopSpawning;
    }

    private void OnDisable()
    {
        GameEvents.OnGameStart -= StartGameSpawning;
        GameEvents.OnGameRestart -= RestartSpawning;
        GameEvents.OnGameOver -= StopSpawning;
    }

    private void Start()
    {
        // Auto-start spawning if starting directly in play mode
        StartGameSpawning();
    }

    /// <summary>
    /// Clears any existing platforms and spawns the starting platform at (0, 0, 0).
    /// </summary>
    public void StartGameSpawning()
    {
        StopSpawning();
        ClearAllPulpits();

        currentPulpitPosition = Vector3.zero;
        previousPulpitPosition = Vector3.zero;

        // Spawn initial starting platform
        SpawnPulpitAt(currentPulpitPosition);

        // Begin the spawn timer loop
        spawnRoutine = StartCoroutine(SpawnLoopCoroutine());
    }

    public void RestartSpawning()
    {
        StartGameSpawning();
    }

    public void StopSpawning()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
    }

    /// <summary>
    /// Coroutine that waits for pulpit_spawn_time before spawning the next adjacent platform.
    /// Ensures no more than 2 pulpits are alive simultaneously.
    /// </summary>
    private IEnumerator SpawnLoopCoroutine()
    {
        while (true)
        {
            float spawnDelay = GameConfig.Instance != null ? GameConfig.Instance.SpawnTime : 2.5f;
            yield return new WaitForSeconds(spawnDelay);

            // Clean up any destroyed platforms from our active tracking list
            activePulpits.RemoveAll(item => item == null);

            // Wait if we already have 2 active pulpits (assignment rule: max 2 simultaneous pulpits)
            while (activePulpits.Count >= 2)
            {
                activePulpits.RemoveAll(item => item == null);
                yield return null;
            }

            // Calculate next valid adjacent position
            Vector3 nextPos = GetNextAdjacentPosition();

            previousPulpitPosition = currentPulpitPosition;
            currentPulpitPosition = nextPos;

            SpawnPulpitAt(currentPulpitPosition);
        }
    }

    /// <summary>
    /// Instantiates a pulpit at target coordinates and tracks it in the active list.
    /// </summary>
    private void SpawnPulpitAt(Vector3 position)
    {
        if (pulpitPrefab == null)
        {
            Debug.LogError("[PulpitManager] Pulpit Prefab is not assigned in the Inspector!");
            return;
        }

        GameObject newPulpit = Instantiate(pulpitPrefab, position, Quaternion.identity, transform);
        activePulpits.Add(newPulpit);

        GameEvents.TriggerPulpitSpawned(position);
    }

    /// <summary>
    /// Picks an adjacent position (offset by 9 units) that is neither the current position
    /// nor the immediately previous position, and is not occupied by an active platform.
    /// </summary>
    private Vector3 GetNextAdjacentPosition()
    {
        List<Vector3> validPositions = new List<Vector3>();

        foreach (Vector3 dir in adjacentDirections)
        {
            Vector3 candidatePos = currentPulpitPosition + (dir * platformSize);

            // Check if this position is occupied by any active platform
            bool isOccupied = false;
            foreach (GameObject active in activePulpits)
            {
                if (active != null && Vector3.Distance(active.transform.position, candidatePos) < 1f)
                {
                    isOccupied = true;
                    break;
                }
            }

            // Rule: Don't spawn on occupied spots and avoid immediate backtracking
            if (!isOccupied && candidatePos != previousPulpitPosition)
            {
                validPositions.Add(candidatePos);
            }
        }

        // Fallback: If all candidates are filtered out, allow any non-occupied adjacent spot
        if (validPositions.Count == 0)
        {
            foreach (Vector3 dir in adjacentDirections)
            {
                Vector3 candidatePos = currentPulpitPosition + (dir * platformSize);
                if (candidatePos != currentPulpitPosition)
                {
                    validPositions.Add(candidatePos);
                }
            }
        }

        // Pick one randomly from the valid adjacent positions
        int randomIndex = Random.Range(0, validPositions.Count);
        return validPositions[randomIndex];
    }

    private void ClearAllPulpits()
    {
        foreach (GameObject pulpit in activePulpits)
        {
            if (pulpit != null)
                Destroy(pulpit);
        }
        activePulpits.Clear();
    }
}
