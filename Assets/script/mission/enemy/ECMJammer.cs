using System.Collections.Generic;
using UnityEngine;

public class ECMJammer : MonoBehaviour
{
    public static readonly List<ECMJammer> ActiveJammers = new();

    public float interferenceRadius = 1800f;
    public bool affectHudLock = true;
    public bool affectRadar = true;

    AugumentStatus status;

    void Awake()
    {
        status = GetComponent<AugumentStatus>();
        if (status == null)
            status = gameObject.AddComponent<AugumentStatus>();

        status.ECM = true;
        status.isEnemy = true;
        status.isPlayer = false;
        status.isVisible = true;
        status.missionObjective = false;
        status.lifeTime = 0f;
        status.issortie = true;
    }

    void OnEnable()
    {
        if (!ActiveJammers.Contains(this))
            ActiveJammers.Add(this);
    }

    void OnDisable()
    {
        ActiveJammers.Remove(this);
    }

    void OnDestroy()
    {
        ActiveJammers.Remove(this);
    }

    public bool Contains(Vector3 worldPosition)
    {
        float radius = Mathf.Max(0f, interferenceRadius);
        return (worldPosition - transform.position).sqrMagnitude <= radius * radius;
    }

    public static bool IsHudJammed(Vector3 worldPosition)
    {
        foreach (var jammer in ActiveJammers)
        {
            if (jammer == null || !jammer.isActiveAndEnabled || !jammer.affectHudLock) continue;
            if (jammer.Contains(worldPosition))
                return true;
        }

        return false;
    }
}
