using UnityEngine;

public class EnemyMissileShooter_longrange : MonoBehaviour
{
    public Transform missileHardpoint;
    public GameObject vlsFirstStagePrefab;
    public float missileSpeed = 100f;
    public float missileCooldown = 3f;
    public float missileLifeTime = 10f;

    public float missilePower = 50f;
    public float missileAcceleration = 20f;
    public float missileMaxSpeed = 150f;
    public float missileTurnRate = 8f;
    public float missileBreakAngle = 90f;
    public float missileProportionalConstant = 3f;
    public bool requireLineOfSight = true;
    public LayerMask lineOfSightMask = ~0;
    public float lineOfSightOriginOffset = 2f;
    public float minimumLaunchUpDot = 0.15f;
    public float vlsRiseDistance = 120f;
    public float vlsRiseSpeed = 80f;
    public float vlsSecondStageBreakAngle = 140f;
    public float vlsSecondStageTurnRateMultiplier = 1.5f;
    public bool createFallbackFirstStageVisual = true;
    public Vector3 fallbackFirstStageScale = new(1.5f, 6f, 1.5f);

    [SerializeField] Gun_e_pool bulletpool;

    float nextMissileTime;

    public bool TryFire(Vector3 direction, Vector3 platformVelocity, Transform target, bool ignoreLineOfSight = false)
    {
        if (Time.time < nextMissileTime) return false;
        if (direction.sqrMagnitude <= 0.001f) return false;
        if (!ignoreLineOfSight && requireLineOfSight && !HasLineOfSight(target)) return false;

        if (!Fire(direction.normalized, platformVelocity, target)) return false;
        nextMissileTime = Time.time + missileCooldown;
        return true;
    }

    bool Fire(Vector3 direction, Vector3 platformVelocity, Transform target)
    {
        if (missileHardpoint == null) missileHardpoint = transform;
        if (bulletpool == null)
            bulletpool = FindFirstObjectByType<Gun_e_pool>();
        if (bulletpool == null) return false;

        GameObject firstStageObject = CreateFirstStageObject();

        VlsFirstStageMissileUnit firstStage = firstStageObject.GetComponent<VlsFirstStageMissileUnit>();
        if (firstStage == null)
            firstStage = firstStageObject.AddComponent<VlsFirstStageMissileUnit>();

        firstStage.Initialize(
            this,
            target,
            platformVelocity,
            missileHardpoint.position,
            vlsRiseDistance,
            vlsRiseSpeed
        );
        GeneratedAudioManager.Play(GeneratedAudioCue.EnemyMissileLaunch, missileHardpoint.position, 0.85f);
        return true;
    }

    GameObject CreateFirstStageObject()
    {
        if (vlsFirstStagePrefab != null)
            return Instantiate(vlsFirstStagePrefab, missileHardpoint.position, Quaternion.LookRotation(Vector3.up));

        if (!createFallbackFirstStageVisual)
        {
            var empty = new GameObject("VLS_FirstStageMissileUnit");
            empty.transform.position = missileHardpoint.position;
            empty.transform.rotation = Quaternion.LookRotation(Vector3.up);
            return empty;
        }

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visual.name = "VLS_FirstStageMissileUnit";
        visual.transform.position = missileHardpoint.position;
        visual.transform.rotation = Quaternion.LookRotation(Vector3.up);
        visual.transform.localScale = fallbackFirstStageScale;

        if (visual.TryGetComponent(out Collider collider))
            Destroy(collider);

        return visual;
    }

    public bool LaunchSecondStage(Transform target, Vector3 platformVelocity, Vector3 launchPosition, out Missile launchedMissile)
    {
        launchedMissile = null;
        if (!IsAlive(target)) return false;
        if (bulletpool == null)
            bulletpool = FindFirstObjectByType<Gun_e_pool>();
        if (bulletpool == null) return false;

        Vector3 direction = CalculateSecondStageDirection(target, launchPosition);
        Vector3 velocity = platformVelocity + direction * missileSpeed;
        GameObject missileObject = bulletpool.missilepull(launchPosition, velocity, missileLifeTime);
        Missile missile = missileObject.GetComponent<Missile>();

        if (missile == null) return false;

        missile.missileInit(launchPosition, velocity, missileLifeTime);
        missile.StatusSetting(
            missilePower,
            missileAcceleration,
            missileMaxSpeed,
            missileTurnRate * vlsSecondStageTurnRateMultiplier,
            Mathf.Max(missileBreakAngle, vlsSecondStageBreakAngle),
            missileProportionalConstant
        );
        missile.isheatseeker = false;
        missile.target = target;
        launchedMissile = missile;
        GeneratedAudioManager.Play(GeneratedAudioCue.EnemyMissileLaunch, launchPosition, 0.7f);
        return true;
    }

    Vector3 CalculateSecondStageDirection(Transform target, Vector3 launchPosition)
    {
        Vector3 direction = target.position - launchPosition;
        if (direction.sqrMagnitude <= 0.001f)
            return Vector3.up;

        return ClampLaunchDirection(direction.normalized);
    }

    Vector3 ClampLaunchDirection(Vector3 direction)
    {
        if (direction.y >= minimumLaunchUpDot) return direction;

        direction.y = minimumLaunchUpDot;
        return direction.normalized;
    }

    bool HasLineOfSight(Transform target)
    {
        if (target == null) return false;
        if (missileHardpoint == null) missileHardpoint = transform;

        Vector3 origin = missileHardpoint.position + Vector3.up * lineOfSightOriginOffset;
        Vector3 toTarget = target.position - origin;
        float distance = toTarget.magnitude;
        if (distance <= 0.001f) return false;

        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            toTarget / distance,
            distance,
            lineOfSightMask,
            QueryTriggerInteraction.Ignore
        );

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var hit in hits)
        {
            if (hit.collider == null) continue;
            if (hit.transform.IsChildOf(transform)) continue;
            if (hit.transform == target || hit.transform.IsChildOf(target))
                return true;

            return false;
        }

        return true;
    }

    static bool IsAlive(Object obj)
    {
        return obj != null;
    }
}

public class VlsFirstStageMissileUnit : MonoBehaviour
{
    EnemyMissileShooter_longrange launcher;
    Transform target;
    Vector3 platformVelocity;
    Vector3 startPosition;
    float riseDistance;
    float riseSpeed;

    public void Initialize(
        EnemyMissileShooter_longrange owner,
        Transform targetTransform,
        Vector3 inheritedVelocity,
        Vector3 origin,
        float distance,
        float speed)
    {
        launcher = owner;
        target = targetTransform;
        platformVelocity = inheritedVelocity;
        startPosition = origin;
        riseDistance = Mathf.Max(0f, distance);
        riseSpeed = Mathf.Max(1f, speed);

        transform.position = origin;
        transform.rotation = Quaternion.LookRotation(Vector3.up);
    }

    void Update()
    {
        transform.position += Vector3.up * riseSpeed * Time.deltaTime;

        if (Vector3.Distance(startPosition, transform.position) < riseDistance)
            return;

        if (launcher != null && target != null &&
            launcher.LaunchSecondStage(target, platformVelocity, transform.position, out Missile missile) &&
            missile != null)
        {
            transform.SetParent(missile.transform, true);
            enabled = false;
            Destroy(gameObject, GetTrailRetentionTime());
            return;
        }

        Destroy(gameObject);
    }

    float GetTrailRetentionTime()
    {
        float retentionTime = 0f;

        foreach (var trail in GetComponentsInChildren<TrailRenderer>())
        {
            if (trail == null) continue;
            trail.emitting = false;
            retentionTime = Mathf.Max(retentionTime, trail.time);
        }

        foreach (var particle in GetComponentsInChildren<ParticleSystem>())
        {
            if (particle == null) continue;
            particle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            var main = particle.main;
            retentionTime = Mathf.Max(retentionTime, main.duration + main.startLifetime.constantMax);
        }

        return Mathf.Max(0.1f, retentionTime);
    }
}
