using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyRailgunShooter : MonoBehaviour
{
    public Transform muzzle;
    public float range = 3200f;
    public float fireCooldown = 2.5f;
    public float randomDeflectionAngle = 5f;
    public float projectileSpeed = 300f;
    public float beamWidth = 10f;
    public float beamColliderLifetime = 0.08f;
    public float beamDamage = 120f;

    [Header("Shockwave")]
    public float shockwaveInterval = 200f;
    public float shockwaveWidth = 120f;
    public float shockwaveDepth = 8f;
    public float shockwaveLifetime = 3.5f;
    public float shockwaveDamage = 35f;

    public EnemyTargetSelector targetSelector;

    float nextFireTime;

    void Awake()
    {
        muzzle ??= transform;
        targetSelector ??= GetComponent<EnemyTargetSelector>();

        if (targetSelector != null)
        {
            targetSelector.detectRange = Mathf.Max(targetSelector.detectRange, range);
            targetSelector.lockRange = Mathf.Max(targetSelector.lockRange, range);
        }
    }

    void Update()
    {
        if (Time.time < nextFireTime) return;
        if (targetSelector == null || !targetSelector.HasLockedTarget) return;

        FireAt(targetSelector.target.transform);
        nextFireTime = Time.time + fireCooldown;
    }

    void FireAt(Transform target)
    {
        if (target == null) return;

        Vector3 origin = muzzle.position;
        Vector3 directAim = target.position - origin;
        if (directAim.sqrMagnitude <= 0.001f) return;

        Vector3 direction = RandomizeDirection(directAim.normalized, randomDeflectionAngle);
        Vector3 impactPoint = GetImpactPoint(origin, direction);
        float distance = Vector3.Distance(origin, impactPoint);
        if (distance <= 0.001f) return;

        StartCoroutine(FlyProjectile(origin, direction, distance));
        GeneratedAudioManager.Play(GeneratedAudioCue.EnemyGunFire, origin, 0.85f);
    }

    IEnumerator FlyProjectile(Vector3 origin, Vector3 direction, float distance)
    {
        Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
        float speed = Mathf.Max(0.001f, projectileSpeed);
        float traveled = 0f;
        float nextShockwaveDistance = Mathf.Max(1f, shockwaveInterval);

        while (traveled < distance)
        {
            float previous = traveled;
            traveled = Mathf.Min(distance, traveled + speed * Time.deltaTime);
            float segmentDistance = traveled - previous;

            if (segmentDistance > 0.001f)
            {
                RailgunDamageVolume.Spawn(
                    "RailgunBeamCollider",
                    origin + direction * (previous + segmentDistance * 0.5f),
                    rotation,
                    new Vector3(beamWidth, beamWidth, segmentDistance),
                    beamDamage,
                    beamColliderLifetime
                );
            }

            while (nextShockwaveDistance <= traveled && nextShockwaveDistance < distance)
            {
                RailgunShockwave.Spawn(
                    origin + direction * nextShockwaveDistance,
                    rotation,
                    shockwaveWidth,
                    shockwaveDepth,
                    shockwaveDamage,
                    shockwaveLifetime
                );
                nextShockwaveDistance += Mathf.Max(1f, shockwaveInterval);
            }

            yield return null;
        }
    }

    Vector3 RandomizeDirection(Vector3 direction, float angle)
    {
        if (angle <= 0f) return direction;

        Vector2 offset = Random.insideUnitCircle.normalized * angle;
        if (offset.sqrMagnitude <= 0.001f)
            offset = Vector2.right * angle;

        Vector3 right = Vector3.Cross(Vector3.up, direction);
        if (right.sqrMagnitude <= 0.001f)
            right = Vector3.Cross(Vector3.forward, direction);

        right.Normalize();
        Vector3 up = Vector3.Cross(direction, right).normalized;

        Quaternion yaw = Quaternion.AngleAxis(offset.x, up);
        Quaternion pitch = Quaternion.AngleAxis(offset.y, right);
        return (yaw * pitch * direction).normalized;
    }

    Vector3 GetImpactPoint(Vector3 origin, Vector3 direction)
    {
        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            direction,
            range,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore
        );

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null) continue;
            if (hit.transform.IsChildOf(transform)) continue;

            if (ObjectGroundBounds.IsGroundCollider(hit.collider))
                return hit.point;
        }

        return origin + direction * range;
    }

}

public class RailgunDamageVolume : MonoBehaviour
{
    readonly HashSet<AugumentStatus> damaged = new();

    float damage;
    float lifetime;
    BoxCollider boxCollider;

    public static RailgunDamageVolume Spawn(
        string objectName,
        Vector3 position,
        Quaternion rotation,
        Vector3 size,
        float damage,
        float lifetime,
        bool visible = false)
    {
        GameObject obj = visible
            ? GameObject.CreatePrimitive(PrimitiveType.Cube)
            : new GameObject(objectName);
        obj.name = objectName;
        obj.transform.SetPositionAndRotation(position, rotation);

        var volume = obj.AddComponent<RailgunDamageVolume>();
        volume.Initialize(size, damage, lifetime);
        return volume;
    }

    public void Initialize(Vector3 size, float damage, float lifetime)
    {
        this.damage = damage;
        this.lifetime = lifetime;

        boxCollider = GetComponent<BoxCollider>();
        if (boxCollider == null)
            boxCollider = gameObject.AddComponent<BoxCollider>();

        boxCollider.isTrigger = true;
        if (TryGetComponent(out MeshRenderer _))
        {
            transform.localScale = size;
            boxCollider.size = Vector3.one;
        }
        else
        {
            boxCollider.size = size;
        }

        ApplyOverlapDamage();
    }

    void Update()
    {
        lifetime -= Time.deltaTime;
        ApplyOverlapDamage();

        if (lifetime <= 0f)
            Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        TryDamage(other);
    }

    void OnTriggerStay(Collider other)
    {
        TryDamage(other);
    }

    void ApplyOverlapDamage()
    {
        if (boxCollider == null) return;

        Collider[] hits = Physics.OverlapBox(
            transform.TransformPoint(boxCollider.center),
            Vector3.Scale(boxCollider.size, transform.lossyScale) * 0.5f,
            transform.rotation,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Collide
        );

        foreach (Collider hit in hits)
            TryDamage(hit);
    }

    void TryDamage(Collider other)
    {
        var status = other.GetComponentInParent<AugumentStatus>();
        if (status == null || !status.isPlayer || status.isEnemy) return;
        if (!damaged.Add(status)) return;

        status.damage(damage);
    }
}

public class RailgunShockwave : MonoBehaviour
{
    readonly RailgunDamageVolume[] volumes = new RailgunDamageVolume[4];
    float width;
    float depth;
    float lifetime;
    float age;

    public static RailgunShockwave Spawn(
        Vector3 position,
        Quaternion rotation,
        float width,
        float depth,
        float damage,
        float lifetime)
    {
        var root = new GameObject("RailgunShockwave");
        root.transform.SetPositionAndRotation(position, rotation);

        var shockwave = root.AddComponent<RailgunShockwave>();
        shockwave.Initialize(width, depth, damage, lifetime);
        return shockwave;
    }

    void Initialize(float width, float depth, float damage, float lifetime)
    {
        this.width = Mathf.Max(0.001f, width);
        this.depth = Mathf.Max(0.001f, depth);
        this.lifetime = Mathf.Max(0.001f, lifetime);

        for (int i = 0; i < volumes.Length; i++)
        {
            volumes[i] = RailgunDamageVolume.Spawn(
                "RailgunShockwaveCollider",
                transform.position,
                transform.rotation,
                Vector3.one,
                damage,
                this.lifetime,
                true
            );
            volumes[i].transform.SetParent(transform, true);
        }

        UpdatePieces(0f);
    }

    public float t;
    void Update()
    {
        age += Time.deltaTime;
        t = Mathf.Clamp01(age / lifetime);
        UpdatePieces(t);

        if (age >= lifetime)
            Destroy(gameObject);
    }

    void UpdatePieces(float t)
    {
        float pieceWidth = Mathf.Lerp(width, width * 2f, t);
        float pieceHeight = Mathf.Lerp(width, 0f, t);
        float thickness = Mathf.Max(0.01f, pieceHeight);
        float offset = Mathf.Max(0f, (pieceWidth - thickness) * 0.5f);

        Vector2 basePosition = new Vector2(0f, offset);

        for (int i = 0; i < volumes.Length; i++)
        {
            Transform piece = volumes[i].transform;

            Vector2 position = i switch
            {
                0 => basePosition,
                1 => new Vector2(-basePosition.y, basePosition.x),
                2 => -basePosition,
                _ => new Vector2(basePosition.y, -basePosition.x),
            };

            piece.localPosition = new Vector3(position.x, position.y, 0f);
            piece.localRotation = Quaternion.identity;
            piece.localScale = i % 2 == 0
                ? new Vector3(pieceWidth, thickness, depth)
                : new Vector3(thickness, pieceWidth, depth);

            var collider = piece.GetComponent<BoxCollider>();
            if (collider != null)
                collider.size = Vector3.one;
        }
    }
}

