using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages spawning and positioning of Pulpit platforms.
/// Tracks remaining spawn time for on-tile diegetic timer displays.
/// </summary>
public class PulpitManager : MonoBehaviour
{
    public static PulpitManager Instance { get; private set; }

    [Header("Platform Prefab")]
    [SerializeField] private GameObject pulpitPrefab;

    [Header("Grid Configuration")]
    [SerializeField] private float platformSize = 5f;

    public float RemainingSpawnTime { get; private set; } = 0f;

    private readonly List<GameObject> activePulpits = new List<GameObject>();
    private Vector3 currentPulpitPosition = Vector3.zero;
    private Vector3 previousPulpitPosition = Vector3.zero;
    private Coroutine spawnRoutine;

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
        GameEvents.OnGameOver += StopSpawning;
        GameEvents.OnReturnToLobby += ClearAllPulpits;
    }

    private void OnDisable()
    {
        GameEvents.OnGameStart -= StartGameSpawning;
        GameEvents.OnGameRestart -= RestartSpawning;
        GameEvents.OnGameOver -= StopSpawning;
        GameEvents.OnReturnToLobby -= ClearAllPulpits;
    }

    private void Start()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsPlaying)
        {
            StartGameSpawning();
        }
        else
        {
            SpawnPulpitAt(Vector3.zero);
        }
    }

    public void StartGameSpawning()
    {
        StopSpawning();
        ClearAllPulpits();

        currentPulpitPosition = Vector3.zero;
        previousPulpitPosition = Vector3.zero;

        SpawnPulpitAt(currentPulpitPosition);

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
        RemainingSpawnTime = 0f;
    }

    private IEnumerator SpawnLoopCoroutine()
    {
        while (true)
        {
            float totalSpawnDelay = GameConfig.Instance != null ? GameConfig.Instance.SpawnTime : 2.5f;
            RemainingSpawnTime = totalSpawnDelay;

            while (RemainingSpawnTime > 0f)
            {
                RemainingSpawnTime -= Time.deltaTime;
                yield return null;
            }

            RemainingSpawnTime = 0f;

            activePulpits.RemoveAll(item => item == null);

            while (activePulpits.Count >= 2)
            {
                activePulpits.RemoveAll(item => item == null);
                yield return null;
            }

            Vector3 nextPos = GetNextAdjacentPosition();

            previousPulpitPosition = currentPulpitPosition;
            currentPulpitPosition = nextPos;

            SpawnPulpitAt(currentPulpitPosition);
        }
    }

    private void SpawnPulpitAt(Vector3 position)
    {
        if (pulpitPrefab == null)
        {
            Debug.LogError("[PulpitManager] Pulpit Prefab is not assigned in the Inspector!");
            return;
        }

        GameObject newPulpit = Instantiate(pulpitPrefab, position, Quaternion.identity, transform);
        newPulpit.transform.localScale = new Vector3(platformSize, 0.5f, platformSize);

        activePulpits.Add(newPulpit);
        GameEvents.TriggerPulpitSpawned(position);
    }

    private Vector3 GetNextAdjacentPosition()
    {
        List<Vector3> validPositions = new List<Vector3>();

        foreach (Vector3 dir in adjacentDirections)
        {
            Vector3 candidatePos = currentPulpitPosition + (dir * platformSize);

            bool isOccupied = false;
            foreach (GameObject active in activePulpits)
            {
                if (active != null && Vector3.Distance(active.transform.position, candidatePos) < 1f)
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
        StopSpawning();
        foreach (GameObject pulpit in activePulpits)
        {
            if (pulpit != null)
                Destroy(pulpit);
        }
        activePulpits.Clear();
    }
}
