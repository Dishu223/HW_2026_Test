using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 3D Cartoon Mesh Dust Puff Engine:
/// - Spawns puffs at 0.70m behind Doofus (well outside his 0.50m body sphere!)
/// - Alternating left/right foot placement
/// - 100% visible on top of every platform tile!
/// </summary>
public class DoofusLocomotionVFX : MonoBehaviour
{
    [Header("Locomotion Tuning")]
    [SerializeField] private float stepInterval = 0.12f;

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
    private bool isLeftFoot = false;
    private Vector3 lastVelocity = Vector3.zero;

    private void Awake()
    {
        controller = GetComponent<DoofusController>();
        rb = GetComponent<Rigidbody>();

        // Create clean distinct cartoon dust material
        Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
        if (litShader == null) litShader = Shader.Find("Standard");

        sharedPuffMaterial = new Material(litShader);
        if (sharedPuffMaterial.HasProperty("_BaseColor"))
            sharedPuffMaterial.SetColor("_BaseColor", new Color(0.95f, 0.95f, 1f, 1f));
        else
            sharedPuffMaterial.color = new Color(0.95f, 0.95f, 1f, 1f);

        // Pre-warm pool of 20 dust spheres
        for (int i = 0; i < 20; i++)
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

        // Alternate feet (left/right foot)
        isLeftFoot = !isLeftFoot;
        float sideOffset = isLeftFoot ? -0.28f : 0.28f;

        // Spawn at 0.65m behind Doofus and 0.28m to the side (completely outside his 0.50m body sphere!)
        Vector3 forwardOffset = -transform.forward * 0.65f;
        Vector3 lateralOffset = transform.right * sideOffset;
        Vector3 spawnPos = new Vector3(
            transform.position.x + forwardOffset.x + lateralOffset.x,
            0.36f, // 11cm above the Y = 0.25m platform surface
            transform.position.z + forwardOffset.z + lateralOffset.z
        );

        puff.transform.position = spawnPos;
        puff.transform.localScale = Vector3.zero;
        puff.SetActive(true);

        // Drift backward and float gently upward
        Vector3 vel = forwardOffset.normalized * 0.8f + Vector3.up * Random.Range(0.6f, 1.1f);
        float targetSize = Random.Range(0.32f, 0.46f);

        activePuffs.Add(new ActivePuff
        {
            gameObject = puff,
            transform = puff.transform,
            initialScale = Vector3.one * targetSize,
            velocity = vel,
            lifetime = 0f,
            maxLifetime = 0.25f
        });
    }

    private void SpawnSkidPuffs()
    {
        for (int i = 0; i < 5; i++)
        {
            GameObject puff = GetPuffFromPool();
            if (puff == null) continue;

            float angle = (i / 5f) * Mathf.PI * 2f;
            Vector3 radialOffset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 0.75f;

            Vector3 spawnPos = new Vector3(
                transform.position.x + radialOffset.x,
                0.36f,
                transform.position.z + radialOffset.z
            );

            puff.transform.position = spawnPos;
            puff.transform.localScale = Vector3.zero;
            puff.SetActive(true);

            Vector3 vel = radialOffset.normalized * 1.8f + Vector3.up * Random.Range(0.8f, 1.4f);
            float targetSize = Random.Range(0.38f, 0.55f);

            activePuffs.Add(new ActivePuff
            {
                gameObject = puff,
                transform = puff.transform,
                initialScale = Vector3.one * targetSize,
                velocity = vel,
                lifetime = 0f,
                maxLifetime = 0.30f
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

            float progress = p.lifetime / p.maxLifetime;

            p.transform.position += p.velocity * Time.deltaTime;

            // Pop big, then shrink to zero
            float scaleMultiplier = (progress < 0.25f)
                ? Mathf.Lerp(0.2f, 1.3f, progress / 0.25f)
                : Mathf.Lerp(1.3f, 0f, (progress - 0.25f) / 0.75f);

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
