using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Prince of Persia / Braid style Time Rewind Engine:
/// - High-frequency circular snapshot recorder of player state
/// - Rewinds player motion arc, platform timers, and un-shatters collapsed platforms
/// - Smooth landing with zero floating stutter
/// </summary>
public class RewindManager : MonoBehaviour
{
    public static RewindManager Instance { get; private set; }

    [Header("Rewind Settings")]
    [Tooltip("How many seconds of history to record")]
    [SerializeField] private float maxRecordSeconds = 3.5f;

    [Tooltip("Speed multiplier for reverse playback (2.2x = rewinds in ~1.5s)")]
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

        public PlayerSnapshot(Vector3 pos, Quaternion rot)
        {
            position = pos;
            rotation = rot;
        }
    }

    private readonly LinkedList<PlayerSnapshot> snapshotBuffer = new LinkedList<PlayerSnapshot>();
    private int currentCharges;
    private bool isRewinding = false;
    private Coroutine rewindRoutine;

    public bool IsRewinding => isRewinding;
    public bool CanRewind => currentCharges > 0 && !isRewinding && snapshotBuffer.Count > 15;
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

        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

        if (playerTransform == null)
        {
            FindPlayerReferences();
            if (playerTransform == null) return;
        }

        snapshotBuffer.AddLast(new PlayerSnapshot(playerTransform.position, playerTransform.rotation));

        int maxSnapshots = Mathf.RoundToInt(maxRecordSeconds / Time.fixedDeltaTime);
        while (snapshotBuffer.Count > maxSnapshots)
        {
            snapshotBuffer.RemoveFirst();
        }
    }

    /// <summary>
    /// Initiates reverse time playback across player, platforms, and timers.
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
            playerRigidbody.linearVelocity = Vector3.zero;
        }

        // Rewind playback loop
        while (snapshotBuffer.Count > 0)
        {
            PlayerSnapshot snapshot = snapshotBuffer.Last.Value;
            snapshotBuffer.RemoveLast();

            if (playerTransform != null)
            {
                playerTransform.position = snapshot.position;
                playerTransform.rotation = snapshot.rotation;
            }

            int framesToSkip = Mathf.Max(1, Mathf.RoundToInt(rewindPlaybackSpeed));
            for (int i = 0; i < framesToSkip - 1 && snapshotBuffer.Count > 0; i++)
            {
                snapshotBuffer.RemoveLast();
            }

            yield return new WaitForFixedUpdate();
        }

        // Seamless landing back on the platform
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
