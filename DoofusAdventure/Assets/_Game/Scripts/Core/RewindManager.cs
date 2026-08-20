using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Prince of Persia / Braid style Time Rewind Engine:
/// - Maintains a single deterministic Global WorldTime clock
/// - Rewinds player motion, platform lifetimes, and spawn schedules in perfect mathematical sync
/// - Automatically un-spawns future platforms and resurrects past platforms
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
        public float worldTime;

        public PlayerSnapshot(Vector3 pos, Quaternion rot, float time)
        {
            position = pos;
            rotation = rot;
            worldTime = time;
        }
    }

    private readonly LinkedList<PlayerSnapshot> snapshotBuffer = new LinkedList<PlayerSnapshot>();
    private float worldTime = 0f;
    private int currentCharges;
    private bool isRewinding = false;
    private Coroutine rewindRoutine;

    public float WorldTime => worldTime;
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
        worldTime = 0f;
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

        // Advance global world clock
        worldTime += Time.fixedDeltaTime;

        if (playerTransform == null)
        {
            FindPlayerReferences();
            if (playerTransform == null) return;
        }

        snapshotBuffer.AddLast(new PlayerSnapshot(playerTransform.position, playerTransform.rotation, worldTime));

        int maxSnapshots = Mathf.RoundToInt(maxRecordSeconds / Time.fixedDeltaTime);
        while (snapshotBuffer.Count > maxSnapshots)
        {
            snapshotBuffer.RemoveFirst();
        }
    }

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

        while (snapshotBuffer.Count > 0)
        {
            PlayerSnapshot snapshot = snapshotBuffer.Last.Value;
            snapshotBuffer.RemoveLast();

            // Rewind global world time to match exact historical frame!
            worldTime = snapshot.worldTime;

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
