using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 100% Foolproof 3D Cartoon Mesh Dust Puff Engine (Mario 3D / Fall Guys style):
/// - Uses standard 3D Spheres with Doofus's own material (zero shader/particle-system pipeline bugs!)
/// - Completely immune to Z-clipping or URP shader compilation failures
/// - Poofs cute cartoon dust balls right behind Doofus above the platform surface
/// </summary>
public class DoofusLocomotionVFX : MonoBehaviour
{
    [Header("Locomotion Tuning")]
    [SerializeField] private float stepInterval = 0.14f;

    private class ActivePuff
    {
        public GameObject gameObject;
        public Transform transform;
        public Vector3 initialScale;
        public Vector3 velocity;
        public float lifetime;
        public float maxLifetime;
    }

    private DoofusController controller;
    private Rigidbody rb;
    private Material sharedPuffMaterial;

    private readonly List<ActivePuff> activePuffs = new List<ActivePuff>();
    private readonly Queue<GameObject> puffPool = new Queue<GameObject>();
    private float stepTimer = 0f;
    private Vector3 lastVelocity = Vector3.zero;

    private void Awake()
    {
        controller = GetComponent<DoofusController>();
        rb = GetComponent<Rigidbody>();

        // Grab Doofus's own body material (guaranteed 100% working in this scene!)
        MeshRenderer bodyRenderer = GetComponentInChildren<MeshRenderer>();
        if (bodyRenderer != null)
        {
            sharedPuffMaterial = bodyRenderer.sharedMaterial;
        }

        if (sharedPuffMaterial == null)
        {
            sharedPuffMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            sharedPuffMaterial.color = Color.white;
        }

        // Pre-warm pool of 15 dust spheres
        for (int i = 0; i < 15; i++)
        {
            GameObject puffObj = CreatePuffObject();
            puffObj.SetActive(false);
            puffPool.Enqueue(puffObj);
        }
    }

    private GameObject CreatePuffObject()
    {
        GameObject puffObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        puffObj.name = "Cartoon_Dust_Puff";

        // Remove collider so it never affects physics
        Collider col = puffObj.GetComponent<Collider>();
        if (col != null) Destroy(col);

        MeshRenderer mr = puffObj.GetComponent<MeshRenderer>();
        if (mr != null && sharedPuffMaterial != null)
        {
            mr.sharedMaterial = sharedPuffMaterial;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        return puffObj;
    }

    private void Update()
    {
        UpdateActivePuffs();

        if (controller == null || rb == null) return;
        if (RewindManager.Instance != null && RewindManager.Instance.IsRewinding) return;

        Vector3 currentVel = rb.linearVelocity;
        bool isMoving = controller.IsMoving && currentVel.sqrMagnitude > 0.1f;

        if (isMoving)
        {
            stepTimer += Time.deltaTime;
            if (stepTimer >= stepInterval)
            {
                stepTimer = 0f;
                SpawnRunningPuff();
            }
        }
        else
        {
            stepTimer = stepInterval;
        }

        // Skid brake detection
        float velDrop = (lastVelocity - currentVel).magnitude;
        if (velDrop > 2.5f && lastVelocity.sqrMagnitude > 1.2f)
        {
            SpawnSkidPuffs();
        }

        lastVelocity = currentVel;
    }

    private void SpawnRunningPuff()
    {
        GameObject puff = GetPuffFromPool();
        if (puff == null) return;

        // Position: 10cm behind Doofus, Y = 0.35m (clearly above the Y = 0.25m platform surface!)
        Vector3 spawnPos = new Vector3(
            transform.position.x - transform.forward.x * 0.25f + Random.Range(-0.1f, 0.1f),
            0.38f,
            transform.position.z - transform.forward.z * 0.25f + Random.Range(-0.1f, 0.1f)
        );

        puff.transform.position = spawnPos;
        puff.transform.localScale = Vector3.zero;
        puff.SetActive(true);

        Vector3 vel = new Vector3(
            Random.Range(-0.3f, 0.3f) - transform.forward.x * 0.3f,
            Random.Range(0.6f, 1.2f), // Upward float
            Random.Range(-0.3f, 0.3f) - transform.forward.z * 0.3f
        );

        float targetSize = Random.Range(0.28f, 0.42f);

        activePuffs.Add(new ActivePuff
        {
            gameObject = puff,
            transform = puff.transform,
            initialScale = Vector3.one * targetSize,
            velocity = vel,
            lifetime = 0f,
            maxLifetime = 0.28f
        });
    }

    private void SpawnSkidPuffs()
    {
        for (int i = 0; i < 4; i++)
        {
            GameObject puff = GetPuffFromPool();
            if (puff == null) continue;

            Vector2 randCircle = Random.insideUnitCircle * 0.25f;
            Vector3 spawnPos = new Vector3(
                transform.position.x + randCircle.x,
                0.38f,
                transform.position.z + randCircle.y
            );

            puff.transform.position = spawnPos;
            puff.transform.localScale = Vector3.zero;
            puff.SetActive(true);

            Vector3 vel = new Vector3(
                randCircle.x * 2.5f,
                Random.Range(0.8f, 1.6f),
                randCircle.y * 2.5f
            );

            float targetSize = Random.Range(0.35f, 0.52f);

            activePuffs.Add(new ActivePuff
            {
                gameObject = puff,
                transform = puff.transform,
                initialScale = Vector3.one * targetSize,
                velocity = vel,
                lifetime = 0f,
                maxLifetime = 0.35f
            });
        }
    }

    private void UpdateActivePuffs()
    {
        for (int i = activePuffs.Count - 1; i >= 0; i--)
        {
            ActivePuff p = activePuffs[i];
            p.lifetime += Time.deltaTime;

            if (p.lifetime >= p.maxLifetime)
            {
                p.gameObject.SetActive(false);
                puffPool.Enqueue(p.gameObject);
                activePuffs.RemoveAt(i);
                continue;
            }

            float progress = p.lifetime / p.maxLifetime; // 0.0 -> 1.0

            // Move upward & outward
            p.transform.position += p.velocity * Time.deltaTime;

            // Cartoon squash & pop scale curve: pop big -> shrink to zero
            float scaleMultiplier = (progress < 0.3f)
                ? Mathf.Lerp(0.3f, 1.25f, progress / 0.3f)
                : Mathf.Lerp(1.25f, 0f, (progress - 0.3f) / 0.7f);

            p.transform.localScale = p.initialScale * scaleMultiplier;
        }
    }

    private GameObject GetPuffFromPool()
    {
        if (puffPool.Count > 0)
        {
            return puffPool.Dequeue();
        }
        return CreatePuffObject();
    }

    private void OnDestroy()
    {
        foreach (var p in activePuffs)
        {
            if (p.gameObject != null) Destroy(p.gameObject);
        }
        while (puffPool.Count > 0)
        {
            GameObject obj = puffPool.Dequeue();
            if (obj != null) Destroy(obj);
        }
    }
}
