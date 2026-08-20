using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages spawning and positioning of Pulpit platforms.
/// Enforces max 2 simultaneous platforms, adjacent grid offsets, and prevents duplicate/backtracking positions.
/// </summary>
public class PulpitManager : MonoBehaviour
{
    [Header("Platform Prefab")]
    [SerializeField] private GameObject pulpitPrefab;

    [Header("Grid Configuration")]
    [Tooltip("Size/spacing of platforms. 5 units makes hopping snappy and fast-paced!")]
    [SerializeField] private float platformSize = 5f;

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
        StartGameSpawning();
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

    private IEnumerator SpawnLoopCoroutine()
    {
        while (true)
        {
            float spawnDelay = GameConfig.Instance != null ? GameConfig.Instance.SpawnTime : 2.5f;
            yield return new WaitForSeconds(spawnDelay);

            activePulpits.RemoveAll(item => item == null);

            // Wait if 2 pulpits are already active
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
        // Automatically match platform scale to configured platformSize
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
