using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages spawning and positioning of Pulpit platforms using the global deterministic timeline.
/// </summary>
public class PulpitManager : MonoBehaviour
{
    public static PulpitManager Instance { get; private set; }

    [Header("Platform Prefab")]
    [SerializeField] private GameObject pulpitPrefab;

    [Header("Grid Configuration")]
    [SerializeField] private float platformSize = 5f;

    private readonly List<Pulpit> activePulpits = new List<Pulpit>();
    private Vector3 currentPulpitPosition = Vector3.zero;
    private Vector3 previousPulpitPosition = Vector3.zero;
    private float nextSpawnWorldTime = 0f;

    private readonly Vector3[] adjacentDirections = new Vector3[]
    {
        Vector3.forward,
        Vector3.back,
        Vector3.right,
        Vector3.left
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
    }

    private void OnDisable()
    {
        GameEvents.OnGameStart -= StartGameSpawning;
        GameEvents.OnGameRestart -= RestartSpawning;
        GameEvents.OnGameOver -= ClearAllPulpits;
        GameEvents.OnReturnToLobby -= ClearAllPulpits;
        GameEvents.OnRewindComplete -= HandleRewindComplete;
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

        // Clean null and destroyed references
        activePulpits.RemoveAll(item => item == null || item.IsDestroyed);

        // Spawn next platform when world time reaches next scheduled spawn
        if (worldTime >= nextSpawnWorldTime && activePulpits.Count < 2)
        {
            Vector3 nextPos = GetNextAdjacentPosition();
            previousPulpitPosition = currentPulpitPosition;
            currentPulpitPosition = nextPos;

            SpawnPulpitAt(currentPulpitPosition, worldTime);
            nextSpawnWorldTime = worldTime + spawnDelay;
        }
    }

    public void StartGameSpawning()
    {
        ClearAllPulpits();

        currentPulpitPosition = Vector3.zero;
        previousPulpitPosition = Vector3.zero;

        float worldTime = RewindManager.Instance != null ? RewindManager.Instance.WorldTime : 0f;
        float spawnDelay = GameConfig.Instance != null ? GameConfig.Instance.SpawnTime : 2.5f;

        SpawnPulpitAt(currentPulpitPosition, worldTime);
        nextSpawnWorldTime = worldTime + spawnDelay;
    }

    public void RestartSpawning()
    {
        StartGameSpawning();
    }

    private void HandleRewindComplete()
    {
        RefreshActivePulpitsList();

        float worldTime = RewindManager.Instance != null ? RewindManager.Instance.WorldTime : 0f;
        float spawnDelay = GameConfig.Instance != null ? GameConfig.Instance.SpawnTime : 2.5f;

        // Identify the closest platform Doofus is standing on
        DoofusController doofus = FindAnyObjectByType<DoofusController>();
        Vector3 playerPos = doofus != null ? doofus.transform.position : Vector3.zero;
        Pulpit closest = GetClosestAlivePulpit(playerPos);

        if (closest != null)
        {
            currentPulpitPosition = closest.transform.position;
            // Schedule next spawn naturally based on how long this platform has lived
            float timeLived = worldTime - closest.SpawnWorldTime;
            float remainingUntilSpawn = Mathf.Max(0.5f, spawnDelay - timeLived);
            nextSpawnWorldTime = worldTime + remainingUntilSpawn;
        }
        else
        {
            nextSpawnWorldTime = worldTime + spawnDelay;
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

        GameEvents.TriggerPulpitSpawned(position);
    }

    private Vector3 GetNextAdjacentPosition()
    {
        List<Vector3> validPositions = new List<Vector3>();

        foreach (Vector3 dir in adjacentDirections)
        {
            Vector3 candidatePos = currentPulpitPosition + (dir * platformSize);

            bool isOccupied = false;
            foreach (Pulpit active in activePulpits)
            {
                if (active != null && !active.IsDestroyed && Vector3.Distance(active.transform.position, candidatePos) < 1f)
                {
                    isOccupied = true;
                    break;
                }
            }

            if (!isOccupied && candidatePos != previousPulpitPosition)
            {
                validPositions.Add(candidatePos);
            }
        }

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

        int randomIndex = Random.Range(0, validPositions.Count);
        return validPositions[randomIndex];
    }

    public void ClearAllPulpits()
    {
        foreach (Pulpit pulpit in activePulpits)
        {
            if (pulpit != null)
                Destroy(pulpit.gameObject);
        }
        activePulpits.Clear();

        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
    }
}
