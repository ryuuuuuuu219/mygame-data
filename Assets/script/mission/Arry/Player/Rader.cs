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
    public Color jammerWaveColor = new(0f, 1f, 0f, 1f);
    public Material jammerWaveMaterial;
    public int jammerWavePoolSize = 4;

    [Header("Mask")]
    public RectTransform radarMaskRoot;
    public bool createRadarMask = true;

    readonly List<Image> jammerWaveImages = new();

    // Use this for initialization
    void Start()
    {
        radarRect = GetComponent<RectTransform>();
        if (PlayerBlip != null) playerBlipRect = PlayerBlip.GetComponent<RectTransform>();

        if (worldCamera == null) worldCamera = Camera.main;
        if (player == null) Debug.LogError("RadarSystem: player not assigned.");
        EnsureRadarMask();

        arrysUI ??= new List<GameObject>();
        enemysUI ??= new List<GameObject>();
        targetsUI ??= new List<GameObject>();

        // 初期プール作成
        CreatePool(10, 10,10);  // 初期数は適当に設定、足りないときは動的追加

        EnsureJammerWavePool();
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
        UpdateJammerWaves();

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
            Vector2 pos = RadarSquarePosition(objects[i].transform.position);

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
            Vector2 pos = RadarSquarePosition(objects[i].transform.position);

            rt.anchoredPosition = GetPlayerBlipPosition() + pos;
            img.color = IsMissionTarget(objects[i]) ? targetColor : enemyColor;
            img.enabled = true;
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

        p.x = Mathf.Clamp(p.x, -1f, 1f);
        p.y = Mathf.Clamp(p.y, -1f, 1f);

        Vector2 halfSize = GetEffectiveRadarHalfSize();
        return new Vector2(p.x * halfSize.x, p.y * halfSize.y);
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
            GameObject u = Instantiate(blipPrefab, GetRadarContentRoot());
            u.GetComponent<Image>().enabled = false;
            arrysUI.Add(u);
        }
        for (int i = 0; i < enemyCount; i++)
        {
            GameObject u = Instantiate(blipPrefab, GetRadarContentRoot());
            u.GetComponent<Image>().enabled = false;
            enemysUI.Add(u);
        }
        for (int i = 0; i < targetCount; i++)
        {
            GameObject u = Instantiate(blipPrefab, GetRadarContentRoot());
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
            GameObject u = Instantiate(blipPrefab, GetRadarContentRoot());
            u.GetComponent<Image>().enabled = false;
            enemysUI.Add(u);
        }
        arrys = ObjectManager.Instance.allies;
        if (arrys == null) arrys = new List<GameObject>();
        // 足りない場合はプール拡張
        while (arrysUI.Count < arrys.Count)
        {
            GameObject u = Instantiate(blipPrefab, GetRadarContentRoot());
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
            GameObject u = Instantiate(blipPrefab, GetRadarContentRoot());
            u.GetComponent<Image>().enabled = false;
            targetsUI.Add(u);
        }

    }

    Transform GetRadarContentRoot()
    {
        EnsureRadarMask();
        if (radarMaskRoot != null) return radarMaskRoot;
        return RaderUI != null ? RaderUI.transform : transform;
    }

    void EnsureRadarMask()
    {
        if (!createRadarMask || radarRect == null) return;

        if (radarMaskRoot == null)
        {
            Transform existing = transform.Find("RadarMask");
            if (existing != null)
            {
                radarMaskRoot = existing.GetComponent<RectTransform>();
            }
        }

        if (radarMaskRoot == null)
        {
            GameObject maskObject = new GameObject("RadarMask", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
            maskObject.transform.SetParent(transform, false);
            radarMaskRoot = maskObject.GetComponent<RectTransform>();
        }

        radarMaskRoot.anchorMin = new Vector2(0.5f, 0.5f);
        radarMaskRoot.anchorMax = new Vector2(0.5f, 0.5f);
        radarMaskRoot.pivot = new Vector2(0.5f, 0.5f);
        radarMaskRoot.anchoredPosition = Vector2.zero;
        radarMaskRoot.sizeDelta = radarRect.rect.size;
        radarMaskRoot.localScale = Vector3.one;

        Image maskImage = radarMaskRoot.GetComponent<Image>();
        Image radarImage = GetComponent<Image>();
        if (maskImage != null && radarImage != null)
        {
            maskImage.sprite = radarImage.sprite;
            maskImage.type = radarImage.type;
            maskImage.preserveAspect = radarImage.preserveAspect;
            maskImage.color = Color.white;
        }

        Mask mask = radarMaskRoot.GetComponent<Mask>();
        if (mask != null)
        {
            mask.showMaskGraphic = false;
        }
    }

    void EnsureJammerWavePool()
    {
        if (RaderUI == null) return;

        if (jammerWaveMaterial == null)
        {
            Shader shader = Shader.Find("Custom/ECMJammerRadarWave");
            if (shader != null)
                jammerWaveMaterial = new Material(shader);
        }

        while (jammerWaveImages.Count < jammerWavePoolSize)
        {
            var waveObject = new GameObject("ECMJammerWave", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            waveObject.transform.SetParent(GetRadarContentRoot(), false);
            var image = waveObject.GetComponent<Image>();
            image.color = jammerWaveColor;
            image.raycastTarget = false;
            image.enabled = false;
            image.material = jammerWaveMaterial != null ? new Material(jammerWaveMaterial) : null;
            jammerWaveImages.Add(image);
        }
    }

    void UpdateJammerWaves()
    {
        EnsureJammerWavePool();

        int used = 0;
        foreach (var jammer in ECMJammer.ActiveJammers)
        {
            if (jammer == null || !jammer.isActiveAndEnabled || !jammer.affectRadar) continue;
            if (used >= jammerWaveImages.Count) break;

            Image image = jammerWaveImages[used];
            RectTransform rect = image.rectTransform;
            Vector2 pos = RadarSquarePosition(jammer.transform.position);
            Vector2 halfSize = GetEffectiveRadarHalfSize();
            float radiusPixels = Mathf.Clamp01(jammer.interferenceRadius / Mathf.Max(1f, detectRange)) *
                Mathf.Min(halfSize.x, halfSize.y);

            rect.SetAsFirstSibling();
            rect.anchoredPosition = GetPlayerBlipPosition() + pos;
            rect.sizeDelta = Vector2.one * radiusPixels * 2f;
            image.color = jammerWaveColor;
            image.enabled = true;

            if (image.material != null)
            {
                image.material.SetFloat("_TimeOffset", used * 0.17f);
                image.material.SetColor("_Color", jammerWaveColor);
            }

            used++;
        }

        for (int i = used; i < jammerWaveImages.Count; i++)
            jammerWaveImages[i].enabled = false;
    }

    Vector2 GetEffectiveRadarHalfSize()
    {
        Vector2 rectHalfSize = Vector2.one * radarRadius;
        if (radarRect != null)
        {
            Rect rect = radarRect.rect;
            rectHalfSize = rect.size * 0.5f;
        }

        return new Vector2(
            Mathf.Max(0f, Mathf.Min(radarRadius, rectHalfSize.x)),
            Mathf.Max(0f, Mathf.Min(radarRadius, rectHalfSize.y)));
    }
}
