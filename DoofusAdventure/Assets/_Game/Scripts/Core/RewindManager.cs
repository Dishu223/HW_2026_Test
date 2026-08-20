using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Prince of Persia / Braid style Time Rewind Engine:
/// - High-frequency circular snapshot recorder of player state
/// - Intercepts fatal falls and plays reverse movement arc backwards in slow-motion
/// - Manages 3 Sand of Time charges with HUD event broadcasting
/// </summary>
public class RewindManager : MonoBehaviour
{
    public static RewindManager Instance { get; private set; }

    [Header("Rewind Settings")]
    [Tooltip("How many seconds of history to record")]
    [SerializeField] private float maxRecordSeconds = 3.5f;

    [Tooltip("Speed multiplier for reverse playback (2.0x = rewinds twice as fast)")]
    [SerializeField] private float rewindPlaybackSpeed = 2.2f;

    [Tooltip("Maximum rewind charges per run")]
    [SerializeField] private int maxCharges = 3;

    [Header("Player Target")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Rigidbody playerRigidbody;

    public struct PlayerSnapshot
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity;

        public PlayerSnapshot(Vector3 pos, Quaternion rot, Vector3 vel)
        {
            position = pos;
            rotation = rot;
            velocity = vel;
        }
    }

    private readonly LinkedList<PlayerSnapshot> snapshotBuffer = new LinkedList<PlayerSnapshot>();
    private int currentCharges;
    private bool isRewinding = false;
    private Coroutine rewindRoutine;

    public bool IsRewinding => isRewinding;
    public bool CanRewind => currentCharges > 0 && !isRewinding && snapshotBuffer.Count > 10;
    public int CurrentCharges => currentCharges;
    public int MaxCharges => maxCharges;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        FindPlayerReferences();
        ResetCharges();
    }

    private void OnEnable()
    {
        GameEvents.OnGameStart += HandleGameStart;
        GameEvents.OnGameRestart += HandleGameStart;
    }

    private void OnDisable()
    {
        GameEvents.OnGameStart -= HandleGameStart;
        GameEvents.OnGameRestart -= HandleGameStart;
    }

    private void FindPlayerReferences()
    {
        if (playerTransform == null)
        {
            DoofusController doofus = FindAnyObjectByType<DoofusController>();
            if (doofus != null)
            {
                playerTransform = doofus.transform;
                playerRigidbody = doofus.GetComponent<Rigidbody>();
            }
        }
    }

    private void HandleGameStart()
    {
        ResetCharges();
        snapshotBuffer.Clear();
        isRewinding = false;
    }

    public void ResetCharges()
    {
        currentCharges = maxCharges;
        GameEvents.TriggerRewindChargesChanged(currentCharges, maxCharges);
    }

    private void FixedUpdate()
    {
        if (isRewinding) return;

        // Only record history during active gameplay
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

        if (playerTransform == null)
        {
            FindPlayerReferences();
            if (playerTransform == null) return;
        }

        Vector3 vel = playerRigidbody != null ? playerRigidbody.linearVelocity : Vector3.zero;
        snapshotBuffer.AddLast(new PlayerSnapshot(playerTransform.position, playerTransform.rotation, vel));

        // Maintain fixed duration buffer
        int maxSnapshots = Mathf.RoundToInt(maxRecordSeconds / Time.fixedDeltaTime);
        while (snapshotBuffer.Count > maxSnapshots)
        {
            snapshotBuffer.RemoveFirst();
        }
    }

    /// <summary>
    /// Initiates reverse time playback, consuming 1 Sand Charge.
    /// </summary>
    public void TriggerRewind()
    {
        if (!CanRewind) return;

        currentCharges--;
        GameEvents.TriggerRewindChargesChanged(currentCharges, maxCharges);

        if (rewindRoutine != null) StopCoroutine(rewindRoutine);
        rewindRoutine = StartCoroutine(RewindPlaybackCoroutine());
    }

    private IEnumerator RewindPlaybackCoroutine()
    {
        isRewinding = true;
        GameEvents.TriggerRewindStart();

        if (playerRigidbody != null)
        {
            playerRigidbody.isKinematic = true;
        }

        // Play back snapshots in reverse
        while (snapshotBuffer.Count > 0)
        {
            PlayerSnapshot snapshot = snapshotBuffer.Last.Value;
            snapshotBuffer.RemoveLast();

            if (playerTransform != null)
            {
                playerTransform.position = snapshot.position;
                playerTransform.rotation = snapshot.rotation;
            }

            // Skip frames based on playback speed
            int framesToSkip = Mathf.Max(1, Mathf.RoundToInt(rewindPlaybackSpeed));
            for (int i = 0; i < framesToSkip - 1 && snapshotBuffer.Count > 0; i++)
            {
                snapshotBuffer.RemoveLast();
            }

            yield return new WaitForFixedUpdate();
        }

        // Settle player back safely on ground
        if (playerRigidbody != null)
        {
            playerRigidbody.isKinematic = false;
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }

        isRewinding = false;
        GameEvents.TriggerRewindComplete();
    }
}
