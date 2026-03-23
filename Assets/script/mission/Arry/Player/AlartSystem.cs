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

    // プール
    public List<GameObject> lockAlerts = new List<GameObject>();
    public List<GameObject> missileAlerts = new List<GameObject>();
    [SerializeField]Camera Camera;

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
    }
}
