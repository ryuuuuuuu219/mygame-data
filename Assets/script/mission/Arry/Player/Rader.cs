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

        radarRect = GetComponent<RectTransform>();
        if (PlayerBlip != null) playerBlipRect = PlayerBlip.GetComponent<RectTransform>();

        if (worldCamera == null) worldCamera = Camera.main;
        if (player == null) Debug.LogError("RadarSystem: player not assigned.");
    }

    [Header("Radar Settings")]
    public float detectRange = 3000f;       // レーダー探知範囲（m）
    public float radarRadius = 100f;        // UI上の半径(px)（blipContainer上の最大距離）
    RectTransform radarRect;
    RectTransform playerBlipRect;

    // Update is called once per frame
    void Update()
    {
        RefreshDetections();

        UpdateBlipGroup(arrys, arrysUI, friendColor);
        UpdateEnemyBlipGroup(enemys, enemysUI);

        DisableUnused(arrysUI, arrys.Count);
        DisableUnused(enemysUI, enemys.Count);
        DisableUnused(targetsUI, 0);
    }
    void UpdateBlipGroup(
        List<GameObject> objects,
        List<GameObject> uiList,
        Color color)
    {
        for (int i = 0; i < objects.Count; i++)
        {
            if (objects[i] == null) continue;

            RectTransform rt = uiList[i].GetComponent<RectTransform>();
            Image img = uiList[i].GetComponent<Image>();
            Vector2 pos = RadarSquarePosition(objects[i].transform.position, rt);

            rt.anchoredPosition = GetPlayerBlipPosition() + pos;
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
    void UpdateEnemyBlipGroup(
        List<GameObject> objects,
        List<GameObject> uiList)
    {
        for (int i = 0; i < objects.Count; i++)
        {
            if (objects[i] == null) continue;

            RectTransform rt = uiList[i].GetComponent<RectTransform>();
            Image img = uiList[i].GetComponent<Image>();
            Vector2 pos = RadarSquarePosition(objects[i].transform.position, rt);

            rt.anchoredPosition = GetPlayerBlipPosition() + pos;
            img.color = IsMissionTarget(objects[i]) ? targetColor : enemyColor;
            img.enabled = true;
        }
    }
    Vector2 RadarSquarePosition(Vector3 worldPos, RectTransform blipRect)
    {
        Vector3 dir = worldPos - player.position;

        // プレイヤー基準に回転
        dir = Quaternion.Euler(0, -player.eulerAngles.y, 0) * dir;

        // XZ → XY
        Vector2 p = new Vector2(dir.x, dir.z);

        // 探知距離で正規化
        p /= detectRange;

        // レーダー枠外なら円周へ投影
        float magnitude = p.magnitude;
        if (magnitude > 1f)
        {
            p /= magnitude;
        }

        float halfBlipSize = 0f;
        if (blipRect != null)
        {
            Rect rect = blipRect.rect;
            Vector3 scale = blipRect.lossyScale;
            halfBlipSize = Mathf.Max(rect.width * scale.x, rect.height * scale.y) * 0.5f;
        }

        float rectRadius = radarRadius;
        if (radarRect != null)
        {
            Rect rect = radarRect.rect;
            rectRadius = Mathf.Min(rect.width, rect.height) * 0.5f;
        }

        float effectiveRadius = Mathf.Max(0f, Mathf.Min(radarRadius, rectRadius) - halfBlipSize);
        return p * effectiveRadius;
    }

    Vector2 GetPlayerBlipPosition()
    {
        if (playerBlipRect != null)
        {
            return playerBlipRect.anchoredPosition;
        }

        return PlayerBlip != null ? (Vector2)PlayerBlip.transform.localPosition : Vector2.zero;
    }

    bool IsMissionTarget(GameObject obj)
    {
        return obj != null &&
            obj.TryGetComponent(out AugumentStatus aug) &&
            aug.missionObjective;
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
        if (enemys == null) enemys = new List<GameObject>();

        // 足りない場合はプール拡張
        while (enemysUI.Count < enemys.Count)
        {
            GameObject u = Instantiate(blipPrefab, RaderUI.transform);
            u.GetComponent<Image>().enabled = false;
            enemysUI.Add(u);
        }
        arrys = ObjectManager.Instance.allies;
        if (arrys == null) arrys = new List<GameObject>();
        // 足りない場合はプール拡張
        while (arrysUI.Count < arrys.Count)
        {
            GameObject u = Instantiate(blipPrefab, RaderUI.transform);
            u.GetComponent<Image>().enabled = false;
            arrysUI.Add(u);
        }

        targets = new List<GameObject>();
        for (int i = 0; i < enemys.Count; i++)
        {
            GameObject enemy = enemys[i];
            if (enemy == null) continue;
            if (enemy.TryGetComponent(out AugumentStatus aug) && aug.missionObjective)
                targets.Add(enemy);
        }
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
