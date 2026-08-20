using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Prince of Persia Cinematic Time Rewind Engine:
/// 1. Slow-Mo Fall Dilation (0.12x).
/// 2. Reverse Acceleration (0.35x -> 3.2x).
/// 3. Platform Touchdown & Interactive Resume Pause:
///    The game pauses safely on the platform, allowing the player to catch their breath
///    and press WASD / Space / Click to resume when ready!
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
    private bool isWaitingForResume = false;
    private Coroutine rewindRoutine;

    public float WorldTime => worldTime;
    public bool IsRewinding => isRewinding;
    public bool IsWaitingForResume => isWaitingForResume;
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
        isWaitingForResume = false;
    }

    public void ResetCharges()
    {
        currentCharges = maxCharges;
        GameEvents.TriggerRewindChargesChanged(currentCharges, maxCharges);
    }

    private void FixedUpdate()
    {
        if (isRewinding || isWaitingForResume) return;

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
        isWaitingForResume = false;
        GameEvents.TriggerRewindStart();

        // 1. Phase 1: Slow-Motion Fall Dilation
        Time.timeScale = 0.12f;
        float slowMoRealDuration = 0.50f;
        float elapsed = 0f;

        while (elapsed < slowMoRealDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        Time.timeScale = 1.0f;
        if (playerRigidbody != null)
        {
            playerRigidbody.isKinematic = true;
            playerRigidbody.linearVelocity = Vector3.zero;
        }

        // 2. Phase 2: Dynamic Reverse Acceleration
        int initialCount = snapshotBuffer.Count;

        while (snapshotBuffer.Count > 0)
        {
            float progress = 1f - ((float)snapshotBuffer.Count / initialCount);

            float currentSpeed;
            if (progress < 0.20f)
            {
                currentSpeed = Mathf.Lerp(0.35f, 3.2f, progress / 0.20f);
            }
            else if (progress < 0.80f)
            {
                currentSpeed = 3.2f;
            }
            else
            {
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

        // 3. Phase 3: Pause Safely on Platform & Wait for Player Input
        if (playerRigidbody != null)
        {
            playerRigidbody.isKinematic = false;
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }

        // Freeze time and prompt player
        Time.timeScale = 0f;
        isWaitingForResume = true;
        GameEvents.TriggerRewindReadyToResume();

        // Wait for player to press WASD, Arrows, Space, or Click to resume!
        yield return new WaitForSecondsRealtime(0.15f); // Brief debounce

        while (isWaitingForResume)
        {
            Keyboard kb = Keyboard.current;
            Mouse mouse = Mouse.current;

            bool resumePressed = (kb != null && (
                kb.wKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame ||
                kb.sKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame ||
                kb.upArrowKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame ||
                kb.downArrowKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame ||
                kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame
            )) || (mouse != null && mouse.leftButton.wasPressedThisFrame);

            if (resumePressed)
            {
                isWaitingForResume = false;
            }

            yield return null;
        }

        // 4. Resume Gameplay Smoothly!
        Time.timeScale = 1.0f;
        isRewinding = false;
        GameEvents.TriggerRewindComplete();
    }
}
