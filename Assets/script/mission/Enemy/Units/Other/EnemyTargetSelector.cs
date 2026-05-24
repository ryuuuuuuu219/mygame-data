using System.Collections.Generic;
using UnityEngine;

public class EnemyTargetSelector : MonoBehaviour
{
    public float detectRange = 3000f;
    public float lockRange = 850f;
    public float retargetInterval = 0.25f;
    public bool alternateClosestTarget = true;
    public bool IsAirsuppression;
    public float thresholdalt = 1000f;
    public float thresholdaltperdist = 0.25f;

    public bool lockon;
    public GameObject target;
    public Vector3 lastDirToTarget;
    public Vector3 targetVelocity;

    float nextRetargetTime;
    GameObject lastPrimaryTarget;

    public bool HasLockedTarget => lockon && target != null;

    void Update()
    {
        if (Time.time >= nextRetargetTime)
        {
            nextRetargetTime = Time.time + retargetInterval;
            SelectTarget();
        }

        UpdateLock();
    }

    void SelectTarget()
    {
        List<GameObject> targets = ObjectManager.Instance != null
            ? ObjectManager.Instance.allies
            : null;

        if (targets == null || targets.Count == 0)
        {
            target = null;
            return;
        }

        GameObject closest = null;
        GameObject secondClosest = null;
        float closestDistance = Mathf.Infinity;
        float secondDistance = Mathf.Infinity;

        foreach (var candidate in targets)
        {
            if (candidate == null) continue;

            float distance = Vector3.Distance(transform.position, candidate.transform.position);
            if (IsAirsuppression)
            {
                if (!IsAirsuppressionTarget(candidate)) continue;
            }
            else if (distance > detectRange)
            {
                continue;
            }

            if (distance < closestDistance)
            {
                secondDistance = closestDistance;
                secondClosest = closest;
                closestDistance = distance;
                closest = candidate;
            }
            else if (distance < secondDistance)
            {
                secondDistance = distance;
                secondClosest = candidate;
            }
        }

        if (alternateClosestTarget && closest != null && secondClosest != null && lastPrimaryTarget == closest)
            target = secondClosest;
        else
            target = closest;

        lastPrimaryTarget = closest;
    }

    void UpdateLock()
    {
        if (target == null)
        {
            lockon = false;
            return;
        }

        Vector3 toTarget = target.transform.position - transform.position;
        lockon = IsAirsuppression || toTarget.magnitude <= lockRange;

        if (lockon)
        {
            lastDirToTarget = toTarget.normalized;
            targetVelocity = GetTargetVelocity(target);
        }
    }

    Vector3 GetTargetVelocity(GameObject candidate)
    {
        if (candidate == null) return Vector3.zero;

        if (candidate.TryGetComponent(out Rigidbody rb))
            return rb.linearVelocity;

        if (candidate.TryGetComponent(out AugumentStatus status))
            return status.Velocity / Mathf.Max(Time.deltaTime, 0.0001f);

        return Vector3.zero;
    }

    bool IsAirsuppressionTarget(GameObject candidate)
    {
        Vector3 toTarget = candidate.transform.position - transform.position;
        if (candidate.transform.position.y >= thresholdalt) return true;

        Vector2 horizontal = new Vector2(toTarget.x, toTarget.z);
        float coneAltitude = horizontal.magnitude * thresholdaltperdist;
        return toTarget.y >= coneAltitude;
    }
}
