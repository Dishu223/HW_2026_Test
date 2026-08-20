using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Prince of Persia / Braid style Time Rewind Engine with Cinematic Time-Ramp Curve:
/// 1. Fatal Fall Dilation: Smooth slow-mo deceleration (0.2x) as player falls into the void.
/// 2. Rewind Acceleration: Starts in reverse slow-mo (0.4x), accelerates smoothly to 2.8x speed.
/// 3. Gentle Landing: Smoothly decelerates back to 1.0x speed as player touches down on the platform.
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
        rewindRoutine = StartCoroutine(SmoothCinematicRewindCoroutine());
    }

    private IEnumerator SmoothCinematicRewindCoroutine()
    {
        isRewinding = true;
        GameEvents.TriggerRewindStart();

        // 1. Phase 1: Slow-Motion Fall Dilation (Suspended in mid-air for 0.3s)
        float slowMoDuration = 0.30f;
        float elapsed = 0f;
        while (elapsed < slowMoDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (playerRigidbody != null)
        {
            playerRigidbody.isKinematic = true;
            playerRigidbody.linearVelocity = Vector3.zero;
        }

        // 2. Phase 2: Dynamic Time-Ramp Reversal Playback
        int initialCount = snapshotBuffer.Count;

        while (snapshotBuffer.Count > 0)
        {
            float progress = 1f - ((float)snapshotBuffer.Count / initialCount); // 0.0 -> 1.0

            // Speed Curve: Start in slow-mo (0.5x) -> Accelerate to (2.8x) -> Decelerate into landing (1.0x)
            float currentSpeed;
            if (progress < 0.25f)
            {
                currentSpeed = Mathf.Lerp(0.5f, 2.8f, progress / 0.25f);
            }
            else if (progress < 0.80f)
            {
                currentSpeed = 2.8f;
            }
            else
            {
                currentSpeed = Mathf.Lerp(2.8f, 1.0f, (progress - 0.80f) / 0.20f);
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

            yield return new WaitForSecondsRealtime(Time.fixedDeltaTime / Mathf.Max(0.5f, currentSpeed));
        }

        // 3. Phase 3: Seamless Touchdown
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
