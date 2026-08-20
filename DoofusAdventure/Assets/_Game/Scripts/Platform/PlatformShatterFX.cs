using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls the 3D physics shatter explosion and reverse-reassembly for platforms:
/// - Generates a 4x4 grid of 16 physics debris shards on boot
/// - Explodes outward when platform reaches 0.00s
/// - Rewinds debris trajectories in reverse during Time Rewind, fusing back into the solid platform!
/// </summary>
public class PlatformShatterFX : MonoBehaviour
{
    [Header("Shatter Physics")]
    [SerializeField] private int gridSubdivisions = 4; // 4x4 = 16 shards
    [SerializeField] private float explosionForce = 350f;
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private float upwardModifier = 1.2f;

    private struct DebrisShard
    {
        public GameObject gameObject;
        public Transform transform;
        public Rigidbody rigidbody;
        public MeshRenderer renderer;
        public Vector3 initialLocalPos;
        public Quaternion initialLocalRot;
        public LinkedList<DebrisSnapshot> snapshotHistory;
    }

    private struct DebrisSnapshot
    {
        public Vector3 position;
        public Quaternion rotation;

        public DebrisSnapshot(Vector3 pos, Quaternion rot)
        {
            position = pos;
            rotation = rot;
        }
    }

    private readonly List<DebrisShard> shards = new List<DebrisShard>();
    private bool isShattered = false;
    private bool isRewinding = false;

    private void Awake()
    {
        GenerateProceduralShards();
    }

    private void OnEnable()
    {
        GameEvents.OnRewindStart += HandleRewindStart;
        GameEvents.OnRewindComplete += HandleRewindComplete;
    }

    private void OnDisable()
    {
        GameEvents.OnRewindStart -= HandleRewindStart;
        GameEvents.OnRewindComplete -= HandleRewindComplete;
    }

    /// <summary>
    /// Procedurally creates a 4x4 grid of 16 mini-cube debris shards matching the platform's dimensions.
    /// </summary>
    private void GenerateProceduralShards()
    {
        if (shards.Count > 0) return;

        float shardSizeX = 1f / gridSubdivisions;
        float shardSizeZ = 1f / gridSubdivisions;
        float shardHeight = 1f; // Relative to local platform height

        for (int x = 0; x < gridSubdivisions; x++)
        {
            for (int z = 0; z < gridSubdivisions; z++)
            {
                float localX = -0.5f + (x + 0.5f) * shardSizeX;
                float localZ = -0.5f + (z + 0.5f) * shardSizeZ;
                Vector3 localPos = new Vector3(localX, 0f, localZ);

                GameObject shardObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shardObj.name = $"Debris_{x}_{z}";
                shardObj.transform.SetParent(transform, false);
                shardObj.transform.localPosition = localPos;
                shardObj.transform.localScale = new Vector3(shardSizeX * 0.95f, shardHeight, shardSizeZ * 0.95f);

                // Configure physics
                Rigidbody rb = shardObj.GetComponent<Rigidbody>();
                if (rb == null) rb = shardObj.AddComponent<Rigidbody>();
                rb.mass = 0.5f;
                rb.isKinematic = true;
                rb.useGravity = false;

                // Disable collisions between debris shards and player
                Collider col = shardObj.GetComponent<Collider>();
                if (col != null) col.enabled = false;

                MeshRenderer mr = shardObj.GetComponent<MeshRenderer>();
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                DebrisShard shard = new DebrisShard
                {
                    gameObject = shardObj,
                    transform = shardObj.transform,
                    rigidbody = rb,
                    renderer = mr,
                    initialLocalPos = localPos,
                    initialLocalRot = Quaternion.identity,
                    snapshotHistory = new LinkedList<DebrisSnapshot>()
                };

                shardObj.SetActive(false);
                shards.Add(shard);
            }
        }
    }

    /// <summary>
    /// Triggers outward physics explosion when platform reaches 0.00s.
    /// </summary>
    public void Explode(Color platformColor, Material sharedMaterial)
    {
        isShattered = true;
        isRewinding = false;

        Vector3 explosionCenter = transform.position + new Vector3(0f, -0.5f, 0f);

        for (int i = 0; i < shards.Count; i++)
        {
            DebrisShard shard = shards[i];
            shard.snapshotHistory.Clear();

            shard.gameObject.SetActive(true);
            shard.transform.localPosition = shard.initialLocalPos;
            shard.transform.localRotation = shard.initialLocalRot;

            if (shard.renderer != null && sharedMaterial != null)
            {
                shard.renderer.material = sharedMaterial;
                shard.renderer.material.color = platformColor;
            }

            shard.rigidbody.isKinematic = false;
            shard.rigidbody.useGravity = true;

            // Add outward radial explosive force + random tumbling torque
            shard.rigidbody.AddExplosionForce(explosionForce, explosionCenter, explosionRadius, upwardModifier, ForceMode.Impulse);
            shard.rigidbody.AddTorque(Random.insideUnitSphere * 40f, ForceMode.Impulse);
        }
    }

    private void FixedUpdate()
    {
        if (isShattered && !isRewinding)
        {
            // Record trajectory history for reverse playback
            for (int i = 0; i < shards.Count; i++)
            {
                DebrisShard shard = shards[i];
                shard.snapshotHistory.AddLast(new DebrisSnapshot(shard.transform.position, shard.transform.rotation));

                while (shard.snapshotHistory.Count > 150)
                {
                    shard.snapshotHistory.RemoveFirst();
                }
            }
        }
        else if (isRewinding && isShattered)
        {
            RewindDebrisStep();
        }
    }

    /// <summary>
    /// Plays shattered debris trajectories in reverse during Time Rewind.
    /// </summary>
    public void RewindDebrisStep()
    {
        int finishedCount = 0;
        int skip = 2;

        for (int i = 0; i < shards.Count; i++)
        {
            DebrisShard shard = shards[i];

            if (shard.snapshotHistory.Count > 0)
            {
                for (int s = 0; s < skip && shard.snapshotHistory.Count > 0; s++)
                {
                    DebrisSnapshot snap = shard.snapshotHistory.Last.Value;
                    shard.snapshotHistory.RemoveLast();

                    shard.transform.position = snap.position;
                    shard.transform.rotation = snap.rotation;
                }
            }
            else
            {
                finishedCount++;
                shard.transform.localPosition = shard.initialLocalPos;
                shard.transform.localRotation = shard.initialLocalRot;
            }
        }

        // When all shards have returned to rest position, fuse back into solid platform!
        if (finishedCount >= shards.Count)
        {
            ResetDebris();
        }
    }

    public void ResetDebris()
    {
        isShattered = false;
        for (int i = 0; i < shards.Count; i++)
        {
            DebrisShard shard = shards[i];
            shard.snapshotHistory.Clear();
            shard.rigidbody.isKinematic = true;
            shard.rigidbody.useGravity = false;
            shard.transform.localPosition = shard.initialLocalPos;
            shard.transform.localRotation = shard.initialLocalRot;
            shard.gameObject.SetActive(false);
        }
    }

    private void HandleRewindStart()
    {
        isRewinding = true;
        for (int i = 0; i < shards.Count; i++)
        {
            shards[i].rigidbody.isKinematic = true;
        }
    }

    private void HandleRewindComplete()
    {
        isRewinding = false;
        if (!isShattered)
        {
            ResetDebris();
        }
    }
}
