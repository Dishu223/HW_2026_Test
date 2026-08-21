using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controls Doofus physics movement, Shift Dash ability, and detects fatal falls.
/// Automatically equips DoofusLocomotionVFX for visual game juice.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class DoofusController : MonoBehaviour
{
    [Header("Fall Detection")]
    [SerializeField] private float fallThresholdY = -1.5f;

    [Header("Dash Ability Tuning")]
    [SerializeField] private float dashSpeedMultiplier = 3.2f;
    [SerializeField] private float dashDuration = 0.22f;
    [SerializeField] private float dashCooldown = 0.85f;

    private Rigidbody rb;
    private Vector3 moveInput;
    private bool isInputActive = false;
    private bool isRewinding = false;
    private float fallGraceCooldown = 0f;

    private bool isDashing = false;
    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;
    private Vector3 dashDirection = Vector3.forward;

    public Vector3 MoveInput => moveInput;
    public bool IsMoving => isInputActive && (moveInput.sqrMagnitude > 0.01f || isDashing);
    public bool IsDashing => isDashing;
    public float DashProgress => isDashing ? Mathf.Clamp01(dashTimer / dashDuration) : 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.useGravity = true;

        transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        if (GetComponent<DoofusLocomotionVFX>() == null)
        {
            gameObject.AddComponent<DoofusLocomotionVFX>();
        }
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            isInputActive = GameManager.Instance.IsPlaying;
        }
    }

    private void OnEnable()
    {
        GameEvents.OnGameStart += HandleGameStart;
        GameEvents.OnGameOver += HandleGameOver;
        GameEvents.OnGameRestart += HandleGameStart;
        GameEvents.OnRewindStart += HandleRewindStart;
        GameEvents.OnRewindComplete += HandleRewindComplete;
        GameEvents.OnReturnToLobby += HandleReturnToLobby;
    }

    private void OnDisable()
    {
        GameEvents.OnGameStart -= HandleGameStart;
        GameEvents.OnGameOver -= HandleGameOver;
        GameEvents.OnGameRestart -= HandleGameStart;
        GameEvents.OnRewindStart -= HandleRewindStart;
        GameEvents.OnRewindComplete -= HandleRewindComplete;
        GameEvents.OnReturnToLobby -= HandleReturnToLobby;
    }

    private void Update()
    {
        if (fallGraceCooldown > 0f)
        {
            fallGraceCooldown -= Time.deltaTime;
        }

        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.deltaTime;
        }

        if (isDashing)
        {
            dashTimer += Time.deltaTime;
            if (dashTimer >= dashDuration)
            {
                isDashing = false;
            }
        }

        if (!isInputActive || isRewinding)
        {
            moveInput = Vector3.zero;
            return;
        }

        moveInput = ReadKeyboardInput();

        bool shiftPressed = Keyboard.current != null && (Keyboard.current.leftShiftKey.wasPressedThisFrame || Keyboard.current.rightShiftKey.wasPressedThisFrame);
        if (shiftPressed && dashCooldownTimer <= 0f && !isDashing)
        {
            TriggerDash();
        }

        if (fallGraceCooldown <= 0f && transform.position.y < fallThresholdY)
        {
            if (RewindManager.Instance != null && RewindManager.Instance.CanRewind)
            {
                RewindManager.Instance.TriggerRewind();
            }
            else
            {
                fallGraceCooldown = 5.0f; // Block repeated calls
                isInputActive = false;
                GameEvents.TriggerDoofusFell();
                GameEvents.TriggerGameOver();
            }
        }
    }

    private void TriggerDash()
    {
        isDashing = true;
        dashTimer = 0f;
        dashCooldownTimer = dashCooldown;

        if (moveInput.sqrMagnitude > 0.05f)
            dashDirection = moveInput.normalized;
        else
            dashDirection = transform.forward;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayDashSound();
        }
    }

    private void FixedUpdate()
    {
        if (!isInputActive || isRewinding) return;

        float baseSpeed = GameConfig.Instance != null ? GameConfig.Instance.PlayerSpeed : 3f;

        if (isDashing)
        {
            float t = dashTimer / dashDuration;
            float speedCurve = Mathf.Lerp(dashSpeedMultiplier, 1.2f, t * t);
            Vector3 targetPosition = rb.position + dashDirection * (baseSpeed * speedCurve) * Time.fixedDeltaTime;
            rb.MovePosition(targetPosition);
        }
        else if (moveInput != Vector3.zero)
        {
            Vector3 targetPosition = rb.position + moveInput * baseSpeed * Time.fixedDeltaTime;
            rb.MovePosition(targetPosition);
        }
    }

    private Vector3 ReadKeyboardInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return Vector3.zero;

        float horizontal = 0f;
        float vertical = 0f;

        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) vertical += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) vertical -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) horizontal += 1f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) horizontal -= 1f;

        Vector3 rawDirection = new Vector3(horizontal, 0f, vertical);
        return rawDirection.sqrMagnitude > 1f ? rawDirection.normalized : rawDirection;
    }

    #region Event Handlers
    private void HandleGameStart()
    {
        isInputActive = true;
        isRewinding = false;
        isDashing = false;
        dashTimer = 0f;
        dashCooldownTimer = 0f;
        fallGraceCooldown = 0f;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = new Vector3(0f, 1f, 0f);
            transform.position = new Vector3(0f, 1f, 0f);
            transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        }
    }

    private void HandleGameOver()
    {
        isInputActive = false;
        isDashing = false;
        moveInput = Vector3.zero;
    }

    private void HandleReturnToLobby()
    {
        isInputActive = false;
        isDashing = false;
        moveInput = Vector3.zero;
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.position = new Vector3(0f, 1f, 0f);
        }
    }

    private void HandleRewindStart()
    {
        isRewinding = true;
        isInputActive = false;
        isDashing = false;
        moveInput = Vector3.zero;
    }

    private void HandleRewindComplete()
    {
        isRewinding = false;
        isInputActive = true;
        isDashing = false;
        fallGraceCooldown = 1.5f;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
    #endregion
}
