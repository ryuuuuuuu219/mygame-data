using System.Collections.Generic;
using UnityEngine;

public class Gun_p : MonoBehaviour
{
    [Header("弾の寿命設定")]
    public float lifeTime = 3f;         // 弾の生存時間（秒）

    [Header("内部パラメータ")]
    public Vector3 velocity;            // 弾の移動速度
    private Vector3 previousPos;
    private Vector3 currentPos;
    private bool groundImpactProbed;
    private bool hasPlannedGroundHit;
    private Vector3 plannedGroundHitPoint;

    public float power = 10f;
    public float size = 1f;
    public float effectRadius = 0f;

    private List<GameObject> enemys;

    private void OnEnable()
    {
        previousPos = transform.position;
        currentPos = transform.position;
        groundImpactProbed = false;
        hasPlannedGroundHit = false;
    }

    public void Init(float Power, float Size)
    {
        this.power = Power;
        this.size = Size;

        transform.GetComponent<BoxCollider>().size = new Vector3(size, size, size);
    }

    private void FixedUpdate()
    {
        lifeTime -= Time.deltaTime;
        if (lifeTime <= 0f)
        {
            gameObject.SetActive(false);
            return;
        }

        previousPos = transform.position;
        transform.position += velocity * Time.fixedDeltaTime;
        currentPos = transform.position;

        if (ResolveColliderHitBetweenPreviousAndCurrent())
        {
            gameObject.SetActive(false);
            return;
        }

        if (ResolvePlannedGroundImpact())
        {
            gameObject.SetActive(false);
            return;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (ObjectGroundBounds.IsGroundCollider(other))
        {
            ImpactEffectFactory.Spawn(transform.position, effectRadius);
            gameObject.SetActive(false);
            return;
        }

        if (DamageTargetResolver.TryGetEnemyStatus(other, out AugumentStatus status))
        {
            status.damage(power);
            ObjectManager.Instance.hitUIflag = true;
            ImpactEffectFactory.Spawn(transform.position, effectRadius);
            GeneratedAudioManager.Play(GeneratedAudioCue.Hit, transform.position, 0.45f);
            gameObject.SetActive(false);
        }
    }

    private bool ResolveColliderHitBetweenPreviousAndCurrent()
    {
        Vector3 delta = currentPos - previousPos;
        float distance = delta.magnitude;
        if (distance <= 0.0001f) return false;

        Vector3 dir = delta / distance;
        float radius = GetProjectileRadius();
        RaycastHit[] hits = Physics.SphereCastAll(
            previousPos,
            radius,
            dir,
            distance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Collide);

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null) continue;
            if (hit.collider.transform.IsChildOf(transform)) continue;

            if (ObjectGroundBounds.IsGroundCollider(hit.collider))
            {
                ImpactEffectFactory.Spawn(hit.point, effectRadius);
                return true;
            }

            if (!DamageTargetResolver.TryGetEnemyStatus(hit.collider, out AugumentStatus status)) continue;

            status.damage(power);
            if (ObjectManager.Instance != null)
                ObjectManager.Instance.hitUIflag = true;
            ImpactEffectFactory.Spawn(hit.point, effectRadius);
            GeneratedAudioManager.Play(GeneratedAudioCue.Hit, hit.point, 0.45f);
            return true;
        }

        return false;
    }

    private bool ResolvePlannedGroundImpact()
    {
        ProbeGroundImpactOnce();

        if (!hasPlannedGroundHit)
            return false;

        Vector3 fromPrevious = plannedGroundHitPoint - previousPos;
        Vector3 fromCurrent = plannedGroundHitPoint - currentPos;
        if (Vector3.Dot(fromPrevious, fromCurrent) > 0f)
            return false;

        ImpactEffectFactory.Spawn(plannedGroundHitPoint, effectRadius);
        transform.position = plannedGroundHitPoint;
        hasPlannedGroundHit = false;
        return true;
    }

    private void ProbeGroundImpactOnce()
    {
        if (groundImpactProbed)
            return;

        if (velocity.sqrMagnitude <= 0.000001f)
            return;

        groundImpactProbed = true;
        hasPlannedGroundHit = false;

        if (velocity.y > 0f)
            return;

        float probeDistance = velocity.magnitude * Mathf.Max(lifeTime, Time.fixedDeltaTime);
        if (Physics.Raycast(
            transform.position,
            velocity.normalized,
            out RaycastHit hit,
            probeDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore) &&
            ObjectGroundBounds.IsGroundCollider(hit.collider))
        {
            plannedGroundHitPoint = hit.point;
            hasPlannedGroundHit = true;
        }
    }

    private float GetProjectileRadius()
    {
        Collider collider = GetComponent<Collider>();
        if (collider == null)
            return Mathf.Max(0.01f, transform.lossyScale.magnitude * 0.5f);

        Vector3 extents = collider.bounds.extents;
        return Mathf.Max(0.01f, Mathf.Max(extents.x, extents.y, extents.z));
    }

}

