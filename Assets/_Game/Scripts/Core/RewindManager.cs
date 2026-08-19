using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Snapshot data recorded 10 times per second during gameplay
[System.Serializable]
public struct GameSnapshot
{
    public Vector3 playerPosition;
    public Quaternion playerRotation;
    public int score;
}

// Manages the Prince of Persia-style Rewind Time mechanic.
// Records past positions and smoothly interpolates backward on demand.
public class RewindManager : MonoBehaviour
{
    public static RewindManager Instance { get; private set; }

    [Header("Rewind Settings")]
    [SerializeField] private float recordInterval = 0.1f; // 10 snapshots per second
    [SerializeField] private int maxHistorySnapshots = 50;  // 5 seconds total history
    [SerializeField] private float rewindPlaybackDuration = 1.6f;

    [Header("Player Reference")]
    [SerializeField] private DoofusController playerController;

    private readonly List<GameSnapshot> snapshotHistory = new List<GameSnapshot>();
    private float recordTimer = 0f;
    private bool isRewinding = false;

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
        GameEvents.OnRewindStart += StartRewindSequence;
        GameEvents.OnGameStart += ClearHistory;
        GameEvents.OnGameRestart += ClearHistory;
    }

    private void OnDisable()
    {
        GameEvents.OnRewindStart -= StartRewindSequence;
        GameEvents.OnGameStart -= ClearHistory;
        GameEvents.OnGameRestart -= ClearHistory;
    }

    private void Start()
    {
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<DoofusController>();
        }
    }

    private void Update()
    {
        if (isRewinding) return;

        // Only record while actively playing
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing)
        {
            recordTimer += Time.deltaTime;
            if (recordTimer >= recordInterval)
            {
                recordTimer = 0f;
                RecordSnapshot();
            }
        }
    }

    private void RecordSnapshot()
    {
        if (playerController == null) return;

        // Keep player only if standing above the void
        if (playerController.transform.position.y < -1f) return;

        GameSnapshot snap = new GameSnapshot
        {
            playerPosition = playerController.transform.position,
            playerRotation = playerController.transform.rotation,
            score = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0
        };

        snapshotHistory.Add(snap);

        // Keep rolling buffer within max size
        if (snapshotHistory.Count > maxHistorySnapshots)
        {
            snapshotHistory.RemoveAt(0);
        }
    }

    private void ClearHistory()
    {
        snapshotHistory.Clear();
        recordTimer = 0f;
        isRewinding = false;
        Time.timeScale = 1.0f;
    }

    private void StartRewindSequence()
    {
        if (isRewinding) return;
        StartCoroutine(RewindRoutine());
    }

    private IEnumerator RewindRoutine()
    {
        isRewinding = true;

        // 1. Slow down time momentarily for dramatic impact
        Time.timeScale = 0.2f;
        yield return new WaitForSecondsRealtime(0.6f);

        // 2. Play backwards through recorded history
        if (snapshotHistory.Count > 0 && playerController != null)
        {
            int totalFrames = snapshotHistory.Count;
            float stepDelay = rewindPlaybackDuration / Mathf.Max(1, totalFrames);

            for (int i = totalFrames - 1; i >= 0; i--)
            {
                GameSnapshot snap = snapshotHistory[i];
                playerController.TeleportTo(snap.playerPosition);
                playerController.transform.rotation = snap.playerRotation;

                if (ScoreManager.Instance != null)
                {
                    ScoreManager.Instance.SetScoreDirectly(snap.score);
                }

                yield return new WaitForSecondsRealtime(stepDelay);
            }
        }
        else if (playerController != null)
        {
            // Fallback: Teleport to origin if no history was recorded yet
            playerController.TeleportTo(new Vector3(0f, 1f, 0f));
        }

        // 3. Clear snapshots and restore normal gameplay
        snapshotHistory.Clear();
        Time.timeScale = 1.0f;
        isRewinding = false;

        GameEvents.TriggerRewindComplete();
        Debug.Log("<color=cyan>[RewindManager]</color> Rewind sequence completed successfully!");
    }
}
