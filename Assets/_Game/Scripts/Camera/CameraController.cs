using System.Collections;
using UnityEngine;

// Smooth isometric camera follow controller with dynamic screen shake feedback.
public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [Header("Target & Positioning")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 14f, -12f);
    [SerializeField] private float followDamping = 5f;

    [Header("Isometric Angles")]
    [SerializeField] private float pitchAngle = 48f;

    private Vector3 currentVelocity;
    private Vector3 shakeOffset = Vector3.zero;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.rotation = Quaternion.Euler(pitchAngle, 0f, 0f);
    }

    private void OnEnable()
    {
        GameEvents.OnPulpitDestroyed += HandlePulpitDestroyed;
        GameEvents.OnMilestoneReached += HandleMilestone;
    }

    private void OnDisable()
    {
        GameEvents.OnPulpitDestroyed -= HandlePulpitDestroyed;
        GameEvents.OnMilestoneReached -= HandleMilestone;
    }

    private void Start()
    {
        if (target == null)
        {
            DoofusController player = FindFirstObjectByType<DoofusController>();
            if (player != null) target = player.transform;
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset + shakeOffset;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, 1f / followDamping);
    }

    private void HandlePulpitDestroyed(Vector3 position)
    {
        // Light shake when a platform explodes
        TriggerScreenShake(0.2f, 0.25f);
    }

    private void HandleMilestone(int milestone)
    {
        // Big celebratory shake on hitting milestone
        TriggerScreenShake(0.35f, 0.4f);
    }

    public void TriggerScreenShake(float intensity, float duration)
    {
        StartCoroutine(ShakeRoutine(intensity, duration));
    }

    private IEnumerator ShakeRoutine(float intensity, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            Vector2 randomCircle = Random.insideUnitCircle * intensity * (1f - (elapsed / duration));
            shakeOffset = new Vector3(randomCircle.x, randomCircle.y, 0f);
            yield return null;
        }

        shakeOffset = Vector3.zero;
    }
}
