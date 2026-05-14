using UnityEngine;
using System.Collections;
public static class PrimitiveHelper
{
    static Mesh sphereMesh;

    public static Mesh SphereMesh
    {
        get
        {
            if (sphereMesh != null) return sphereMesh;

            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphereMesh = go.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(go);

            return sphereMesh;
        }
    }
}

[RequireComponent(typeof(Rigidbody))]
public class BombProjectile : MonoBehaviour
{
    // =========================
    // Physics
    [Header("Physics")]
    public Rigidbody rb;
    public float lifeTime = 30f;

    // =========================
    // Damage
    [Header("Damage")]
    public float damageRadius = 50f;
    public float damage = 100f;

    // =========================
    // Proximity Fuse
    [Header("Proximity Fuse")]
    public float physicalRadius = 0.5f;
    public float proximityRadius = 30f;
    public bool useProximityFuse = true;

    // =========================
    // Visuals
    [Header("Visuals")]
    public Material cubeVisual;     // 通常時（Cube）
    public Material sphereVisual;   // 爆発時（Sphere / Transparent）
    public float sphereStartAlpha = 0.25f;
    public float fadeOutTime = 0.4f;

    [Header("Explosion Particle")]
    public ParticleSystem explosionParticle;

    Material Mat;
    MeshRenderer meshRenderer;
    MeshFilter meshFilter;
    // =========================
    // State
    float timer = 0f;
    bool isExploded = false;
    Vector3 previousPos;

    // =========================
    void Awake()
    {
        rb ??= GetComponent<Rigidbody>();

        meshRenderer = GetComponent<MeshRenderer>();
        meshFilter = GetComponent<MeshFilter>();

        if (cubeVisual != null)
            meshRenderer.material = cubeVisual;

        Mat = meshRenderer.material;
    }

    void OnEnable()
    {
        previousPos = transform.position;
    }

    // =========================
    void FixedUpdate()
    {
        if (isExploded) return;

        timer += Time.fixedDeltaTime;
        if (timer > lifeTime)
        {
            Explode();
        }

        if (HitTerrainBetweenPreviousAndCurrent(out Vector3 hitPoint))
        {
            Explode(hitPoint);
            return;
        }

        previousPos = transform.position;
    }

    // =========================
    void Explode()
    {
        Explode(transform.position);
    }

    void Explode(Vector3 center)
    {
        if (isExploded) return;
        isExploded = true;
        transform.position = center;

        // -------- 見た目切替
        if (sphereVisual != null)
        {
            meshRenderer.material = sphereVisual;
            Mat = meshRenderer.material;

            meshFilter.sharedMesh = PrimitiveHelper.SphereMesh;

            transform.localScale = Vector3.one * damageRadius * 2f;
            SetSphereAlpha(sphereStartAlpha);
        }
        
        // -------- パーティクル再生
        if (explosionParticle != null)
        {
            explosionParticle.Clear();
            explosionParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = explosionParticle.main;
            main.loop = false;
            main.duration = 0.3f;
            main.startLifetime = 0.3f;
            main.startSize = 2f/ damageRadius;

            var emission = explosionParticle.emission;
            emission.rateOverTime = 200f;

            // サイズ減衰（時間で小さく）
            var size = explosionParticle.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(
                0.1f,
                AnimationCurve.Linear(0f, 1f, 0.3f, 0f)
            );

            explosionParticle.Play();
        }

        // -------- Rigidbody停止
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;

        // -------- ダメージ（1回のみ）

        foreach (var t in ObjectManager.Instance.Enemies)
        {
            if (t == null) continue;

            float dist = Vector3.Distance(center, t.transform.position);
            if (dist > damageRadius) continue;

            float factor = 1f - (dist / damageRadius);
            float finalDamage = damage * factor;

            if (t.TryGetComponent(out AugumentStatus status))
            {
                status.damage(finalDamage);
                ObjectManager.Instance.hitUIflag = true;
            }
        }

        // -------- フェードアウト開始
        StartCoroutine(FadeOutSphere());
    }
    void OnDrawGizmosSelected()
    {
        // 爆発前後で中心がズレないように
        Vector3 center = transform.position;

        // ダメージ範囲
        Gizmos.color = new Color(1f, 0f, 0f, 0.25f); // 半透明赤
        Gizmos.DrawSphere(center, damageRadius);

        // 近接信管範囲（任意）
        if (useProximityFuse)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.25f); // 半透明黄色
            Gizmos.DrawSphere(center, proximityRadius);
        }
    }
    // =========================
    IEnumerator FadeOutSphere()
    {
        if (Mat == null)
        {
            Destroy(gameObject);
            yield break;
        }

        float t = 0f;
        float startA = Mat.color.a;

        while (t < fadeOutTime)
        {
            float a = Mathf.Lerp(startA, 0f, t / fadeOutTime);
            SetSphereAlpha(a);

            t += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }

    // =========================
    void SetSphereAlpha(float a)
    {
        Color c = Mat.color;
        c.a = a;
        Mat.color = c;
    }

    // =========================
    // 投下時初速
    public void Initialize(Vector3 initialVelocity)
    {
        rb.linearVelocity = initialVelocity;
        previousPos = transform.position;
    }

    bool HitTerrainBetweenPreviousAndCurrent(out Vector3 hitPoint)
    {
        hitPoint = transform.position;
        Vector3 currentPos = transform.position;
        Vector3 delta = currentPos - previousPos;
        float distance = delta.magnitude;
        if (distance <= 0.0001f) return false;

        if (Physics.Raycast(
            previousPos,
            delta / distance,
            out RaycastHit hit,
            distance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore) &&
            hit.collider is TerrainCollider)
        {
            hitPoint = hit.point;
            return true;
        }

        return false;
    }
}
