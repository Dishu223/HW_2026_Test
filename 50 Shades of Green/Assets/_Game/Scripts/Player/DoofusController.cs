using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Controls Doofus character movement and checks for edge falls
[RequireComponent(typeof(Rigidbody))]
public class DoofusController : MonoBehaviour
{
    [Header("Physics & Fall")]
    [SerializeField] private float fallThreshold = -3f;

    private Rigidbody rb;
    private Vector3 spawnPosition = new Vector3(0f, 1.5f, 0f);
    private Vector2 inputVector;
    private bool hasFallen = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; // Keep Doofus upright
    }

    private void OnEnable()
    {
        GameEvents.OnGameStart += ResetPlayer;
        GameEvents.OnGameRestart += ResetPlayer;
    }

    private void OnDisable()
    {
        GameEvents.OnGameStart -= ResetPlayer;
        GameEvents.OnGameRestart -= ResetPlayer;
    }

    private void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
        {
            inputVector = Vector2.zero;
            return;
        }

        ReadInput();

        // Check if Doofus fell off the platform edge into the void
        if (!hasFallen && transform.position.y < fallThreshold)
        {
            hasFallen = true;
            GameEvents.TriggerDoofusFell();
        }
    }

    private void ReadInput()
    {
        float h = 0f;
        float v = 0f;

#if ENABLE_INPUT_SYSTEM
        // New Input System (Unity 6 standard)
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) v += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) v -= 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) h -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) h += 1f;
        }
#else
        // Legacy input fallback
        h = Input.GetAxisRaw("Horizontal");
        v = Input.GetAxisRaw("Vertical");
#endif

        inputVector = new Vector2(h, v).normalized;
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

        float speed = GameConfig.Instance != null ? GameConfig.Instance.PlayerSpeed : 3f;

        // Move horizontally and vertically along the X-Z plane while preserving vertical gravity velocity
        Vector3 targetVelocity = new Vector3(inputVector.x * speed, rb.linearVelocity.y, inputVector.y * speed);
        rb.linearVelocity = targetVelocity;
    }

    public void ResetPlayer()
    {
        hasFallen = false;
        transform.position = spawnPosition;
        rb.linearVelocity = Vector3.zero;
    }
}
