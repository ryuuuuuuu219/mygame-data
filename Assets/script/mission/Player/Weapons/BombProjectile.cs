using UnityEngine;
using System.Collections;
using System.Collections.Generic;
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

    // =========================
    // Damage
    [Header("Damage")]
    public float damageRadius = 50f;
    public float damage = 100f;

    // =========================
    // Visuals
    [Header("Visuals")]
    public Material cubeVisual;     // 通常時（Cube）
    public Material sphereVisual;   // 爆発時（Sphere / Transparent）
    public float sphereStartAlpha = 0.25f;
    public float fadeOutTime = 0.4f;

    [Header("Ground Radius Preview")]
    public LineRenderer groundRadiusLine;
    public Material groundRadiusLineMaterial;
    public Color groundRadiusLineColor = new Color(1f, 0.15f, 0.05f, 0.85f);
    public float groundRadiusLineWidth = 2f;
    public int groundRadiusLineSegments = 96;
    public float groundRadiusLineHeightOffset = 1f;
    public float groundPreviewRayDistance = 5000f;

    [Header("Explosion Particle")]
    public ParticleSystem explosionParticle;

    Material Mat;
    MeshRenderer meshRenderer;
    MeshFilter meshFilter;
    Transform groundRadiusLineTransform;
    // =========================
    // State
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

        EnsureGroundRadiusLine();
    }

    void OnEnable()
    {
        previousPos = transform.position;
        UpdateGroundRadiusLine();
    }

    // =========================
    void FixedUpdate()
    {
        if (isExploded) return;

        if (HitTerrainBetweenPreviousAndCurrent(out Vector3 hitPoint))
        {
            Explode(hitPoint);
            return;
        }

        if (ObjectGroundBounds.IsBelowWorldOrTerrain(transform.position))
        {
            ExplodeAtCurrentGroundOrPosition();
            return;
        }

        UpdateGroundRadiusLine();
        previousPos = transform.position;
    }

    // =========================
    void Explode()
    {
        ExplodeAtCurrentGroundOrPosition();
    }

    void Explode(Vector3 center)
    {
        if (isExploded) return;
        isExploded = true;
        transform.position = center;
        SetGroundRadiusLineVisible(false);
        GeneratedAudioManager.Play(GeneratedAudioCue.BombExplosion, center, 0.9f);
        ImpactEffectFactory.Spawn(center, damageRadius);

        // -------- Rigidbody停止
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;

        ApplyExplosionDamage(center);
        Destroy(gameObject);
    }
    void OnDrawGizmosSelected()
    {
        // 爆発前後で中心がズレないように
        Vector3 center = transform.position;

        // ダメージ範囲
        Gizmos.color = new Color(1f, 0f, 0f, 0.25f); // 半透明赤
        Gizmos.DrawSphere(center, damageRadius);

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
            ObjectGroundBounds.IsGroundCollider(hit.collider))
        {
            hitPoint = hit.point;
            return true;
        }

        return false;
    }

    void ExplodeAtCurrentGroundOrPosition()
    {
        if (TryGetGroundPose(out Vector3 groundPoint, out _))
            Explode(groundPoint);
        else
            Explode(transform.position);
    }

    void ApplyExplosionDamage(Vector3 center)
    {
        if (ObjectManager.Instance == null) return;

        var damaged = new HashSet<AugumentStatus>();
        Collider[] hits = Physics.OverlapSphere(
            center,
            damageRadius,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Collide);

        foreach (Collider hit in hits)
        {
            if (hit == null) continue;

            if (!DamageTargetResolver.TryGetEnemyStatus(hit, out AugumentStatus status)) continue;
            if (!damaged.Add(status)) continue;

            Vector3 closestPoint = hit.ClosestPoint(center);
            float dist = Vector3.Distance(center, closestPoint);
            if (dist > damageRadius) continue;

            float factor = 1f - Mathf.Clamp01(dist / damageRadius);
            float finalDamage = damage * factor;
            if (finalDamage <= 0f) continue;

            status.damage(finalDamage);
            ObjectManager.Instance.hitUIflag = true;
        }
    }

    void EnsureGroundRadiusLine()
    {
        if (groundRadiusLine != null)
        {
            groundRadiusLineTransform = groundRadiusLine.transform;
            ConfigureGroundRadiusLine();
            return;
        }

        var lineObject = new GameObject("UGB_GroundRadiusLine");
        lineObject.transform.SetParent(null);
        groundRadiusLineTransform = lineObject.transform;
        groundRadiusLine = lineObject.AddComponent<LineRenderer>();
        ConfigureGroundRadiusLine();
    }

    void ConfigureGroundRadiusLine()
    {
        if (groundRadiusLine == null) return;

        groundRadiusLine.loop = true;
        groundRadiusLine.useWorldSpace = false;
        groundRadiusLine.positionCount = Mathf.Max(8, groundRadiusLineSegments);
        groundRadiusLine.startWidth = groundRadiusLineWidth;
        groundRadiusLine.endWidth = groundRadiusLineWidth;
        groundRadiusLine.startColor = groundRadiusLineColor;
        groundRadiusLine.endColor = groundRadiusLineColor;
        groundRadiusLine.material = groundRadiusLineMaterial != null
            ? groundRadiusLineMaterial
            : new Material(Shader.Find("Sprites/Default"));
    }

    void UpdateGroundRadiusLine()
    {
        EnsureGroundRadiusLine();
        if (groundRadiusLine == null || isExploded)
        {
            SetGroundRadiusLineVisible(false);
            return;
        }

        if (!TryGetGroundPose(out Vector3 groundPoint, out Vector3 groundNormal))
        {
            SetGroundRadiusLineVisible(false);
            return;
        }

        SetGroundRadiusLineVisible(true);
        groundRadiusLineTransform.position = groundPoint + groundNormal * groundRadiusLineHeightOffset;
        groundRadiusLineTransform.rotation = Quaternion.FromToRotation(Vector3.up, groundNormal);

        int segments = Mathf.Max(8, groundRadiusLineSegments);
        if (groundRadiusLine.positionCount != segments)
            groundRadiusLine.positionCount = segments;

        float radius = Mathf.Max(0f, damageRadius);
        for (int i = 0; i < segments; i++)
        {
            float angle = (Mathf.PI * 2f * i) / segments;
            groundRadiusLine.SetPosition(i, new Vector3(
                Mathf.Cos(angle) * radius,
                0f,
                Mathf.Sin(angle) * radius));
        }
    }

    bool TryGetGroundPose(out Vector3 point, out Vector3 normal)
    {
        Vector3 rayStart = transform.position + Vector3.up * 10f;
        if (Physics.Raycast(
            rayStart,
            Vector3.down,
            out RaycastHit hit,
            groundPreviewRayDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore) &&
            ObjectGroundBounds.IsGroundCollider(hit.collider))
        {
            point = hit.point;
            normal = hit.normal;
            return true;
        }

        point = transform.position;
        normal = Vector3.up;
        return false;
    }

    void SetGroundRadiusLineVisible(bool visible)
    {
        if (groundRadiusLine != null)
            groundRadiusLine.enabled = visible;
    }

    void OnDestroy()
    {
        if (groundRadiusLineTransform != null)
            Destroy(groundRadiusLineTransform.gameObject);
    }
}

