using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AlartSystem : MonoBehaviour
{
    [Header("ロック警報")]
    [SerializeField] List<GameObject> Lockingenemys;
    public bool[] LockingArray;

    [Header("ミサイル警報")]
    [SerializeField] List<GameObject> Missiles;
    public bool[] MissileArray;

    [Header("UIプレハブとCanvas")]
    public GameObject AlartUIprefub;
    public Canvas Canvas;

    [Header("警告音距離間隔")]
    public float warningNearDistance = 300f;
    public float warningFarDistance = 3500f;
    public float missileNearInterval = 0.18f;
    public float missileFarInterval = 0.8f;
    public float lockNearInterval = 0.45f;
    public float lockFarInterval = 1.4f;

    // プール
    public List<GameObject> lockAlerts = new List<GameObject>();
    public List<GameObject> missileAlerts = new List<GameObject>();
    [SerializeField]Camera Camera;
    bool hadLockThreat;
    bool hadMissileThreat;

    void Start()
    {
        // 初期プール作成
        CreatePool(10, 50);  // 初期数は適当に設定、足りないときは動的追加
    }

    // プレハブのプール作成
    void CreatePool(int enemyCount, int missileCount)
    {
        for (int i = 0; i < enemyCount; i++)
        {
            GameObject u = Instantiate(AlartUIprefub);
            u.name = "LockAlertUI_" + i;
            u.GetComponent<TetrahedronMesh>().Visible(false);
            lockAlerts.Add(u);
        }

        for (int i = 0; i < missileCount; i++)
        {
            GameObject u = Instantiate(AlartUIprefub);
            u.name = "MissileAlertUI_" + i;
            u.GetComponent<TetrahedronMesh>().Visible(false);
            missileAlerts.Add(u);
        }
    }

    // 敵情報更新
    void UpdateenemyInfo()
    {
        Lockingenemys = ObjectManager.Instance.Enemies as List<GameObject>;
        LockingArray = new bool[Lockingenemys.Count];

        // 足りない場合はプール拡張
        while (lockAlerts.Count < Lockingenemys.Count)
        {
            GameObject u = Instantiate(AlartUIprefub);
            u.name = "LockAlertUI_" + lockAlerts.Count;
            u.GetComponent<TetrahedronMesh>().isArartUI = false;
            u.GetComponent<TetrahedronMesh>().Visible(false);
            lockAlerts.Add(u);
        }

        for (int i = 0; i < Lockingenemys.Count; i++)
        {
            GameObject tgt=null;
            if (Lockingenemys[i] != null)
            {
                if (Lockingenemys[i].GetComponent<FCS_e>() != null)
                {
                    tgt = Lockingenemys[i].GetComponent<FCS_e>().target;
                }
                else
                {
                    if (Lockingenemys[i].GetComponent<AugumentStatus>() != null)
                    {
                        //tgt = Lockingenemys[i].GetComponent<AugumentStatus>().target;
                    }

                }
            }
            if (tgt != null && tgt == gameObject)
            {
                LockingArray[i] = true;
            }
        }
    }

    // ミサイル情報更新
    void UpdatemissileInfo()
    {
        Missiles = ObjectManager.Instance.missiles_e;
        MissileArray = new bool[Missiles.Count];

        // 足りない場合はプール拡張
        while (missileAlerts.Count < Missiles.Count)
        {
            GameObject u = Instantiate(AlartUIprefub);
            u.name = "MissileAlertUI_" + missileAlerts.Count;
            u.GetComponent<TetrahedronMesh>().isArartUI = true;
            u.GetComponent<TetrahedronMesh>().Visible(false);
            missileAlerts.Add(u);
        }

        for (int i = 0; i < Missiles.Count; i++)
        {
            if (Missiles[i] != null)
            {
                Missile missile = Missiles[i].GetComponent<Missile>();
                if (missile != null)
                {
                    MissileArray[i] = missile.target == gameObject.transform;
                }
            }
        }
    }

    // UI更新
    void UpdateAlartUI()
    {
        // ロック警報UI更新
        for (int i = 0; i < Lockingenemys.Count; i++)
        {
            GameObject ui = lockAlerts[i];
            if (LockingArray[i])
            {
                if (ui.GetComponent<TetrahedronMesh>() != null)
                {
                    ui.GetComponent<TetrahedronMesh>().Visible(true);
                    ui.GetComponent<TetrahedronMesh>().isArartUI = false;
                    ui.GetComponent<TetrahedronMesh>().targetObj = Lockingenemys[i];
                    ui.GetComponent<TetrahedronMesh>().axisCamera = Camera;
                    ui.GetComponent<TetrahedronMesh>().playerObj = gameObject;
                }
            }
            else
            {
                ui.GetComponent<TetrahedronMesh>().Visible(false);
            }
        }

        // ミサイル警報UI更新
        for (int i = 0; i < Missiles.Count; i++)
        {
            GameObject ui = missileAlerts[i];
            if (MissileArray[i])
            {
                if (ui.GetComponent<TetrahedronMesh>() != null)
                {
                    ui.GetComponent<TetrahedronMesh>().Visible(true);
                    ui.GetComponent<TetrahedronMesh>().isArartUI = true;
                    ui.GetComponent<TetrahedronMesh>().targetObj = Missiles[i];
                    ui.GetComponent<TetrahedronMesh>().axisCamera = Camera;
                    ui.GetComponent<TetrahedronMesh>().playerObj = gameObject;
                }
            }
            else
            {
                ui.GetComponent<TetrahedronMesh>().Visible(false);
            }
        }


        // 余剰UI非表示
        for (int i = Lockingenemys.Count; i < lockAlerts.Count; i++)
        {
            lockAlerts[i].GetComponent<TetrahedronMesh>().Visible(false);
        }
        for (int i = Missiles.Count; i < missileAlerts.Count; i++)
        {
            missileAlerts[i].GetComponent<TetrahedronMesh>().Visible(false);
        }

    }

    void Update()
    {
        UpdateenemyInfo();
        UpdatemissileInfo();
        UpdateAlartUI();
        UpdateAudioWarnings();
    }

    void UpdateAudioWarnings()
    {
        bool lockThreat = HasAnyTrue(LockingArray);
        bool missileThreat = HasAnyTrue(MissileArray);
        float lockInterval = GetThreatInterval(Lockingenemys, LockingArray, lockNearInterval, lockFarInterval);
        float missileInterval = GetThreatInterval(Missiles, MissileArray, missileNearInterval, missileFarInterval);

        if (lockThreat && !hadLockThreat)
            GeneratedAudioManager.Play(GeneratedAudioCue.LockWarning, null, 0.7f);
        if (missileThreat && !hadMissileThreat)
            GeneratedAudioManager.Play(GeneratedAudioCue.MissileWarning, null, 0.9f);

        GeneratedAudioManager.SetWarning(missileThreat, lockThreat, missileInterval, lockInterval);
        hadLockThreat = lockThreat;
        hadMissileThreat = missileThreat;
    }

    float GetThreatInterval(List<GameObject> threats, bool[] activeFlags, float nearInterval, float farInterval)
    {
        if (threats == null || activeFlags == null) return farInterval;

        float nearestDistance = float.MaxValue;
        int count = Mathf.Min(threats.Count, activeFlags.Length);
        for (int i = 0; i < count; i++)
        {
            if (!activeFlags[i] || threats[i] == null) continue;

            float distance = Vector3.Distance(transform.position, threats[i].transform.position);
            if (distance < nearestDistance)
                nearestDistance = distance;
        }

        if (nearestDistance == float.MaxValue) return farInterval;

        float near = Mathf.Max(0f, warningNearDistance);
        float far = Mathf.Max(near + 1f, warningFarDistance);
        float t = Mathf.InverseLerp(far, near, nearestDistance);
        return Mathf.Lerp(farInterval, nearInterval, t);
    }

    bool HasAnyTrue(bool[] values)
    {
        if (values == null) return false;
        for (int i = 0; i < values.Length; i++)
        {
            if (values[i]) return true;
        }
        return false;
    }
}
