using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

[System.Serializable]
public class Vertexdata
{
    public GameObject obj;
    public LineRenderer lr;
    public Vector3 VcenterPos;
    public Vector3 VcenterWorldPos;
    public List<Vector3> offset;
    public Quaternion rot;
}
public class solving : MonoBehaviour
{
    [Header("パラメータ調整")]

    [SerializeField][Range(0, 1)] int separationmode = 0;
    [SerializeField]Vector3 rotspeed, rotspeedtotal;

    [SerializeField][Range(1, 10)] public float separation = 1;//拡散度
    [SerializeField][Range(0, 1)] public float size = 1;//破片ごとの大きさ
    [SerializeField][Range(0, 1000)] public float totalsize = 1;//全体の大きさ
    [SerializeField] public Vector3 rot;
    [SerializeField] Vector3 objPos;

    [SerializeField] public List<Vertexdata> vertices;

    //説明
    //重心を原点に配置した辺の長さが2の立方体を24分割します
    //頂点2、面の中点2を頂点とする三角錐8
    //面の中心3、頂点1を頂点とする三角錐8
    //残った八面体を原点で8分割
    //この時の頂点データを出してください
    //
    List<Vector3> basePoints = new();

    enum BP
    {
        // cube corners
        Ppp, Ppn, Pnp, Pnn,
        Npp, Npn, Nnp, Nnn,

        // face centers
        Px, Nx, Py, Ny, Pz, Nz,

        // center
        C
    }
    void basepointInit()
    {
        basePoints = new()
    {
        new( 1, 1, 1), // Ppp
        new( 1, 1,-1), // Ppn
        new( 1,-1, 1), // Pnp
        new( 1,-1,-1), // Pnn
        new(-1, 1, 1), // Npp
        new(-1, 1,-1), // Npn
        new(-1,-1, 1), // Nnp
        new(-1,-1,-1), // Nnn

        Vector3.right,  // Px
        Vector3.left,   // Nx
        Vector3.up,     // Py
        Vector3.down,   // Ny
        Vector3.forward,// Pz
        Vector3.back,   // Nz

        Vector3.zero    // C
    };
    }

    void build(int i)
    {
        Vertexdata data = new();
        data.obj = new GameObject("vertex" + i);
        data.lr = data.obj.AddComponent<LineRenderer>();

        data.lr.material = new Material(Shader.Find("Sprites/Default"));
        data.lr.startColor = Color.white;
        data.lr.endColor = Color.white;
        data.lr.loop = false;
        data.lr.startWidth = 0.05f;
        data.lr.endWidth = 0.05f;
        data.lr.useWorldSpace = false;

        vertices.Add(data);
    }

    void solve0(int i, params BP[] ids)
    {
        var data = vertices[i];

        cell(out data.VcenterPos, out data.offset, ids);

        data.lr.positionCount = orderTable[ids.Length].Length;
        for (int j = 0; j < orderTable[ids.Length].Length; j++)
        {
            data.lr.SetPosition(j, data.offset[orderTable[ids.Length][j]]);
        }

        data.obj.transform.parent = this.transform;

    }

    void solve1(int i, params Vector3[] pos)
    {
        var data = vertices[i];

        cell2(out data.VcenterPos, out data.offset, pos);

        data.lr.positionCount = orderTable[pos.Length].Length;
        for (int j = 0; j < orderTable[pos.Length].Length; j++)
        {
            data.lr.SetPosition(j, data.offset[orderTable[pos.Length][j]]);
        }

        data.obj.transform.parent = this.transform;

    }

    void cell(out Vector3 center, out List<Vector3> offset, params BP[] ids)
    {
        center = Vector3.zero;

        // --- 重心（基準空間） ---
        foreach (var id in ids)
            center += basePoints[(int)id];

        center /= ids.Length;

        if (totalsize <= 0f)
        {
            offset = new List<Vector3>();
            return;
        }

        // ★ 全体サイズをここで一度だけ適用
        center *= totalsize / 2f;

        // --- 破片形状 ---
        offset = new List<Vector3>(ids.Length);

        foreach (var id in ids)
        {
            Vector3 local = basePoints[(int)id] - (center / (totalsize / 2f));
            offset.Add(local * (totalsize / 2f) * size);
        }
    }

    void cell2(out Vector3 center, out List<Vector3> offset, Vector3[] pos)
    {
        center = Vector3.zero;

        // --- 重心（基準空間） ---
        foreach (var p in pos)
            center += p;

        center /= pos.Length;

        if (totalsize <= 0f)
        {
            offset = new List<Vector3>();
            return;
        }

        // ★ 全体サイズをここで一度だけ適用
        center *= totalsize / 2f;

        // --- 破片形状 ---
        offset = new List<Vector3>(pos.Length);

        foreach (var p in pos)
        {
            Vector3 local = p - (center / (totalsize / 2f));
            offset.Add(local * (totalsize / 2f) * size);
        }
    }

    Vector3 BP2Vec3(params BP[] bp)
    {
        Vector3 res = Vector3.zero;
        foreach (var b in bp)
        {
            res += basePoints[(int)b];
        }
        res /= bp.Length;
        return res;
    }

    


    Dictionary<int, int[]> orderTable = new()
{
    { 4, new[] { 0,1,2,3, 1,3,0,2 } },
    { 5, new[] { 0,1,2,3,4, 0,1,4,2,4,3 } }
};

    Vector3[][] tetraTableMode0, tetraTableMode1;



    private void define()
    {
        tetraTableMode0 = new Vector3[][]
        {
                new Vector3[]{BP2Vec3(BP.Px), BP2Vec3(BP.Py), BP2Vec3(BP.Pz), BP2Vec3(BP.C)} ,
                new Vector3[]{BP2Vec3(BP.Px), BP2Vec3(BP.Py), BP2Vec3(BP.Nz), BP2Vec3(BP.C)} ,
                new Vector3[]{BP2Vec3(BP.Px), BP2Vec3(BP.Ny), BP2Vec3(BP.Pz), BP2Vec3(BP.C)} ,
                new Vector3[]{BP2Vec3(BP.Px),BP2Vec3(BP.Ny),BP2Vec3(BP.Nz),BP2Vec3(BP.C)} ,
                new Vector3[]{BP2Vec3(BP.Nx),BP2Vec3(BP.Py),BP2Vec3(BP.Pz),BP2Vec3(BP.C)} ,
                new Vector3[]{BP2Vec3(BP.Nx),BP2Vec3(BP.Py),BP2Vec3(BP.Nz),BP2Vec3(BP.C)} ,
                new Vector3[]{BP2Vec3(BP.Nx),BP2Vec3(BP.Ny),BP2Vec3(BP.Pz),BP2Vec3(BP.C)} ,
                new Vector3[]{BP2Vec3(BP.Nx),BP2Vec3(BP.Ny),BP2Vec3(BP.Nz),BP2Vec3(BP.C)} ,

                new Vector3[]{BP2Vec3(BP.Px),BP2Vec3(BP.Py),BP2Vec3(BP.Ppp),BP2Vec3(BP.Ppn)} ,
                new Vector3[]{BP2Vec3(BP.Px),BP2Vec3(BP.Ny),BP2Vec3(BP.Pnp),BP2Vec3(BP.Pnn)} ,
                new Vector3[]{BP2Vec3(BP.Nx),BP2Vec3(BP.Py),BP2Vec3(BP.Npp),BP2Vec3(BP.Npn)} ,
                new Vector3[]{BP2Vec3(BP.Nx),BP2Vec3(BP.Ny),BP2Vec3(BP.Nnp),BP2Vec3(BP.Nnn)} ,
                new Vector3[]{BP2Vec3(BP.Px),BP2Vec3(BP.Pz),BP2Vec3(BP.Ppp),BP2Vec3(BP.Pnp)} ,
                new Vector3[]{BP2Vec3(BP.Px),BP2Vec3(BP.Nz),BP2Vec3(BP.Ppn),BP2Vec3(BP.Pnn)} ,
                new Vector3[]{BP2Vec3(BP.Nx),BP2Vec3(BP.Pz),BP2Vec3(BP.Npp),BP2Vec3(BP.Nnp)} ,
                new Vector3[]{BP2Vec3(BP.Nx),BP2Vec3(BP.Nz),BP2Vec3(BP.Npn),BP2Vec3(BP.Nnn)} ,
                new Vector3[]{BP2Vec3(BP.Py),BP2Vec3(BP.Pz),BP2Vec3(BP.Ppp),BP2Vec3(BP.Npp)} ,
                new Vector3[]{ BP2Vec3(BP.Py), BP2Vec3(BP.Nz), BP2Vec3(BP.Ppn), BP2Vec3(BP.Npn)} ,
                new Vector3[]{ BP2Vec3(BP.Ny), BP2Vec3(BP.Pz), BP2Vec3(BP.Pnp), BP2Vec3(BP.Nnp)} ,
                new Vector3[]{ BP2Vec3(BP.Ny), BP2Vec3(BP.Nz), BP2Vec3(BP.Pnn), BP2Vec3(BP.Nnn)} ,

                new Vector3[]{ BP2Vec3(BP.Px), BP2Vec3(BP.Py), BP2Vec3(BP.Ppp), BP2Vec3(BP.Pz)} ,
                new Vector3[]{ BP2Vec3(BP.Px), BP2Vec3(BP.Py), BP2Vec3(BP.Ppn), BP2Vec3(BP.Nz)} ,
                new Vector3[]{ BP2Vec3(BP.Px), BP2Vec3(BP.Ny), BP2Vec3(BP.Pnp), BP2Vec3(BP.Pz)} ,
                new Vector3[]{ BP2Vec3(BP.Px), BP2Vec3(BP.Ny), BP2Vec3(BP.Pnn), BP2Vec3(BP.Nz)} ,
                new Vector3[]{ BP2Vec3(BP.Nx), BP2Vec3(BP.Py), BP2Vec3(BP.Npp), BP2Vec3(BP.Pz)} ,
                new Vector3[]{ BP2Vec3(BP.Nx), BP2Vec3(BP.Py), BP2Vec3(BP.Npn), BP2Vec3(BP.Nz)} ,
                new Vector3[]{ BP2Vec3(BP.Nx), BP2Vec3(BP.Ny), BP2Vec3(BP.Nnp), BP2Vec3(BP.Pz)} ,
                new Vector3[]{ BP2Vec3(BP.Nx), BP2Vec3(BP.Ny), BP2Vec3(BP.Nnn), BP2Vec3(BP.Nz)}
        };
        tetraTableMode1 = new Vector3[][]
{
    // ===== +X face (Px) =====
    new[]{ BP2Vec3(BP.Px), BP2Vec3(BP.Ppp), BP2Vec3(BP.Ppn), BP2Vec3(BP.C) },
    new[]{ BP2Vec3(BP.Px), BP2Vec3(BP.Ppn), BP2Vec3(BP.Pnn), BP2Vec3(BP.C) },
    new[]{ BP2Vec3(BP.Px), BP2Vec3(BP.Pnn), BP2Vec3(BP.Pnp), BP2Vec3(BP.C) },
    new[]{ BP2Vec3(BP.Px), BP2Vec3(BP.Pnp), BP2Vec3(BP.Ppp), BP2Vec3(BP.C) },

    // ===== -X face (Nx) =====
    new[]{ BP2Vec3(BP.Nx), BP2Vec3(BP.Npp), BP2Vec3(BP.Npn), BP2Vec3(BP.C) },
    new[]{ BP2Vec3(BP.Nx), BP2Vec3(BP.Npn), BP2Vec3(BP.Nnn), BP2Vec3(BP.C) },
    new[]{ BP2Vec3(BP.Nx), BP2Vec3(BP.Nnn), BP2Vec3(BP.Nnp), BP2Vec3(BP.C) },
    new[]{ BP2Vec3(BP.Nx), BP2Vec3(BP.Nnp), BP2Vec3(BP.Npp), BP2Vec3(BP.C) },

    // ===== +Y face (Py) =====
    new[]{ BP2Vec3(BP.Py), BP2Vec3(BP.Ppp), BP2Vec3(BP.Npp), BP2Vec3(BP.C) },
    new[]{ BP2Vec3(BP.Py), BP2Vec3(BP.Npp), BP2Vec3(BP.Npn), BP2Vec3(BP.C) },
    new[]{ BP2Vec3(BP.Py), BP2Vec3(BP.Npn), BP2Vec3(BP.Ppn), BP2Vec3(BP.C) },
    new[]{ BP2Vec3(BP.Py), BP2Vec3(BP.Ppn), BP2Vec3(BP.Ppp), BP2Vec3(BP.C) },

    // ===== -Y face (Ny) =====
    new[]{ BP2Vec3(BP.Ny), BP2Vec3(BP.Pnp), BP2Vec3(BP.Nnp), BP2Vec3(BP.C) },
    new[]{ BP2Vec3(BP.Ny), BP2Vec3(BP.Nnp), BP2Vec3(BP.Nnn), BP2Vec3(BP.C) },
    new[]{ BP2Vec3(BP.Ny), BP2Vec3(BP.Nnn), BP2Vec3(BP.Pnn), BP2Vec3(BP.C) },
    new[]{ BP2Vec3(BP.Ny), BP2Vec3(BP.Pnn), BP2Vec3(BP.Pnp), BP2Vec3(BP.C) },

    // ===== +Z face (Pz) =====
    new[]{ BP2Vec3(BP.Pz), BP2Vec3(BP.Ppp), BP2Vec3(BP.Pnp), BP2Vec3(BP.C) },
    new[]{ BP2Vec3(BP.Pz), BP2Vec3(BP.Pnp), BP2Vec3(BP.Nnp), BP2Vec3(BP.C) },
    new[]{ BP2Vec3(BP.Pz), BP2Vec3(BP.Nnp), BP2Vec3(BP.Npp), BP2Vec3(BP.C) },
    new[]{ BP2Vec3(BP.Pz), BP2Vec3(BP.Npp), BP2Vec3(BP.Ppp), BP2Vec3(BP.C) },

    // ===== -Z face (Nz) =====
    new[]{ BP2Vec3(BP.Nz), BP2Vec3(BP.Ppn), BP2Vec3(BP.Pnn), BP2Vec3(BP.C) },
    new[]{ BP2Vec3(BP.Nz), BP2Vec3(BP.Pnn), BP2Vec3(BP.Nnn), BP2Vec3(BP.C) },
    new[]{ BP2Vec3(BP.Nz), BP2Vec3(BP.Nnn), BP2Vec3(BP.Npn), BP2Vec3(BP.C) },
    new[]{ BP2Vec3(BP.Nz), BP2Vec3(BP.Npn), BP2Vec3(BP.Ppn), BP2Vec3(BP.C) },
};
    }
    void updatePos()
    {
        switch(separationmode)
        {
            case 0:
                for (int i = 0; i < 28; i++)
                {
                    solve1(i, tetraTableMode0[i]);
                }
                break;
                case 1:
                for (int i = 0; i < 24; i++)
                {
                    solve1(i, tetraTableMode1[i]);
                }
                    break;
                default:
                break;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        vertices = new();
        basepointInit();
        for (int i = 0; i < 28; i++)
        {
            build(i);
        }
        define();
    }

    private void Update()
    {
        rot += rotspeed * Time.deltaTime;
        transform.rotation = Quaternion.Euler(rot);
        updatePos();
        for (int i = 0; i < vertices.Count; i++)
        {
            var localPos = vertices[i].VcenterPos * (separation);
            Quaternion localrot = Quaternion.Euler(rot);
            vertices[i].obj.transform.position = objPos + localrot * localPos;

            vertices[i].rot = localrot;
            vertices[i].obj.transform.rotation = vertices[i].rot;

            vertices[i].obj.GetComponent<LineRenderer>().startWidth = totalsize / 1000;
            vertices[i].obj.GetComponent<LineRenderer>().endWidth = totalsize / 1000;

            vertices[i].VcenterWorldPos = vertices[i].obj.transform.position;
        }
    }
}
