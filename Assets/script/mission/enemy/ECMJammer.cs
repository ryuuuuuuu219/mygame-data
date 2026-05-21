using System.Collections.Generic;
using UnityEngine;

public class ECMJammer : MonoBehaviour
{
    public static readonly List<ECMJammer> ActiveJammers = new();

    public float interferenceRadius = 1800f;
    public bool affectHudLock = true;
    public bool affectRadar = true;

    AugumentStatus status;
    SphereCollider interferenceCollider;

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

        SetupInterferenceCollider();
    }

    void OnEnable()
    {
        if (!ActiveJammers.Contains(this))
            ActiveJammers.Add(this);

        SetupInterferenceCollider();
    }

    void OnDisable()
    {
        ActiveJammers.Remove(this);
    }

    void OnDestroy()
    {
        ActiveJammers.Remove(this);
    }

    void OnValidate()
    {
        if (interferenceCollider != null)
            interferenceCollider.radius = Mathf.Max(0f, interferenceRadius);
    }

    void SetupInterferenceCollider()
    {
        if (interferenceCollider == null)
            interferenceCollider = GetComponent<SphereCollider>();

        if (interferenceCollider == null)
            interferenceCollider = gameObject.AddComponent<SphereCollider>();

        interferenceCollider.isTrigger = true;
        interferenceCollider.radius = Mathf.Max(0f, interferenceRadius);
    }

    void OnTriggerEnter(Collider other)
    {
        SetEnemyEcm(other, true);
    }

    void OnTriggerStay(Collider other)
    {
        SetEnemyEcm(other, true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!TryGetEnemyStatus(other, out AugumentStatus targetStatus))
            return;

        targetStatus.ECM = IsCoveredByOtherJammer(targetStatus.transform.position);
    }

    static void SetEnemyEcm(Collider other, bool value)
    {
        if (!TryGetEnemyStatus(other, out AugumentStatus targetStatus))
            return;

        targetStatus.ECM = value;
    }

    static bool TryGetEnemyStatus(Collider other, out AugumentStatus targetStatus)
    {
        targetStatus = null;
        if (other == null || !other.CompareTag("enemy"))
            return false;

        targetStatus = other.GetComponentInParent<AugumentStatus>();
        return targetStatus != null;
    }

    bool IsCoveredByOtherJammer(Vector3 worldPosition)
    {
        foreach (var jammer in ActiveJammers)
        {
            if (jammer == null || jammer == this || !jammer.isActiveAndEnabled) continue;
            if (jammer.Contains(worldPosition))
                return true;
        }

        return false;
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
