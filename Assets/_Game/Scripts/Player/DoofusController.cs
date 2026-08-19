using UnityEngine;

// Handles player input, physics-based movement, and fall detection.
[RequireComponent(typeof(Rigidbody))]
public class DoofusController : MonoBehaviour
{
    [Header("Physics & Ground Check")]
    [SerializeField] private float fallThresholdY = -4f;
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody rb;
    private Vector3 moveInput;
    private bool hasFallen = false;

    public Vector3 CurrentVelocity => rb != null ? rb.linearVelocity : Vector3.zero;
    public Vector3 CurrentMoveInput => moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
        // Ensure physics doesn't accidentally tip our snowman over
        rb.freezeRotation = true;
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void OnEnable()
    {
        GameEvents.OnGameStart += HandleGameStart;
        GameEvents.OnGameRestart += HandleGameRestart;
        GameEvents.OnGameOver += HandleGameOver;
    }

    private void OnDisable()
    {
        GameEvents.OnGameStart -= HandleGameStart;
        GameEvents.OnGameRestart -= HandleGameRestart;
        GameEvents.OnGameOver -= HandleGameOver;
    }

    private void Update()
    {
        // Only accept input during active gameplay
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
        {
            moveInput = Vector3.zero;
            return;
        }

        // Read standard input (WASD and Arrow keys)
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        // Normalize diagonal movement so player doesn't move faster diagonally
        Vector3 rawDirection = new Vector3(horizontal, 0f, vertical);
        moveInput = rawDirection.magnitude > 1f ? rawDirection.normalized : rawDirection;

        // Check if Doofus fell off into the void
        if (!hasFallen && transform.position.y < fallThresholdY)
        {
            hasFallen = true;
            GameEvents.TriggerDoofusFell();
        }
    }

    private void FixedUpdate()
    {
        // Stop physics movement if not actively playing
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
        {
            return;
        }

        // Apply movement using speed configured in game_data.json
        float speed = GameConfig.Instance != null ? GameConfig.Instance.PlayerSpeed : 3f;
        Vector3 targetVelocity = new Vector3(moveInput.x * speed, rb.linearVelocity.y, moveInput.z * speed);
        rb.linearVelocity = targetVelocity;
    }

    private void HandleGameStart()
    {
        hasFallen = false;
        rb.isKinematic = false;
    }

    private void HandleGameRestart()
    {
        hasFallen = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.position = new Vector3(0f, 1f, 0f);
        transform.rotation = Quaternion.identity;
    }

    private void HandleGameOver()
    {
        moveInput = Vector3.zero;
    }

    public void TeleportTo(Vector3 position)
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.position = position;
        hasFallen = false;
    }
}
