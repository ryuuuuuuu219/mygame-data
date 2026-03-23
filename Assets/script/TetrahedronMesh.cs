using System;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(LineRenderer))]
public class TetrahedronMesh : MonoBehaviour
{
    public Color faceColor;  // 面の色(半透明)
    public Color edgeColor;  // 辺の色
    public float edgeWidth = 0.05f;

    private LineRenderer lineRenderer;
    Mesh mesh;
    Material mat;

    [Header("ビューカメラ")]
    public Camera axisCamera;// ビューカメラ
    public Vector3 camPos;

    private Camera mainCam;// 座標基準用

    [Header("仮想球座標")]
    public float distance = 150f; // カメラから仮想球までの距離
    public float radius; // 仮想球の半径
    public Vector3 center; // 仮想球の中心

    [Header("描画")]
    public bool isArartUI;

    // 頂点座標
    Vector3[] vertices = new Vector3[]
    {
        new Vector3(0, 0, -1f),          // 頂点
        new Vector3(-1f, -0.57f, 1.63f),   // 底面1
        new Vector3(1f, -0.57f, 1.63f),    // 底面2
        new Vector3(0, 1, 1.63f)       // 底面3
    };

    int[] triangles = new int[]
    {
        0, 1, 2,
        0, 2, 3,
        0, 3, 1,
        1, 3, 2 // 底面
    };
    public GameObject targetObj;
    public GameObject playerObj;

    public void Visible(bool enabled)
    {
        mesh = GetComponent<MeshFilter>()?.mesh;
        if (GetComponent<MeshRenderer>() != null)
        {
            GetComponent<MeshRenderer>().enabled = enabled;
        }
        if (GetComponent<LineRenderer>() != null)
        {
            if (lineRenderer == null)
            {
                lineRenderer = GetComponent<LineRenderer>();
            }
            lineRenderer.enabled = enabled;
        }
    }

    void Start()
    {
        mainCam = Camera.main;

        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();


        // マテリアル初期化
        mat = new Material(Shader.Find("Standard"));
        mat.SetFloat("_Mode", 3);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0); 

        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 4000;
        
        // Start() で1回だけ
        var mr = GetComponent<MeshRenderer>();
        mr.material = mat;

        // LineRenderer 初期化
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.widthMultiplier = edgeWidth;
        lineRenderer.loop = false; // ループ無効
        lineRenderer.positionCount = 0;

        // ② Meshはローカル座標のまま
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    void Update()
    {
        if (!targetObj)
        {
            mat.color = Color.clear;
            lineRenderer.startColor = Color.clear;
            lineRenderer.endColor = Color.clear;
            return;
        }

        /*
         * 操作可能カメラmainCamはスティックによって回転操作される
         * 基準用カメラaxisCameraはプレイヤー機の向きに追従する
         */

        // カメラ基準でターゲット方向を計算
        Vector3 toTargetWorld = (targetObj.transform.position - mainCam.transform.position).normalized;

        Vector3 toTargetLocal = mainCam.transform.InverseTransformDirection(toTargetWorld);




        // 前後判定 (カメラのforwardとtoTargetの内積)
        bool isFront = Vector3.Dot(axisCamera.transform.forward, toTargetWorld) > 0f;


        // 半径と表示色
        radius = Mathf.Min(Vector3.Distance(playerObj.transform.position, targetObj.transform.position), 850f);
        radius /= 850f; // サイズ調整

        float alpha = isArartUI ? 0.8f : 0f;

        faceColor = isFront ? new Color(1f, 1f, 0f, alpha) : new Color(1f, 0f, 0f, alpha);
        edgeColor = isFront ? Color.yellow : Color.red;

        // 仮想球の中心
        center = mainCam.transform.position + mainCam.transform.forward * distance;


        /*
         * 球の中心はプレイヤーの視界＝基準用カメラの前方に固定
         * この球面上の位置をプレイヤーの操作（mainCam）の回転量で補正したい
         */


        // 三角錐の位置（球の表面）
        Vector3 posOnSphere =
        center +
        mainCam.transform.TransformDirection(toTargetLocal) *
        (10f + radius * 40f);

        Quaternion rot = Quaternion.LookRotation(
        mainCam.transform.TransformDirection(toTargetLocal),
        mainCam.transform.up
        );

        transform.SetPositionAndRotation(posOnSphere, rot);
        // 頂点を回転・配置
        Vector3[] worldVertices = new Vector3[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 local = vertices[i] * (1f + Mathf.Clamp01(1f - radius));
            worldVertices[i] = posOnSphere + rot * local;

        }

        // 辺更新
        Vector3[] edges = {
        worldVertices[0], worldVertices[1],
        worldVertices[1], worldVertices[2],
        worldVertices[2], worldVertices[0],
        worldVertices[0], worldVertices[3],
        worldVertices[3], worldVertices[1],
        worldVertices[3], worldVertices[2]
        };

        lineRenderer.positionCount = edges.Length;

        lineRenderer.useWorldSpace = true;
        lineRenderer.SetPositions(edges);

        lineRenderer.startColor = edgeColor;
        lineRenderer.endColor = edgeColor;

        mat.color = faceColor;

        // ① オブジェクト自体を配置
        transform.position = posOnSphere;
        transform.rotation = rot;
    }

}
