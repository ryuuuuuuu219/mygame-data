using System.Collections.Generic;
using UnityEngine;

public class MissileShooterGroupManager : MonoBehaviour
{
    public MissileShooterGroup[] groups;
    public float shortInterval = 0.1f;
    public float longInterval = 3f;
    public bool disableLauncherControllers = true;

    EnemyTargetSelector targetSelector;
    AugumentStatus status;
    int groupIndex;
    int launcherIndex;
    float nextFireTime;
    bool waitingLongInterval;

    void Awake()
    {
        targetSelector = GetComponent<EnemyTargetSelector>();
        if (targetSelector == null)
            targetSelector = gameObject.AddComponent<EnemyTargetSelector>();

        status = GetComponent<AugumentStatus>();
    }

    void Start()
    {
        if (disableLauncherControllers)
            DisableLauncherControllers();
    }

    void Update()
    {
        if (groups == null || groups.Length == 0) return;
        if (Time.time < nextFireTime) return;

        if (!TryGetNextLauncher(out GameObject launcher))
        {
            nextFireTime = Time.time + longInterval;
            waitingLongInterval = true;
            ResetSequence();
            return;
        }

        if (waitingLongInterval)
            waitingLongInterval = false;

        if (TryFireLauncher(launcher))
        {
            launcherIndex++;
            nextFireTime = Time.time + shortInterval;
        }
        else
        {
            nextFireTime = Time.time + shortInterval;
        }
    }

    public void SetSingleGroup(IReadOnlyList<GameObject> launchers)
    {
        groups = new[]
        {
            new MissileShooterGroup
            {
                groupName = "VLS",
                launchers = launchers == null ? null : ToArray(launchers)
            }
        };

        ResetSequence();
        if (disableLauncherControllers)
            DisableLauncherControllers();
    }

    bool TryGetNextLauncher(out GameObject launcher)
    {
        launcher = null;

        while (groupIndex < groups.Length)
        {
            var group = groups[groupIndex];
            var launchers = group?.launchers;

            if (launchers == null || launcherIndex >= launchers.Length)
            {
                groupIndex++;
                launcherIndex = 0;
                continue;
            }

            launcher = launchers[launcherIndex];
            if (launcher != null)
                return true;

            launcherIndex++;
        }

        return false;
    }

    bool TryFireLauncher(GameObject launcher)
    {
        if (launcher == null || targetSelector == null || !targetSelector.HasLockedTarget) return false;

        Vector3 direction = targetSelector.target.transform.position - launcher.transform.position;
        Vector3 platformVelocity = status != null ? status.Velocity : Vector3.zero;

        if (launcher.TryGetComponent(out EnemyMissileShooter shooter))
        {
            return shooter.TryFire(direction, platformVelocity, targetSelector.target.transform, true);
        }

        return false;
    }

    void DisableLauncherControllers()
    {
        if (groups == null) return;

        foreach (var group in groups)
        {
            if (group?.launchers == null) continue;

            foreach (var launcher in group.launchers)
            {
                if (launcher == null) continue;
                if (launcher.TryGetComponent(out GroundAntiAirController controller))
                    controller.enabled = false;
            }
        }
    }

    void ResetSequence()
    {
        groupIndex = 0;
        launcherIndex = 0;
    }

    static GameObject[] ToArray(IReadOnlyList<GameObject> source)
    {
        var result = new GameObject[source.Count];
        for (int i = 0; i < source.Count; i++)
            result[i] = source[i];

        return result;
    }
}

[System.Serializable]
public class MissileShooterGroup
{
    public string groupName;
    public GameObject[] launchers;
}
