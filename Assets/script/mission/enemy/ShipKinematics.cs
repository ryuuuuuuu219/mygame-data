using System.Collections.Generic;
using UnityEngine;

public class ShipKinematics : MonoBehaviour
{
    public AugumentStatus status;

    [Header("Kinematics")]
    public Vector3 pseudoVelocity;     // 疑似艦体速度
    Vector3 lastPos;

    [Header("Turrets")]
    public List<GameObject> turrets = new();
    public bool isturretDestroyed = false;

    Dictionary<GameObject, AugumentStatus> registeredTurrets = new();

    public float scaleFactor => transform.lossyScale.x;
    void Awake()
    {
        status = GetComponent<AugumentStatus>();
        lastPos = transform.position;

        status.OnDestroyed += DestroyAllTurrets;

        Vector3 originalScale = transform.localScale;
        foreach (var t in turrets)
        {
            if (t != null)
            {
                registeredTurrets[t] = t.GetComponent<AugumentStatus>();
            }
        }
    }

    void LateUpdate()
    {
        pseudoVelocity = transform.position - lastPos;
        pseudoVelocity /= Time.fixedDeltaTime;
        status.Velocity = pseudoVelocity;
        lastPos = transform.position;

        isturretDestroyed = false;
        // 全砲台に配布
        for (int i = 0; i < turrets.Count; i++)
        {
            var t = turrets[i];
            if (t != null && registeredTurrets.ContainsKey(t))
            {
                var tstatus = registeredTurrets[t];
                tstatus.Velocity = pseudoVelocity;
            }
            else if (t == null)
            {
                isturretDestroyed = true;
            }
        }

        if (isturretDestroyed && turrets.Count == 0)
        {
            status.missionObjective=false;
        }

    }

    public void RegisterTurret(GameObject turret)
    {
        if (!turrets.Contains(turret))
            turrets.Add(turret);
    }

    public void DestroyAllTurrets()
    {
        var OM = ObjectManager.Instance;
        foreach (var t in turrets)
        {
            if (t != null)
            {
                OM.UnregisterEnemy(t, status.waveID);
                Destroy(t.gameObject);
            }
        }
        turrets.Clear();
    }
}
