using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages spawning and positioning of Pulpit platforms:
/// - Uses Deterministic Path Caching so platforms re-spawn at the EXACT SAME coordinates after a rewind!
/// - Forward-biased exploration and anti-backtracking memory
/// </summary>
public class PulpitManager : MonoBehaviour
{
    public static PulpitManager Instance { get; private set; }

    [Header("Platform Prefab")]
    [SerializeField] private GameObject pulpitPrefab;

    [Header("Grid Configuration")]
    [SerializeField] private float platformSize = 5f;

    private readonly List<Pulpit> activePulpits = new List<Pulpit>();
    private readonly List<Vector3> deterministicPathCache = new List<Vector3>();
    private readonly List<Vector3> recentPositionsHistory = new List<Vector3>();
    private const int MAX_RECENT_HISTORY = 4;

    private int nextSequenceIndexToSpawn = 0;
    private float nextSpawnWorldTime = 0f;

    private readonly Vector3[] adjacentDirections = new Vector3[]
    {
        Vector3.forward,
        Vector3.right,
        Vector3.left,
        Vector3.back
    };

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
        GameEvents.OnGameStart += StartGameSpawning;
        GameEvents.OnGameRestart += RestartSpawning;
        GameEvents.OnGameOver += ClearAllPulpits;
        GameEvents.OnReturnToLobby += ClearAllPulpits;
        GameEvents.OnRewindComplete += HandleRewindComplete;
        GameEvents.OnPulpitLanded += HandlePlayerLandedOnPulpit;
    }

    private void OnDisable()
    {
        GameEvents.OnGameStart -= StartGameSpawning;
        GameEvents.OnGameRestart -= RestartSpawning;
        GameEvents.OnGameOver -= ClearAllPulpits;
        GameEvents.OnReturnToLobby -= ClearAllPulpits;
        GameEvents.OnRewindComplete -= HandleRewindComplete;
        GameEvents.OnPulpitLanded -= HandlePlayerLandedOnPulpit;
    }

    private void Start()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsPlaying)
        {
            StartGameSpawning();
        }
        else
        {
            deterministicPathCache.Clear();
            deterministicPathCache.Add(Vector3.zero);
            SpawnPulpitAt(Vector3.zero, 0f, 0);
            nextSequenceIndexToSpawn = 1;
        }
    }

    private void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;
        if (RewindManager.Instance != null && RewindManager.Instance.IsRewinding) return;

        float worldTime = RewindManager.Instance != null ? RewindManager.Instance.WorldTime : Time.time;
        float spawnDelay = GameConfig.Instance != null ? GameConfig.Instance.SpawnTime : 2.5f;

        activePulpits.RemoveAll(item => item == null || item.IsDestroyed);

        if (worldTime >= nextSpawnWorldTime && activePulpits.Count < 2)
        {
            Vector3 nextPos;

            // 1. If this step was already generated before a rewind, preserve its EXACT previous position!
            if (nextSequenceIndexToSpawn < deterministicPathCache.Count)
            {
                nextPos = deterministicPathCache[nextSequenceIndexToSpawn];
            }
            else
            {
                // 2. Generate new forward-biased adjacent position and cache it!
                Vector3 anchorPos = GetLatestAnchorPosition();
                nextPos = GenerateAdjacentPositionFrom(anchorPos);
                deterministicPathCache.Add(nextPos);
            }

            SpawnPulpitAt(nextPos, worldTime, nextSequenceIndexToSpawn);
            nextSequenceIndexToSpawn++;
            nextSpawnWorldTime = worldTime + spawnDelay;
        }
    }

    public void StartGameSpawning()
    {
        ClearAllPulpits();
        deterministicPathCache.Clear();
        recentPositionsHistory.Clear();

        deterministicPathCache.Add(Vector3.zero);
        recentPositionsHistory.Add(Vector3.zero);

        float worldTime = RewindManager.Instance != null ? RewindManager.Instance.WorldTime : 0f;
        float spawnDelay = GameConfig.Instance != null ? GameConfig.Instance.SpawnTime : 2.5f;

        SpawnPulpitAt(Vector3.zero, worldTime, 0);
        nextSequenceIndexToSpawn = 1;
        nextSpawnWorldTime = worldTime + spawnDelay;
    }

    public void RestartSpawning()
    {
        StartGameSpawning();
    }

    private void HandlePlayerLandedOnPulpit()
    {
        // Keep track of visited platforms
    }

    private void HandleRewindComplete()
    {
        RefreshActivePulpitsList();

        float worldTime = RewindManager.Instance != null ? RewindManager.Instance.WorldTime : 0f;
        float spawnDelay = GameConfig.Instance != null ? GameConfig.Instance.SpawnTime : 2.5f;

        // Find highest sequence index among currently alive pulpits
        int highestAliveIndex = 0;
        Pulpit highestAlivePulpit = null;

        foreach (Pulpit p in activePulpits)
        {
            if (p != null && !p.IsDestroyed && p.PlatformSequenceIndex >= highestAliveIndex)
            {
                highestAliveIndex = p.PlatformSequenceIndex;
                highestAlivePulpit = p;
            }
        }

        // Rewind the next sequence index to spawn so it reads from the deterministic cache!
        nextSequenceIndexToSpawn = highestAliveIndex + 1;

        if (highestAlivePulpit != null)
        {
            float timeLived = worldTime - highestAlivePulpit.SpawnWorldTime;
            float remainingUntilSpawn = Mathf.Max(0.5f, spawnDelay - timeLived);
            nextSpawnWorldTime = worldTime + remainingUntilSpawn;
        }
        else
        {
            nextSpawnWorldTime = worldTime + spawnDelay;
        }
    }

    private Vector3 GetLatestAnchorPosition()
    {
        if (activePulpits.Count > 0)
        {
            Pulpit newest = activePulpits[activePulpits.Count - 1];
            if (newest != null && !newest.IsDestroyed)
            {
                return newest.transform.position;
            }
        }

        if (deterministicPathCache.Count > 0)
        {
            return deterministicPathCache[deterministicPathCache.Count - 1];
        }

        return Vector3.zero;
    }

    private void RefreshActivePulpitsList()
    {
        activePulpits.Clear();
        Pulpit[] all = FindObjectsByType<Pulpit>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (Pulpit p in all)
        {
            if (p != null && !p.IsDestroyed)
            {
                activePulpits.Add(p);
            }
        }
    }

    private void SpawnPulpitAt(Vector3 position, float spawnTime, int sequenceIndex)
    {
        if (pulpitPrefab == null)
        {
            Debug.LogError("[PulpitManager] Pulpit Prefab is not assigned in the Inspector!");
            return;
        }

        GameObject newPulpitObj = Instantiate(pulpitPrefab, position, Quaternion.identity, transform);
        newPulpitObj.transform.localScale = new Vector3(platformSize, 0.5f, platformSize);

        Pulpit pulpitScript = newPulpitObj.GetComponent<Pulpit>();
        if (pulpitScript != null)
        {
            pulpitScript.InitializeTimeline(spawnTime, sequenceIndex);
            activePulpits.Add(pulpitScript);
        }

        recentPositionsHistory.Add(position);
        if (recentPositionsHistory.Count > MAX_RECENT_HISTORY)
        {
            recentPositionsHistory.RemoveAt(0);
        }

        GameEvents.TriggerPulpitSpawned(position);
    }

    private Vector3 GenerateAdjacentPositionFrom(Vector3 anchorPosition)
    {
        List<Vector3> preferredCandidates = new List<Vector3>();
        List<int> candidateWeights = new List<int>();

        foreach (Vector3 dir in adjacentDirections)
        {
            Vector3 candidatePos = anchorPosition + (dir * platformSize);

            bool isOccupied = false;
            foreach (Pulpit active in activePulpits)
            {
                if (active != null && !active.IsDestroyed && Vector3.Distance(active.transform.position, candidatePos) < 1f)
                {
                    isOccupied = true;
                    break;
                }
            }

            bool isRecent = false;
            foreach (Vector3 recent in recentPositionsHistory)
            {
                if (Vector3.Distance(recent, candidatePos) < 1f)
                {
                    isRecent = true;
                    break;
                }
            }

            if (!isOccupied && !isRecent)
            {
                preferredCandidates.Add(candidatePos);

                if (dir == Vector3.forward)
                    candidateWeights.Add(5);
                else if (dir == Vector3.right || dir == Vector3.left)
                    candidateWeights.Add(3);
                else
                    candidateWeights.Add(1);
            }
        }

        if (preferredCandidates.Count > 0)
        {
            int totalWeight = 0;
            foreach (int w in candidateWeights) totalWeight += w;

            int randomRoll = Random.Range(0, totalWeight);
            int cumulative = 0;

            for (int i = 0; i < preferredCandidates.Count; i++)
            {
                cumulative += candidateWeights[i];
                if (randomRoll < cumulative)
                {
                    return preferredCandidates[i];
                }
            }

            return preferredCandidates[0];
        }

        foreach (Vector3 dir in adjacentDirections)
        {
            Vector3 candidatePos = anchorPosition + (dir * platformSize);
            bool isOccupied = false;
            foreach (Pulpit active in activePulpits)
            {
                if (active != null && !active.IsDestroyed && Vector3.Distance(active.transform.position, candidatePos) < 1f)
                {
                    isOccupied = true;
                    break;
                }
            }

            if (!isOccupied)
            {
                return candidatePos;
            }
        }

        return anchorPosition + (Vector3.forward * platformSize);
    }

    public void ClearAllPulpits()
    {
        foreach (Pulpit pulpit in activePulpits)
        {
            if (pulpit != null)
                Destroy(pulpit.gameObject);
        }
        activePulpits.Clear();
        deterministicPathCache.Clear();
        recentPositionsHistory.Clear();
        nextSequenceIndexToSpawn = 0;

        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
    }
}
