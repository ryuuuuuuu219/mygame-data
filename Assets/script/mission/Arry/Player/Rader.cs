using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class Rader : MonoBehaviour
{

    [Header("References")]
    public Transform player;                // プレイヤー機（forwardを基準に回転）
    public Camera worldCamera;              // ワールド->スクリーン変換に使うカメラ（通常は Camera.main）

    [Header("Blip")]
    public Canvas RaderUI;                 // レーダーUIのCanvas
    public GameObject PlayerBlip;         // プレイヤー機のImageを持つGameObject
    public GameObject blipPrefab;           // Imageを持つプレハブ

    public List<GameObject> arrys;
    public List<GameObject> enemys;
    public List<GameObject> targets;

    [Header("Visuals")]
    public Color friendColor = Color.cyan;
    public Color enemyColor = Color.green;
    public Color targetColor = Color.red;

    // Use this for initialization
    void Start()
    {
        // 初期プール作成
        CreatePool(10, 10,10);  // 初期数は適当に設定、足りないときは動的追加

        if (worldCamera == null) worldCamera = Camera.main;
        if (player == null) Debug.LogError("RadarSystem: player not assigned.");
    }

    [Header("Radar Settings")]
    public float detectRange = 3000f;       // レーダー探知範囲（m）
    public float radarRadius = 100f;        // UI上の半径(px)（blipContainer上の最大距離）

    // Update is called once per frame
    void Update()
    {
        RefreshDetections();

        UpdateBlipGroup(arrys, arrysUI, friendColor);
        UpdateBlipGroup(enemys, enemysUI, enemyColor);
        UpdateBlipGroup(targets, targetsUI, targetColor);

        DisableUnused(arrysUI, arrys.Count);
        DisableUnused(enemysUI, enemys.Count);
        DisableUnused(targetsUI, targets.Count);
    }
    void UpdateBlipGroup(
        List<GameObject> objects,
        List<GameObject> uiList,
        Color color)
    {
        for (int i = 0; i < objects.Count; i++)
        {
            if (objects[i] == null) continue;

            Vector2 pos = RadarSquarePosition(objects[i].transform.position);

            RectTransform rt = uiList[i].GetComponent<RectTransform>();
            Image img = uiList[i].GetComponent<Image>();

            rt.localPosition = PlayerBlip.transform.localPosition + (Vector3)pos;
            img.color = color;
            img.enabled = true;
        }
    }
    void DisableUnused(List<GameObject> uiList, int usedCount)
    {
        for (int i = usedCount; i < uiList.Count; i++)
        {
            uiList[i].GetComponent<Image>().enabled = false;
        }
    }
    Vector2 RadarSquarePosition(Vector3 worldPos)
    {
        Vector3 dir = worldPos - player.position;

        // プレイヤー基準に回転
        dir = Quaternion.Euler(0, -player.eulerAngles.y, 0) * dir;

        // XZ → XY
        Vector2 p = new Vector2(dir.x, dir.z);

        // 探知距離で正規化
        p /= detectRange;

        // 正方形外なら外周へ投影
        float max = Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.y));
        if (max > 1f)
        {
            p /= max;
        }

        return p * radarRadius;
    }


    public List<GameObject> arrysUI;
    public List<GameObject> enemysUI;
    public List<GameObject> targetsUI;

    // プレハブのプール作成
    void CreatePool(int arryCount, int enemyCount, int targetCount)
    {
        for (int i = 0; i < arryCount; i++)
        {
            GameObject u = Instantiate(blipPrefab, RaderUI.transform);
            u.GetComponent<Image>().enabled = false;
            arrysUI.Add(u);
        }
        for (int i = 0; i < enemyCount; i++)
        {
            GameObject u = Instantiate(blipPrefab, RaderUI.transform);
            u.GetComponent<Image>().enabled = false;
            enemysUI.Add(u);
        }
        for (int i = 0; i < targetCount; i++)
        {
            GameObject u = Instantiate(blipPrefab, RaderUI.transform);
            u.GetComponent<Image>().enabled = false;
            targetsUI.Add(u);
        }
    }
    void RefreshDetections()
    {
        enemys = ObjectManager.Instance.Enemies as List<GameObject>;

        // 足りない場合はプール拡張
        while (enemysUI.Count < enemys.Count)
        {
            GameObject u = Instantiate(blipPrefab, RaderUI.transform);
            u.GetComponent<Image>().enabled = false;
            enemysUI.Add(u);
        }
        arrys = ObjectManager.Instance.allies;
        // 足りない場合はプール拡張
        while (arrysUI.Count < arrys.Count)
        {
            GameObject u = Instantiate(blipPrefab, RaderUI.transform);
            u.GetComponent<Image>().enabled = false;
            arrysUI.Add(u);
        }

        List<GameObject> detecttargets = enemys;
        for (int i = enemys.Count-1; i >= 0; i--)
        {
            if (detecttargets[i] == null) continue;
            if (detecttargets[i].TryGetComponent(out AugumentStatus aug))
            {
                if (!aug.missionObjective)
                {
                    detecttargets.Remove(enemys[i]);
                    enemys.RemoveAt(i);
                }
            }
        }
        targets = detecttargets;
        //Debug.LogError("brake pt.");
        // 足りない場合はプール拡張
        while (targetsUI.Count < targets.Count)
        {
            GameObject u = Instantiate(blipPrefab,RaderUI.transform);
            u.GetComponent<Image>().enabled = false;
            targetsUI.Add(u);
        }

    }
}