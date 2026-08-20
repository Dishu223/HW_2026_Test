using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Organic Randomized Platform Shatter Effect:
/// - Generates a bespoke cluster of 8-14 randomized irregular tile shards per platform
/// - Varied shard sizes, shapes, and angles for organic broken-stone look
/// - Radial wave crumble (center collapses, corners tumble outward)
/// - Reassembles seamlessly during Time Rewind
/// </summary>
public class PlatformShatterFX : MonoBehaviour
{
    [Header("Shatter Dynamics")]
    [SerializeField] private float minSeparationForce = 8f;
    [SerializeField] private float maxSeparationForce = 18f;

    private struct DebrisShard
    {
        public GameObject gameObject;
        public Transform transform;
        public Rigidbody rigidbody;
        public MeshRenderer renderer;
        public Vector3 initialLocalPos;
        public Quaternion initialLocalRot;
        public Vector3 initialLocalScale;
        public float delayOffset;
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
        GenerateOrganicRandomizedShards();
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
    /// Generates a randomized cluster of 8-14 irregular stone/tile shards with varied shapes and positions.
    /// </summary>
    private void GenerateOrganicRandomizedShards()
    {
        if (shards.Count > 0) return;

        // Choose random shard count between 9 and 13 for each unique platform
        int shardCount = Random.Range(9, 14);

        for (int i = 0; i < shardCount; i++)
        {
            // Organic scatter across the platform surface
            float angle = (i / (float)shardCount) * Mathf.PI * 2f + Random.Range(-0.3f, 0.3f);
            float radius = (i == 0) ? 0f : Random.Range(0.15f, 0.42f);

            float localX = Mathf.Cos(angle) * radius;
            float localZ = Mathf.Sin(angle) * radius;
            Vector3 localPos = new Vector3(localX, 0f, localZ);

            GameObject shardObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shardObj.name = $"OrganicShard_{i}";
            shardObj.transform.SetParent(transform, false);
            shardObj.transform.localPosition = localPos;

            // Randomized organic rotation & scale
            Quaternion localRot = Quaternion.Euler(0f, Random.Range(0f, 90f), 0f);
            shardObj.transform.localRotation = localRot;

            float sizeX = Random.Range(0.20f, 0.36f);
            float sizeZ = Random.Range(0.20f, 0.36f);
            float height = Random.Range(0.35f, 0.55f);
            Vector3 localScale = new Vector3(sizeX, height, sizeZ);
            shardObj.transform.localScale = localScale;

            Rigidbody rb = shardObj.GetComponent<Rigidbody>();
            if (rb == null) rb = shardObj.AddComponent<Rigidbody>();
            rb.mass = Random.Range(0.12f, 0.22f);
            rb.linearDamping = Random.Range(0.5f, 0.8f);
            rb.angularDamping = Random.Range(0.6f, 1.2f);
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
                initialLocalRot = localRot,
                initialLocalScale = localScale,
                delayOffset = radius * 0.12f, // Ripple collapse from center outward
                snapshotHistory = new LinkedList<DebrisSnapshot>()
            };

            shardObj.SetActive(false);
            shards.Add(shard);
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

            // Subtle shade variation on each piece for gorgeous depth
            if (shard.renderer != null && sharedMaterial != null)
            {
                shard.renderer.material = sharedMaterial;
                float shadeJitter = Random.Range(0.88f, 1.08f);
                Color pieceColor = new Color(
                    Mathf.Clamp01(platformColor.r * shadeJitter),
                    Mathf.Clamp01(platformColor.g * shadeJitter),
                    Mathf.Clamp01(platformColor.b * shadeJitter),
                    platformColor.a
                );
                shard.renderer.material.color = pieceColor;
            }

            shard.rigidbody.isKinematic = false;
            shard.rigidbody.useGravity = true;

            // Organic outward radial tumble
            float distFromCenter = shard.initialLocalPos.magnitude;
            Vector3 pushDir = new Vector3(
                shard.initialLocalPos.x * 2f + Random.Range(-0.3f, 0.3f),
                -0.4f - shard.delayOffset,
                shard.initialLocalPos.z * 2f + Random.Range(-0.3f, 0.3f)
            ).normalized;

            float force = Random.Range(minSeparationForce, maxSeparationForce) * (0.8f + distFromCenter);
            shard.rigidbody.linearVelocity = pushDir * force * 0.25f;
            shard.rigidbody.AddTorque(Random.insideUnitSphere * Random.Range(4f, 12f), ForceMode.Impulse);
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
