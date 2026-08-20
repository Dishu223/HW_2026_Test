using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Prince of Persia Cinematic Time Rewind Engine:
/// 1. Dramatic Slow-Motion Dilation (Time.timeScale drops to 0.12x for 0.55s):
///    Doofus visibly floats in super slow motion over the void while the camera glides in.
/// 2. Smooth Reverse Acceleration (0.3x -> 3.2x):
///    Time rolls backwards, starting in slow reverse and building to high-speed playback.
/// 3. Graceful Landing Deceleration (3.2x -> 1.0x):
///    Decelerates gently to set Doofus softly back down on the restored platform.
/// </summary>
public class RewindManager : MonoBehaviour
{
    public static RewindManager Instance { get; private set; }

    [Header("Rewind Settings")]
    [Tooltip("How many seconds of history to record")]
    [SerializeField] private float maxRecordSeconds = 3.5f;

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
        Time.timeScale = 1f;
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
        rewindRoutine = StartCoroutine(CinematicSuperSlowMoRewindCoroutine());
    }

    private IEnumerator CinematicSuperSlowMoRewindCoroutine()
    {
        isRewinding = true;
        GameEvents.TriggerRewindStart();

        // 1. Phase 1: High-Impact Dramatic Slow Motion (Time.timeScale = 0.12x for 0.55s)
        // You visibly see Doofus floating down in super slow motion over the void!
        Time.timeScale = 0.12f;
        float slowMoRealDuration = 0.55f;
        float elapsed = 0f;

        while (elapsed < slowMoRealDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // Freeze physics before beginning reverse travel
        Time.timeScale = 1.0f;
        if (playerRigidbody != null)
        {
            playerRigidbody.isKinematic = true;
            playerRigidbody.linearVelocity = Vector3.zero;
        }

        // 2. Phase 2: Dynamic Reverse Acceleration (0.3x -> 3.2x -> 1.0x)
        int initialCount = snapshotBuffer.Count;

        while (snapshotBuffer.Count > 0)
        {
            float progress = 1f - ((float)snapshotBuffer.Count / initialCount);

            float currentSpeed;
            if (progress < 0.20f)
            {
                // Start reverse in dramatic slow-mo (0.35x), ramping up
                currentSpeed = Mathf.Lerp(0.35f, 3.2f, progress / 0.20f);
            }
            else if (progress < 0.80f)
            {
                // Full high-speed reverse playback
                currentSpeed = 3.2f;
            }
            else
            {
                // Gracefully decelerate down to normal speed (1.0x) for gentle landing
                currentSpeed = Mathf.Lerp(3.2f, 1.0f, (progress - 0.80f) / 0.20f);
            }

            int framesToPop = Mathf.Max(1, Mathf.RoundToInt(currentSpeed));
            for (int i = 0; i < framesToPop && snapshotBuffer.Count > 0; i++)
            {
                PlayerSnapshot snapshot = snapshotBuffer.Last.Value;
                snapshotBuffer.RemoveLast();

                worldTime = snapshot.worldTime;

                if (playerTransform != null)
                {
                    playerTransform.position = snapshot.position;
                    playerTransform.rotation = snapshot.rotation;
                }
            }

            yield return new WaitForSecondsRealtime(Time.fixedDeltaTime / Mathf.Max(0.4f, currentSpeed));
        }

        // 3. Phase 3: Seamless Touchdown
        if (playerRigidbody != null)
        {
            playerRigidbody.isKinematic = false;
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }

        Time.timeScale = 1.0f;
        isRewinding = false;
        GameEvents.TriggerRewindComplete();
    }
}
