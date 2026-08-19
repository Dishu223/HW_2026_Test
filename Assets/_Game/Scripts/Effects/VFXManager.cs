using UnityEngine;

// Central visual effects coordinator. Spawns particle bursts
// in response to platform events, milestones, and player steps.
public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    [Header("Particle Prefabs")]
    [SerializeField] private GameObject landPoofPrefab;
    [SerializeField] private GameObject shatterSparksPrefab;
    [SerializeField] private GameObject spawnBeamPrefab;
    [SerializeField] private GameObject milestoneConfettiPrefab;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        GameEvents.OnPulpitLanded += HandlePulpitLanded;
        GameEvents.OnPulpitDestroyed += HandlePulpitDestroyed;
        GameEvents.OnPulpitSpawned += HandlePulpitSpawned;
        GameEvents.OnMilestoneReached += HandleMilestone;
    }

    private void OnDisable()
    {
        GameEvents.OnPulpitLanded -= HandlePulpitLanded;
        GameEvents.OnPulpitDestroyed -= HandlePulpitDestroyed;
        GameEvents.OnPulpitSpawned -= HandlePulpitSpawned;
        GameEvents.OnMilestoneReached -= HandleMilestone;
    }

    private void HandlePulpitLanded()
    {
        DoofusController player = FindFirstObjectByType<DoofusController>();
        if (player != null && landPoofPrefab != null)
        {
            SpawnVFX(landPoofPrefab, player.transform.position, 1.5f);
        }
    }

    private void HandlePulpitDestroyed(Vector3 position)
    {
        if (shatterSparksPrefab != null)
        {
            SpawnVFX(shatterSparksPrefab, position, 2.0f);
        }
    }

    private void HandlePulpitSpawned(Vector3 position)
    {
        if (spawnBeamPrefab != null)
        {
            SpawnVFX(spawnBeamPrefab, position, 2.0f);
        }
    }

    private void HandleMilestone(int milestone)
    {
        if (milestoneConfettiPrefab != null)
        {
            SpawnVFX(milestoneConfettiPrefab, Camera.main != null ? Camera.main.transform.position + Camera.main.transform.forward * 5f : Vector3.zero, 3.5f);
        }
    }

    private void SpawnVFX(GameObject prefab, Vector3 position, float autoDestroyTime)
    {
        if (prefab == null) return;
        GameObject obj = Instantiate(prefab, position, Quaternion.identity);
        Destroy(obj, autoDestroyTime);
    }
}
