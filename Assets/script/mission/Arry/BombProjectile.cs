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

    // =========================
    void FixedUpdate()
    {
        if (isExploded) return;

        timer += Time.fixedDeltaTime;
        if (timer > lifeTime)
        {
            Explode();
        }
    }

    // =========================
    // 近接信管（数学判定）
    void LateUpdate()
    {
        if (isExploded) return;
        if (!useProximityFuse) return;

        Vector3 p = transform.position;
        Vector3 v = rb.linearVelocity;

        if (v.sqrMagnitude < 0.01f) return;

        Vector3 vDir = v.normalized;

        foreach (var t in ObjectManager.Instance.Enemies)
        {
            if (t == null) continue;

            Vector3 toTarget = t.transform.position - p;
            float dist = toTarget.magnitude;

            if (dist < physicalRadius)
            {
                // 物理衝突扱いで爆発
                Explode();
                return;
            }

            if (dist > proximityRadius) continue;

            // 進行方向90度以内
            float dot = Vector3.Dot(vDir, toTarget.normalized);
            if (dot > 0f) {
                continue;
            }
            Explode();
            return;
        }
    }

    // =========================
    void Explode()
    {
        if (isExploded) return;
        isExploded = true;

        Vector3 center = transform.position;

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
    }
}
