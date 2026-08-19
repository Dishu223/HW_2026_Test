using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Manages the spawning and lifecycle of 9x9 Pulpit platforms.
// Ensures at most 2 pulpits exist at any time and spawns new pulpits
// adjacent to the most recent one according to JSON configurations.
public class PulpitManager : MonoBehaviour
{
    public static PulpitManager Instance { get; private set; }

    [Header("Prefab References")]
    [SerializeField] private GameObject pulpitPrefab;

    [Header("Platform Layout Settings")]
    [SerializeField] private float platformSize = 9f; // Standard 9x9 dimensions

    // Tracks currently existing pulpits in the scene (maximum 2)
    private readonly List<Pulpit> activePulpits = new List<Pulpit>();
    private Vector3 lastSpawnPosition = Vector3.zero;
    private Vector3 previousSpawnPosition = Vector3.zero;
    private Coroutine spawnCoroutine;

    // Available adjacent offsets (Right, Left, Forward, Back)
    private readonly Vector3[] adjacentOffsets = new Vector3[]
    {
        new Vector3(9f, 0f, 0f),
        new Vector3(-9f, 0f, 0f),
        new Vector3(0f, 0f, 9f),
        new Vector3(0f, 0f, -9f)
    };

    public List<Pulpit> ActivePulpits => activePulpits;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        GameEvents.OnGameStart += StartPlatformCycle;
        GameEvents.OnGameRestart += ResetPlatforms;
        GameEvents.OnGameOver += StopPlatformCycle;
    }

    private void OnDisable()
    {
        GameEvents.OnGameStart -= StartPlatformCycle;
        GameEvents.OnGameRestart -= ResetPlatforms;
        GameEvents.OnGameOver -= StopPlatformCycle;
    }

    private void StartPlatformCycle()
    {
        ResetPlatforms();
        // Spawn the very first pulpit at origin
        SpawnPulpitAt(Vector3.zero);
        
        // Start monitoring and spawning the next adjacent pulpit
        if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
        spawnCoroutine = StartCoroutine(PulpitSpawnLoop());
    }

    private void StopPlatformCycle()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    private void ResetPlatforms()
    {
        StopPlatformCycle();

        // Clean up any existing platforms
        for (int i = activePulpits.Count - 1; i >= 0; i--)
        {
            if (activePulpits[i] != null)
            {
                Destroy(activePulpits[i].gameObject);
            }
        }
        activePulpits.Clear();
        lastSpawnPosition = Vector3.zero;
        previousSpawnPosition = Vector3.zero;
    }

    private IEnumerator PulpitSpawnLoop()
    {
        while (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing)
        {
            float spawnDelay = GameConfig.Instance != null ? GameConfig.Instance.PulpitSpawnTime : 2.5f;
            yield return new WaitForSeconds(spawnDelay);

            // Maintain max 2 pulpits constraint:
            // Remove null / destroyed platforms from active tracking list
            activePulpits.RemoveAll(p => p == null);

            // If we have room for another pulpit (less than 2 active), spawn adjacent one
            if (activePulpits.Count < 2)
            {
                Vector3 nextPosition = GetNextAdjacentPosition();
                SpawnPulpitAt(nextPosition);
            }
        }
    }

    // Spawns a new pulpit at the specified world position
    private void SpawnPulpitAt(Vector3 position)
    {
        if (pulpitPrefab == null)
        {
            Debug.LogError("[PulpitManager] Pulpit Prefab is not assigned in the Inspector!");
            return;
        }

        GameObject newObj = Instantiate(pulpitPrefab, position, Quaternion.identity);
        Pulpit newPulpit = newObj.GetComponent<Pulpit>();

        if (newPulpit != null)
        {
            float minTime = GameConfig.Instance != null ? GameConfig.Instance.MinDestroyTime : 4f;
            float maxTime = GameConfig.Instance != null ? GameConfig.Instance.MaxDestroyTime : 5f;
            newPulpit.Initialize(minTime, maxTime);

            activePulpits.Add(newPulpit);
            previousSpawnPosition = lastSpawnPosition;
            lastSpawnPosition = position;

            GameEvents.TriggerPulpitSpawned(position);
            Debug.Log($"<color=green>[PulpitManager]</color> Spawned Pulpit at {position}. Active count: {activePulpits.Count}");
        }
    }

    // Calculates a random adjacent position that doesn't overlap existing pulpits or backtrack
    private Vector3 GetNextAdjacentPosition()
    {
        List<Vector3> validPositions = new List<Vector3>();

        foreach (var offset in adjacentOffsets)
        {
            Vector3 candidatePos = lastSpawnPosition + (offset.normalized * platformSize);

            // 1. Check if candidate position is occupied by any active pulpit
            bool isOccupied = false;
            foreach (var active in activePulpits)
            {
                if (active != null && Vector3.Distance(active.transform.position, candidatePos) < 1f)
                {
                    isOccupied = true;
                    break;
                }
            }

            // 2. Avoid immediately jumping back to the previous pulpit position if possible
            bool isPrevious = Vector3.Distance(candidatePos, previousSpawnPosition) < 1f;

            if (!isOccupied && !isPrevious)
            {
                validPositions.Add(candidatePos);
            }
        }

        // Fallback: If all candidates were filtered out, accept any unoccupied adjacent spot
        if (validPositions.Count == 0)
        {
            foreach (var offset in adjacentOffsets)
            {
                Vector3 candidatePos = lastSpawnPosition + (offset.normalized * platformSize);
                bool isOccupied = false;
                foreach (var active in activePulpits)
                {
                    if (active != null && Vector3.Distance(active.transform.position, candidatePos) < 1f)
                    {
                        isOccupied = true;
                        break;
                    }
                }
                if (!isOccupied) validPositions.Add(candidatePos);
            }
        }

        // Last resort safety fallback
        if (validPositions.Count == 0)
        {
            return lastSpawnPosition + new Vector3(platformSize, 0f, 0f);
        }

        // Pick randomly from valid adjacent spots
        int randomIndex = Random.Range(0, validPositions.Count);
        return validPositions[randomIndex];
    }
}
