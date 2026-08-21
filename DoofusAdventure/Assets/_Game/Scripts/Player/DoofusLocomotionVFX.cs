using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 3D Cartoon Dust Puff & Left/Right Footstep Audio Engine:
/// - Spawns delicate cartoon dust puffs alternating left/right heels
/// - Plays alternating Left / Right Boop sounds via SoundManager
/// </summary>
public class DoofusLocomotionVFX : MonoBehaviour
{
    [Header("Locomotion Tuning")]
    [SerializeField] private float stepInterval = 0.13f;

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
    private Material sharedPuffMaterial;

    private readonly List<ActivePuff> activePuffs = new List<ActivePuff>();
    private readonly Queue<GameObject> puffPool = new Queue<GameObject>();
    private float stepTimer = 0f;
    private bool isLeftFoot = false;
    private bool wasMovingLastFrame = false;

    private void Awake()
    {
        controller = GetComponent<DoofusController>();

        Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
        if (litShader == null) litShader = Shader.Find("Standard");

        sharedPuffMaterial = new Material(litShader);
        if (sharedPuffMaterial.HasProperty("_BaseColor"))
            sharedPuffMaterial.SetColor("_BaseColor", new Color(0.96f, 0.96f, 1f, 0.85f));
        else
            sharedPuffMaterial.color = new Color(0.96f, 0.96f, 1f, 0.85f);

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
        puffObj.name = "Cartoon_Puff";

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

        if (controller == null) return;
        if (RewindManager.Instance != null && RewindManager.Instance.IsRewinding) return;

        bool isMoving = controller.IsMoving;

        if (isMoving)
        {
            stepTimer += Time.deltaTime;
            if (stepTimer >= stepInterval)
            {
                stepTimer = 0f;
                SpawnRunningPuff();

                // Trigger alternating Left / Right Boop sound (disabled during Dash!)
                if (!controller.IsDashing && SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlayFootstep(isLeftFoot);
                }
            }
        }
        else
        {
            stepTimer = stepInterval;
        }

        if (wasMovingLastFrame && !isMoving)
        {
            SpawnSkidPuffs();
        }

        wasMovingLastFrame = isMoving;
    }

    private void SpawnRunningPuff()
    {
        GameObject puff = GetPuffFromPool();
        if (puff == null) return;

        isLeftFoot = !isLeftFoot;
        float sideOffset = isLeftFoot ? -0.20f : 0.20f;

        Vector3 forwardOffset = -transform.forward * 0.48f;
        Vector3 lateralOffset = transform.right * sideOffset;
        Vector3 spawnPos = new Vector3(
            transform.position.x + forwardOffset.x + lateralOffset.x,
            0.29f,
            transform.position.z + forwardOffset.z + lateralOffset.z
        );

        puff.transform.position = spawnPos;
        puff.transform.localScale = Vector3.zero;
        puff.SetActive(true);

        Vector3 vel = forwardOffset.normalized * 0.4f + Vector3.up * Random.Range(0.4f, 0.7f);
        float targetSize = Random.Range(0.16f, 0.24f);

        activePuffs.Add(new ActivePuff
        {
            gameObject = puff,
            transform = puff.transform,
            initialScale = Vector3.one * targetSize,
            velocity = vel,
            lifetime = 0f,
            maxLifetime = 0.20f
        });
    }

    private void SpawnSkidPuffs()
    {
        for (int i = 0; i < 4; i++)
        {
            GameObject puff = GetPuffFromPool();
            if (puff == null) continue;

            float angle = (i / 4f) * Mathf.PI * 2f;
            Vector3 radialOffset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 0.45f;

            Vector3 spawnPos = new Vector3(
                transform.position.x + radialOffset.x,
                0.29f,
                transform.position.z + radialOffset.z
            );

            puff.transform.position = spawnPos;
            puff.transform.localScale = Vector3.zero;
            puff.SetActive(true);

            Vector3 vel = radialOffset.normalized * 0.9f + Vector3.up * Random.Range(0.5f, 0.9f);
            float targetSize = Random.Range(0.20f, 0.28f);

            activePuffs.Add(new ActivePuff
            {
                gameObject = puff,
                transform = puff.transform,
                initialScale = Vector3.one * targetSize,
                velocity = vel,
                lifetime = 0f,
                maxLifetime = 0.22f
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

            float scaleMultiplier = (progress < 0.3f)
                ? Mathf.Lerp(0.3f, 1.15f, progress / 0.3f)
                : Mathf.Lerp(1.15f, 0f, (progress - 0.3f) / 0.7f);

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
