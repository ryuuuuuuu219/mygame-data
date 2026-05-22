using System.Collections.Generic;
using UnityEngine;

public class ECMJammer : MonoBehaviour
{
    public static readonly List<ECMJammer> ActiveJammers = new();

    public float interferenceRadius = 1800f;
    public bool affectHudLock = true;
    public bool affectRadar = true;

    AugumentStatus status;
    InterferenceCollider interferenceCollider;

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

        ECMManager.RegisterJammer(gameObject);
        SetupInterferenceCollider();
    }

    void OnDisable()
    {
        ActiveJammers.Remove(this);
        ECMManager.UnregisterJammer(gameObject);
    }

    void OnDestroy()
    {
        ActiveJammers.Remove(this);
        ECMManager.UnregisterJammer(gameObject);
    }

    void OnValidate()
    {
        if (interferenceCollider != null)
            interferenceCollider.SetRadius(interferenceRadius);
    }

    void SetupInterferenceCollider()
    {
        if (interferenceCollider == null)
            interferenceCollider = GetComponentInChildren<InterferenceCollider>(true);

        if (interferenceCollider == null)
        {
            var colliderObject = new GameObject("InterferenceCollider");
            colliderObject.transform.SetParent(transform, false);
            interferenceCollider = colliderObject.AddComponent<InterferenceCollider>();
        }

        interferenceCollider.Initialize(this, interferenceRadius);
    }

    public void SetTargetInterference(Collider other, bool value)
    {
        if (!TryGetEnemyStatus(other, out AugumentStatus targetStatus))
            return;

        ECMManager.SetEffect(gameObject, targetStatus.gameObject, value);
    }

    public void RefreshTargetInterference(Collider other)
    {
        if (!TryGetEnemyStatus(other, out AugumentStatus targetStatus))
            return;

        ECMManager.SetEffect(gameObject, targetStatus.gameObject, false);
    }

    static bool TryGetEnemyStatus(Collider other, out AugumentStatus targetStatus)
    {
        targetStatus = null;
        if (other == null)
            return false;

        targetStatus = other.GetComponentInParent<AugumentStatus>();
        return targetStatus != null && targetStatus.isEnemy;
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
