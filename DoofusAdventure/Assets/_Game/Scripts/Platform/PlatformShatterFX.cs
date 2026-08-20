using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls the smooth 3D crumbling shatter effect:
/// - Generates a 6x6 grid of 36 sleek mini-shards
/// - Crumblingly breaks apart and tumbles smoothly downwards into the void
/// - Reassembles gracefully during Time Rewind
/// </summary>
public class PlatformShatterFX : MonoBehaviour
{
    [Header("Shatter Physics")]
    [SerializeField] private int gridSubdivisions = 6; // 6x6 = 36 sleek mini-shards
    [SerializeField] private float separationForce = 20f; // Gentle outward separation
    [SerializeField] private float randomTorqueAmount = 8f;

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

    private void GenerateProceduralShards()
    {
        if (shards.Count > 0) return;

        float shardSizeX = 1f / gridSubdivisions;
        float shardSizeZ = 1f / gridSubdivisions;
        float shardHeight = 1f;

        for (int x = 0; x < gridSubdivisions; x++)
        {
            for (int z = 0; z < gridSubdivisions; z++)
            {
                float localX = -0.5f + (x + 0.5f) * shardSizeX;
                float localZ = -0.5f + (z + 0.5f) * shardSizeZ;
                Vector3 localPos = new Vector3(localX, 0f, localZ);

                GameObject shardObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shardObj.name = $"Shard_{x}_{z}";
                shardObj.transform.SetParent(transform, false);
                shardObj.transform.localPosition = localPos;
                shardObj.transform.localScale = new Vector3(shardSizeX * 0.92f, shardHeight * 0.92f, shardSizeZ * 0.92f);

                Rigidbody rb = shardObj.GetComponent<Rigidbody>();
                if (rb == null) rb = shardObj.AddComponent<Rigidbody>();
                rb.mass = 0.2f;
                rb.linearDamping = 0.8f; // Smooth floaty air resistance
                rb.angularDamping = 1.0f;
                rb.isKinematic = true;
                rb.useGravity = false;

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

    public void Explode(Color platformColor, Material sharedMaterial)
    {
        isShattered = true;
        isRewinding = false;

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

            // Gentle outward separation vector + smooth downward gravity tumble
            Vector2 randCircle = Random.insideUnitCircle.normalized * Random.Range(1f, 3f);
            Vector3 pushDirection = new Vector3(shard.initialLocalPos.x * 2.5f + randCircle.x, -0.5f, shard.initialLocalPos.z * 2.5f + randCircle.y).normalized;

            shard.rigidbody.linearVelocity = pushDirection * separationForce * 0.25f;
            shard.rigidbody.AddTorque(Random.insideUnitSphere * randomTorqueAmount, ForceMode.Impulse);
        }
    }

    private void FixedUpdate()
    {
        if (isShattered && !isRewinding)
        {
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
