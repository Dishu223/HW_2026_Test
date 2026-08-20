using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages spawning and positioning of Pulpit platforms.
/// Pauses spawning during Rewind and re-synchronizes adjacent platforms on resume.
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
        GameEvents.OnRewindStart += HandleRewindStart;
        GameEvents.OnRewindComplete += HandleRewindComplete;
    }

    private void OnDisable()
    {
        GameEvents.OnGameStart -= StartGameSpawning;
        GameEvents.OnGameRestart -= RestartSpawning;
        GameEvents.OnGameOver -= StopSpawning;
        GameEvents.OnReturnToLobby -= ClearAllPulpits;
        GameEvents.OnRewindStart -= HandleRewindStart;
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
    }

    private void HandleRewindStart()
    {
        StopSpawning();
    }

    private void HandleRewindComplete()
    {
        // Re-synchronize active platforms list
        RefreshActivePulpitsList();

        // Find the platform Doofus is currently standing on
        Pulpit playerPulpit = GetClosestAlivePulpit(Vector3.zero);
        DoofusController doofus = FindAnyObjectByType<DoofusController>();
        if (doofus != null)
        {
            playerPulpit = GetClosestAlivePulpit(doofus.transform.position);
        }

        if (playerPulpit != null)
        {
            currentPulpitPosition = playerPulpit.transform.position;
        }

        // Resume continuous spawn loop smoothly
        StopSpawning();
        spawnRoutine = StartCoroutine(SpawnLoopCoroutine());
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

    private IEnumerator SpawnLoopCoroutine()
    {
        while (true)
        {
            float spawnDelay = GameConfig.Instance != null ? GameConfig.Instance.SpawnTime : 2.5f;
            yield return new WaitForSeconds(spawnDelay);

            activePulpits.RemoveAll(item => item == null || item.IsDestroyed);

            while (activePulpits.Count >= 2)
            {
                activePulpits.RemoveAll(item => item == null || item.IsDestroyed);
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

        GameObject newPulpitObj = Instantiate(pulpitPrefab, position, Quaternion.identity, transform);
        newPulpitObj.transform.localScale = new Vector3(platformSize, 0.5f, platformSize);

        Pulpit pulpitScript = newPulpitObj.GetComponent<Pulpit>();
        if (pulpitScript != null)
        {
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
        StopSpawning();
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
