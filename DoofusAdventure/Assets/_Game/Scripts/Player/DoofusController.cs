using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controls Doofus physics-based movement using Unity's New Input System.
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

        // Lock rotations so Doofus slides upright without tumbling over
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

        // Read input using Unity 6 New Input System
        moveInput = ReadKeyboardInput();

        // Check if Doofus fell below the threshold
        if (!hasFallen && transform.position.y < fallThresholdY)
        {
            hasFallen = true;
            GameEvents.TriggerDoofusFell();
        }
    }

    private void FixedUpdate()
    {
        if (!isInputActive || moveInput == Vector3.zero) return;

        // Fetch speed from GameConfig (loaded from JSON) with fallback
        float speed = GameConfig.Instance != null ? GameConfig.Instance.PlayerSpeed : 3f;

        // Move physics body smoothly using MovePosition
        Vector3 targetPosition = rb.position + moveInput * speed * Time.fixedDeltaTime;
        rb.MovePosition(targetPosition);
    }

    /// <summary>
    /// Reads WASD and Arrow key input directly via UnityEngine.InputSystem.Keyboard.
    /// Normalizes diagonal movement so diagonal speed matches cardinal speed.
    /// </summary>
    private Vector3 ReadKeyboardInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return Vector3.zero;

        float horizontal = 0f;
        float vertical = 0f;

        // Forward / Backward (W / S or Up / Down arrows)
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) vertical += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) vertical -= 1f;

        // Left / Right (A / D or Left / Right arrows)
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) horizontal += 1f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) horizontal -= 1f;

        Vector3 rawDirection = new Vector3(horizontal, 0f, vertical);

        // Normalize if moving diagonally
        return rawDirection.sqrMagnitude > 1f ? rawDirection.normalized : rawDirection;
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
