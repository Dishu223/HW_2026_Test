using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls the clean, compact 3D crumbling shatter effect:
/// - 3x3 grid of 9 neat, thin floor tile shards (no clutter or giant beams)
/// - Gently separates and tumbles downwards with gravity
/// - Reassembles smoothly during Time Rewind
/// </summary>
public class PlatformShatterFX : MonoBehaviour
{
    [Header("Shatter Physics")]
    [SerializeField] private int gridSubdivisions = 3; // 3x3 = 9 neat, distinct tile pieces
    [SerializeField] private float separationForce = 12f;
    [SerializeField] private float randomTorqueAmount = 5f;

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

        for (int x = 0; x < gridSubdivisions; x++)
        {
            for (int z = 0; z < gridSubdivisions; z++)
            {
                float localX = -0.5f + (x + 0.5f) * shardSizeX;
                float localZ = -0.5f + (z + 0.5f) * shardSizeZ;
                Vector3 localPos = new Vector3(localX, 0f, localZ);

                GameObject shardObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shardObj.name = $"TileShard_{x}_{z}";
                shardObj.transform.SetParent(transform, false);
                shardObj.transform.localPosition = localPos;

                // Thin, compact, lightweight tile dimensions
                shardObj.transform.localScale = new Vector3(shardSizeX * 0.70f, 0.40f, shardSizeZ * 0.70f);

                Rigidbody rb = shardObj.GetComponent<Rigidbody>();
                if (rb == null) rb = shardObj.AddComponent<Rigidbody>();
                rb.mass = 0.15f;
                rb.linearDamping = 0.6f;
                rb.angularDamping = 0.8f;
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

            // Gentle natural outward crumble
            Vector3 pushDirection = new Vector3(shard.initialLocalPos.x * 1.5f, -0.4f, shard.initialLocalPos.z * 1.5f).normalized;
            shard.rigidbody.linearVelocity = pushDirection * separationForce * 0.3f;
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
