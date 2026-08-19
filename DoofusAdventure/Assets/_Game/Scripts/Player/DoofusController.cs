using UnityEngine;

/// <summary>
/// Controls Doofus physics-based movement using WASD or Arrow keys.
/// Movement speed is fetched dynamically from GameConfig (loaded from JSON).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class DoofusController : MonoBehaviour
{
    [Header("Fall Detection")]
    [SerializeField] private float fallThresholdY = -5f;

    private Rigidbody rb;
    private Vector3 moveInput;
    private bool isInputActive = true;
    private bool hasFallen = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
        // Configure Rigidbody constraints so Doofus slides upright without tumbling
        rb.freezeRotation = true;
        rb.useGravity = true;
    }

    private void OnEnable()
    {
        GameEvents.OnGameStart += HandleGameStart;
        GameEvents.OnGameOver += HandleGameOver;
        GameEvents.OnRewindStart += HandleRewindStart;
        GameEvents.OnRewindComplete += HandleRewindComplete;
    }

    private void OnDisable()
    {
        GameEvents.OnGameStart -= HandleGameStart;
        GameEvents.OnGameOver -= HandleGameOver;
        GameEvents.OnRewindStart -= HandleRewindStart;
        GameEvents.OnRewindComplete -= HandleRewindComplete;
    }

    private void Update()
    {
        if (!isInputActive)
        {
            moveInput = Vector3.zero;
            return;
        }

        // Read movement input (supports WASD and Arrow keys)
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        // Combine into world-space direction on X-Z plane
        Vector3 rawDirection = new Vector3(horizontal, 0f, vertical);

        // Normalize so diagonal movement is not faster than cardinal movement
        moveInput = rawDirection.sqrMagnitude > 1f ? rawDirection.normalized : rawDirection;

        // Check if Doofus fell below the platform threshold
        if (!hasFallen && transform.position.y < fallThresholdY)
        {
            hasFallen = true;
            GameEvents.TriggerDoofusFell();
        }
    }

    private void FixedUpdate()
    {
        if (!isInputActive || moveInput == Vector3.zero) return;

        // Fetch configured speed from GameConfig (or fallback to 3f if config is uninitialized)
        float speed = GameConfig.Instance != null ? GameConfig.Instance.PlayerSpeed : 3f;

        // Move physics body smoothly using MovePosition
        Vector3 targetPosition = rb.position + moveInput * speed * Time.fixedDeltaTime;
        rb.MovePosition(targetPosition);
    }

    #region Event Handlers
    private void HandleGameStart()
    {
        isInputActive = true;
        hasFallen = false;
    }

    private void HandleGameOver()
    {
        isInputActive = false;
        moveInput = Vector3.zero;
    }

    private void HandleRewindStart()
    {
        isInputActive = false;
        rb.isKinematic = true;
    }

    private void HandleRewindComplete()
    {
        isInputActive = true;
        hasFallen = false;
        rb.isKinematic = false;
    }
    #endregion
}
