using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controls Doofus physics-based movement using Unity's New Input System.
/// Default rotation faces camera (180 degrees).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class DoofusController : MonoBehaviour
{
    [Header("Fall Detection")]
    [SerializeField] private float fallThresholdY = -5f;

    private Rigidbody rb;
    private Vector3 moveInput;
    private bool isInputActive = false;
    private bool hasFallen = false;

    public Vector3 MoveInput => moveInput;
    public bool IsMoving => isInputActive && moveInput.sqrMagnitude > 0.01f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.useGravity = true;

        // Face camera at start (Y rotation = 180)
        transform.rotation = Quaternion.Euler(0f, 180f, 0f);
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
        if (!isInputActive)
        {
            moveInput = Vector3.zero;
            return;
        }

        moveInput = ReadKeyboardInput();

        if (!hasFallen && transform.position.y < fallThresholdY)
        {
            hasFallen = true;
            GameEvents.TriggerDoofusFell();
        }
    }

    private void FixedUpdate()
    {
        if (!isInputActive || moveInput == Vector3.zero) return;

        float speed = GameConfig.Instance != null ? GameConfig.Instance.PlayerSpeed : 3f;
        Vector3 targetPosition = rb.position + moveInput * speed * Time.fixedDeltaTime;
        rb.MovePosition(targetPosition);
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
        hasFallen = false;

        if (rb != null)
        {
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
        moveInput = Vector3.zero;
    }

    private void HandleReturnToLobby()
    {
        isInputActive = false;
        moveInput = Vector3.zero;
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.position = new Vector3(0f, 1f, 0f);
            transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        }
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
