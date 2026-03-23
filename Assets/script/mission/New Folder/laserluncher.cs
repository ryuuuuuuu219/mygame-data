using System.Collections.Generic;
using System.ComponentModel;
using Unity.VisualScripting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

public class laserluncher : MonoBehaviour
{
    LineRenderer lr;
    LineRenderer lr2;
    GameObject lobj;
    public GameObject target;

    Quaternion site, tgtsite;
    Vector3 sweepAxis = Vector3.up;
    [SerializeField] float followspe = 45f;
    [SerializeField] float followspelunching = 5f;

    public float energy = 0, genRate=1f, threshold = 50f;

    [Range(0.001f, 0.1f)]
    public float tensorStrength = 0.1f;
    [Range(10f, 1000f)]
    public float size = 10f;

    public int count = 50;

    public solving solvcs;
    AugumentStatus status;
    int maxmode, currentmode;
    int currentphase;
    List<Vertexdata> pathvertices = new List<Vertexdata>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        TryGetComponent<AugumentStatus>(out status);

        currentmode = 0;
        solvcs = GetComponent<solving>();
        maxmode = solvcs == null ? 0 : 1;

        if(solvcs != null)
        {
            solvcs.separation = 1f;
            solvcs.size = 1f;
        }

        lr = transform.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));

        lr.enabled = false;
        Color color = new Color(1f, 0.7f, 1f, 0.3f);
        lr.startColor = color;
        lr.endColor = color;
        lr.startWidth = 5f;
        lr.endWidth = 5f;

        #region TargetLine
        lobj = new GameObject("TargetLine");

        lr2 = lobj.transform.AddComponent<LineRenderer>();
        lr2.material = new Material(Shader.Find("Sprites/Default"));

        lr2.enabled = false;
        lr2.startColor = Color.red;
        lr2.endColor = Color.red;
        lr2.startWidth = 5f;
        lr2.endWidth = 5f;
        #endregion

        site = Quaternion.LookRotation(Vector3.up);
    }

    bool islunch = false;
    // Update is called once per frame
    void Update()
    {
        if (target == null) return;

        Vector3 leadtgtPos = target.transform.position + target.transform.forward * 200f;
        Vector3 leadtgtdir = (leadtgtPos - transform.position).normalized;
        Quaternion leadtgtrot = Quaternion.LookRotation(leadtgtdir);
        float dist = Vector3.Distance(target.transform.position, transform.position);

        switch (currentmode)
        {
            case 0:

                if (islunch)
                {
                    energy -= Time.deltaTime;
                    FireLaser();
                    float angle = followspelunching * Time.deltaTime;
                    site = Quaternion.AngleAxis(angle, sweepAxis) * site;
                }
                else
                {
                    energy += genRate * Time.deltaTime;

                    if (!islunch &&
                        energy > threshold)
                    {
                        islunch = true;
                        FireLaser(true);
                        int point = Mathf.FloorToInt(dist / size);
                        point = Mathf.Clamp(point, 0, count - 1);

                        Vector3 dir = (lr.GetPosition(point) - transform.position).normalized;
                        Quaternion rot = Quaternion.LookRotation(dir);
                        Vector3 tgtdir = (target.transform.position - transform.position).normalized;
                        Quaternion tgtrot = Quaternion.LookRotation(tgtdir);

                        lr2.enabled = true;
                        lr2.positionCount = 2;
                        lr2.SetPosition(0, target.transform.position);
                        lr2.SetPosition(1, lr.GetPosition(point));

                        Quaternion delta = tgtrot * Quaternion.Inverse(rot);
                        tgtsite = site * delta;

                        sweepAxis = Vector3.Cross(dir, tgtdir).normalized;

                        if (sweepAxis == Vector3.zero)
                        {
                            sweepAxis = transform.up; // フォールバック
                        }
                    }
                    site = Quaternion.RotateTowards(site, leadtgtrot, followspe * Time.deltaTime);
                    tgtsite = leadtgtrot;
                }
                if (energy < 0)
                {
                    islunch = false;
                    lr.enabled = false;
                    lr2.enabled = false;
                }
                if (dist < 1500f &&
                    maxmode > 0 )
                {
                    //とりあえず止めてから次の段階へ
                    islunch = false;
                    lr.enabled = false;
                    lr2.enabled = false;
                    Debug.Log("Phase1 to Phase2");
                    statusSolve();

                    currentmode++;
                }
                break;
            case 1:
                timer += Time.deltaTime;
                solvcs.size = Mathf.Lerp(1f, 0.6f, timer / 3f);
                solvcs.separation = Mathf.Lerp(1f, 10f, timer / 3f);
                if (timer > 3f)
                {
                    pathvertices = solvcs.vertices;
                    currentmode++;
                    timer = 0f;
                }
                break;
            case 2:

                if (islunch)
                {
                    energy -= Time.deltaTime;
                    FireLaser2();
                }
                else
                {
                    energy += genRate * Time.deltaTime;

                    if (!islunch &&
                        energy > threshold)
                    {
                        islunch = true;
                        FireLaser2(true);
                    }
                }
                if (energy < 0)
                {
                    islunch = false;
                    lr.enabled = false;
                    lr2.enabled = false;
                }
                if (dist >= 1500f)
                {
                    islunch = false;
                    lr.enabled = false;
                    lr2.enabled = false;
                    Debug.Log("Phase2 to Phase1");

                    currentmode++;
                }

                break;
            case 3:
                timer += Time.deltaTime;
                solvcs.size = Mathf.Lerp(0.6f, 1f, timer / 3f);
                solvcs.separation = Mathf.Lerp(10f, 1f, timer / 3f);
                if (timer > 3f)
                {
                    currentmode = 0;
                    timer = 0f;
                    statusMarge();
                }
                break;
            default:
                break;
        }
    }
    float timer = 0f;

    void FireLaser(bool isInit=false)
    {
        lr.enabled = true;
        lr.positionCount = count;

        Vector3 cPos= transform.position;
        Vector3 dir = site * Vector3.forward;


        for (int i = 0; i < count; i++)
        {
            if (isInit)
            {
                lr.SetPosition(i, cPos);

                // 空間による歪み（方向だけ変える）
                dir += TensorField(cPos, tensorStrength * i);
                dir.Normalize();

                // 位置を進める
                cPos += dir * size;
            }
            else
            {
                float angle = followspelunching * Time.deltaTime; 
                Vector3 origin = transform.position;
                Vector3 pos = lr.GetPosition(i) - origin;
                pos = Quaternion.AngleAxis(angle, sweepAxis) * pos;
                pos += origin;
                lr.SetPosition(i, pos);
            }
        }
    }

    Vector3 TensorField(Vector3 p, float max)
    {
        float scale = 1f / size;
        var v = new Vector3(
            Mathf.PerlinNoise(p.y * scale, p.z * scale) - 0.5f,
            Mathf.PerlinNoise(p.z * scale, p.x * scale) - 0.5f,
            Mathf.PerlinNoise(p.x * scale, p.y * scale) - 0.5f
        );
        float abs = v.magnitude;

        v = Vector3.ClampMagnitude(v, max);
        v = Vector3.ClampMagnitude(v, 0.001f);

        return v;
    }

    List<int> pathindex; 
    
    void FireLaser2(bool isInit = false)
    {
        // ---- ガード ----
        if (target == null || pathvertices == null || pathvertices.Count == 0)
        {
            lr.enabled = false;
            return;
        }

        // 使う経路数はローカルで管理（countを書き換えない）
        int pathCount = Mathf.Min(count, pathvertices.Count);

        lr.enabled = true;
        lr.positionCount = pathCount + 2; // 始点 + 経路 + 終点

        // ---- 初期化（経路シャッフル）----
        if (isInit || pathindex == null || pathindex.Count != pathCount)
        {
            pathindex = new List<int>(pathCount);

            // Fisher–Yates shuffle 用のインデックス配列
            List<int> indices = new List<int>(pathvertices.Count);
            for (int i = 0; i < pathvertices.Count; i++)
                indices.Add(i);

            for (int i = indices.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (indices[i], indices[j]) = (indices[j], indices[i]);
            }

            // 先頭から必要数だけ採用
            for (int i = 0; i < pathCount; i++)
                pathindex.Add(indices[i]);

            // 終点：最後の経路点 → ターゲット方向へ延長
            Vector3 lastPos = pathvertices[pathindex[pathCount - 1]].VcenterWorldPos;
            Vector3 dirToTarget = (target.transform.position - lastPos).normalized;
            lastsetPos = lastPos + dirToTarget * EXTEND_DISTANCE;
            lastsetPos = (lastsetPos - transform.position);
        }

        // ---- LineRenderer 座標設定 ----

        // 始点：発射位置
        lr.SetPosition(0, transform.position);

        // 経路：分割頂点のワールド座標
        for (int i = 0; i < pathCount; i++)
        {
            lr.SetPosition(
                i + 1,
                pathvertices[pathindex[i]].VcenterWorldPos
            );
        }


        Quaternion rot = Quaternion.Euler(solvcs.rot);
        lr.SetPosition(pathCount + 1, transform.position + rot * lastsetPos);
    }
    // 無限遠っぽく見せたい場合の延長距離
    const float EXTEND_DISTANCE = 1500f;
    Vector3 lastsetPos;

    void statusSolve()
    {
        if (solvcs != null &&
            status != null)
        {
            bool isTgt = status.missionObjective;
            float hp = status.CurrentStatus.GetVar("HP");
            float maxhp = status.maxhp;
            int waveId = status.waveID;

            for (int i = 0; i < solvcs.vertices.Count; i++)
            {
                var target = solvcs.vertices[i].obj;
                AugumentStatus s;
                if (target.TryGetComponent<AugumentStatus>(out s) == false)
                {
                    s = target.AddComponent<AugumentStatus>();
                    s.isEnemy = true;
                    s.waveID = waveId;
                }
                if (s != null)
                {
                    s.missionObjective = isTgt;
                    if (s.TryGetHP(out float h, out float max))
                    {
                        h = hp / solvcs.vertices.Count;
                        s.maxhp = maxhp / solvcs.vertices.Count;
                    }
                    s.maxhp = maxhp / solvcs.vertices.Count;
                }
                s.isVisible = true;
                s.issortie = true;
                s.lifeTime = status.lifeTime;
                ObjectManager.Instance.RegisterEnemy(target,waveId);
            }
            status.isVisible = false;
        }
    }
    void statusMarge()
    {
        if (solvcs != null &&
            status != null)
        {
            float hp = 0f;
            int waveId = status.waveID;
            for (int i = 0; i < solvcs.vertices.Count; i++)
            {
                var target = solvcs.vertices[i].obj;
                var s = target.GetComponent<AugumentStatus>();
                if (s != null)
                {
                    if (s.TryGetHP(out float h, out float max))
                    {
                        hp += h;
                    }
                }
                s.isVisible = false;
                s.issortie = false;
                //ObjectManager.Instance.UnregisterEnemy(target, waveId);
            }
            status.hp = hp;
            status.isVisible = true;
        }
    }
}
