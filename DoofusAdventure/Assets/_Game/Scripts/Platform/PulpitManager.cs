using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages spawning and positioning of Pulpit platforms:
/// - Forward-biased exploration so Doofus journeys forward through the world
/// - Strict 4-tile memory history to eliminate backtracking to recent spots
/// - Always adjacent and reachable from current platform
/// </summary>
public class PulpitManager : MonoBehaviour
{
    public static PulpitManager Instance { get; private set; }

    [Header("Platform Prefab")]
    [SerializeField] private GameObject pulpitPrefab;

    [Header("Grid Configuration")]
    [SerializeField] private float platformSize = 5f;

    private readonly List<Pulpit> activePulpits = new List<Pulpit>();
    private readonly List<Vector3> recentPositionsHistory = new List<Vector3>();
    private const int MAX_RECENT_HISTORY = 4;

    private Vector3 currentAnchorPosition = Vector3.zero;
    private float nextSpawnWorldTime = 0f;

    private readonly Vector3[] adjacentDirections = new Vector3[]
    {
        Vector3.forward, // High priority
        Vector3.right,   // Medium priority
        Vector3.left,    // Medium priority
        Vector3.back     // Low priority (fallback only)
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
            SpawnPulpitAt(Vector3.zero, 0f);
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
            UpdateAnchorToLatestPlatform();

            Vector3 nextPos = GetNextAdjacentPosition();
            currentAnchorPosition = nextPos;

            SpawnPulpitAt(nextPos, worldTime);
            nextSpawnWorldTime = worldTime + spawnDelay;
        }
    }

    public void StartGameSpawning()
    {
        ClearAllPulpits();
        recentPositionsHistory.Clear();

        currentAnchorPosition = Vector3.zero;
        recentPositionsHistory.Add(Vector3.zero);

        float worldTime = RewindManager.Instance != null ? RewindManager.Instance.WorldTime : 0f;
        float spawnDelay = GameConfig.Instance != null ? GameConfig.Instance.SpawnTime : 2.5f;

        SpawnPulpitAt(Vector3.zero, worldTime);
        nextSpawnWorldTime = worldTime + spawnDelay;
    }

    public void RestartSpawning()
    {
        StartGameSpawning();
    }

    private void HandlePlayerLandedOnPulpit()
    {
        DoofusController doofus = FindAnyObjectByType<DoofusController>();
        if (doofus != null)
        {
            Pulpit current = GetClosestAlivePulpit(doofus.transform.position);
            if (current != null)
            {
                currentAnchorPosition = current.transform.position;
            }
        }
    }

    private void HandleRewindComplete()
    {
        RefreshActivePulpitsList();

        float worldTime = RewindManager.Instance != null ? RewindManager.Instance.WorldTime : 0f;
        float spawnDelay = GameConfig.Instance != null ? GameConfig.Instance.SpawnTime : 2.5f;

        DoofusController doofus = FindAnyObjectByType<DoofusController>();
        Vector3 playerPos = doofus != null ? doofus.transform.position : Vector3.zero;
        Pulpit closest = GetClosestAlivePulpit(playerPos);

        if (closest != null)
        {
            currentAnchorPosition = closest.transform.position;
            float timeLived = worldTime - closest.SpawnWorldTime;
            float remainingUntilSpawn = Mathf.Max(0.5f, spawnDelay - timeLived);
            nextSpawnWorldTime = worldTime + remainingUntilSpawn;
        }
        else
        {
            nextSpawnWorldTime = worldTime + spawnDelay;
        }
    }

    private void UpdateAnchorToLatestPlatform()
    {
        if (activePulpits.Count == 0) return;

        Pulpit newest = activePulpits[activePulpits.Count - 1];
        if (newest != null && !newest.IsDestroyed)
        {
            currentAnchorPosition = newest.transform.position;
        }
    }

    private void RefreshActivePulpitsList()
    {
        activePulpits.Clear();
        Pulpit[] all = FindObjectsByType<Pulpit>(FindObjectsSortMode.None);
        foreach (Pulpit p in all)
        {
            if (p != null && !p.IsDestroyed)
            {
                activePulpits.Add(p);
            }
        }
    }

    private Pulpit GetClosestAlivePulpit(Vector3 origin)
    {
        Pulpit[] all = FindObjectsByType<Pulpit>(FindObjectsSortMode.None);
        Pulpit closest = null;
        float minDst = float.MaxValue;

        foreach (Pulpit p in all)
        {
            if (p != null && !p.IsDestroyed)
            {
                float dst = Vector3.Distance(origin, p.transform.position);
                if (dst < minDst)
                {
                    minDst = dst;
                    closest = p;
                }
            }
        }
        return closest;
    }

    private void SpawnPulpitAt(Vector3 position, float spawnTime)
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
            pulpitScript.InitializeTimeline(spawnTime);
            activePulpits.Add(pulpitScript);
        }

        // Record into recent history
        recentPositionsHistory.Add(position);
        if (recentPositionsHistory.Count > MAX_RECENT_HISTORY)
        {
            recentPositionsHistory.RemoveAt(0);
        }

        GameEvents.TriggerPulpitSpawned(position);
    }

    /// <summary>
    /// Weighted Forward-Biased candidate selection with strict recent history avoidance.
    /// </summary>
    private Vector3 GetNextAdjacentPosition()
    {
        List<Vector3> preferredCandidates = new List<Vector3>();
        List<int> candidateWeights = new List<int>();

        foreach (Vector3 dir in adjacentDirections)
        {
            Vector3 candidatePos = currentAnchorPosition + (dir * platformSize);

            // 1. Check if occupied by any currently active pulpit
            bool isOccupied = false;
            foreach (Pulpit active in activePulpits)
            {
                if (active != null && !active.IsDestroyed && Vector3.Distance(active.transform.position, candidatePos) < 1f)
                {
                    isOccupied = true;
                    break;
                }
            }

            // 2. Check if in recent history (prevent backtracking!)
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

                // Forward gets highest weight, Left/Right medium, Back lowest
                if (dir == Vector3.forward)
                    candidateWeights.Add(5); // 50%
                else if (dir == Vector3.right || dir == Vector3.left)
                    candidateWeights.Add(3); // 30% each
                else
                    candidateWeights.Add(1); // 10%
            }
        }

        // If preferred non-recent directions are available, choose with forward bias
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

        // Fallback: If trapped by recent tiles, pick any unoccupied adjacent space
        foreach (Vector3 dir in adjacentDirections)
        {
            Vector3 candidatePos = currentAnchorPosition + (dir * platformSize);
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

        // Emergency fallback: Forward
        return currentAnchorPosition + (Vector3.forward * platformSize);
    }

    public void ClearAllPulpits()
    {
        foreach (Pulpit pulpit in activePulpits)
        {
            if (pulpit != null)
                Destroy(pulpit.gameObject);
        }
        activePulpits.Clear();
        recentPositionsHistory.Clear();

        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
    }
}
