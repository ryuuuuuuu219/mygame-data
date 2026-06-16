using System.Collections.Generic;
using UnityEngine;

public static class EnemyMissileThreatSensor
{
    public struct ThreatInfo
    {
        public float dist;
        public GameObject entity;
        public Vector3 evadeDirection;
        public Vector3 approachDirection;
        public float closingSpeed;
        public float timeToImpact;
        public float score;
        public bool targeted;
    }

    public static int SenseIncomingMissiles(
        Transform self,
        Rigidbody selfRb,
        List<ThreatInfo> results,
        float detectRange,
        float approachAngle,
        float criticalTime)
    {
        results.Clear();

        ObjectManager objectManager = ObjectManager.Instance;
        if (self == null || objectManager == null || objectManager.missiles_a == null)
            return 0;

        Vector3 selfVelocity = selfRb != null ? selfRb.linearVelocity : Vector3.zero;
        float approachDotThreshold = Mathf.Cos(Mathf.Clamp(approachAngle, 0f, 180f) * Mathf.Deg2Rad);

        foreach (GameObject missileObject in objectManager.missiles_a)
        {
            if (missileObject == null) continue;

            Vector3 missileToMe = self.position - missileObject.transform.position;
            float distance = missileToMe.magnitude;
            if (distance <= 0.001f || distance > detectRange) continue;

            Rigidbody missileRb = missileObject.GetComponent<Rigidbody>();
            Vector3 missileVelocity = missileRb != null ? missileRb.linearVelocity : missileObject.transform.forward;
            Vector3 missileDirection = SafeNormalize(missileVelocity, missileObject.transform.forward);
            Vector3 toMeDirection = missileToMe / distance;

            float approachDot = Vector3.Dot(missileDirection, toMeDirection);
            if (approachDot < approachDotThreshold) continue;

            float closingSpeed = Vector3.Dot(missileVelocity - selfVelocity, toMeDirection);
            if (closingSpeed <= 0.1f) continue;

            Missile missile = missileObject.GetComponent<Missile>();
            bool targeted = missile != null && missile.target == self;
            float timeToImpact = distance / closingSpeed;
            float timeScore = Mathf.Clamp(criticalTime - timeToImpact, 0f, criticalTime) * 500f;
            float distanceScore = Mathf.Clamp(detectRange - distance, 0f, detectRange);
            float dotScore = Mathf.Clamp01(approachDot) * 600f;

            Vector3 lateral = Vector3.Cross(missileDirection, Vector3.up);
            if (lateral.sqrMagnitude < 0.001f)
                lateral = Vector3.Cross(missileDirection, self.up);

            lateral = SafeNormalize(lateral, self.right);
            if (Vector3.Dot(lateral, self.right) < 0f)
                lateral = -lateral;

            Vector3 away = toMeDirection * 0.45f;
            Vector3 climbBias = self.position.y < 1200f ? Vector3.up * 0.35f : Vector3.zero;

            results.Add(new ThreatInfo
            {
                dist = distance,
                entity = missileObject,
                evadeDirection = SafeNormalize(lateral + away + climbBias, self.right),
                approachDirection = missileDirection,
                closingSpeed = closingSpeed,
                timeToImpact = timeToImpact,
                score = distanceScore + timeScore + dotScore + (targeted ? 900f : 0f),
                targeted = targeted
            });
        }

        results.Sort((a, b) => a.dist.CompareTo(b.dist));
        return results.Count;
    }

    static Vector3 SafeNormalize(Vector3 value, Vector3 fallback)
    {
        if (value.sqrMagnitude > 0.0001f && IsFinite(value))
            return value.normalized;

        if (fallback.sqrMagnitude > 0.0001f && IsFinite(fallback))
            return fallback.normalized;

        return Vector3.forward;
    }

    static bool IsFinite(Vector3 value)
    {
        return !float.IsNaN(value.x)
            && !float.IsNaN(value.y)
            && !float.IsNaN(value.z)
            && !float.IsInfinity(value.x)
            && !float.IsInfinity(value.y)
            && !float.IsInfinity(value.z);
    }
}
