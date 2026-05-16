using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;
using NUnit.Framework;
using static WeaponSystem;
using System;

public class DebugHUD : MonoBehaviour
{
    [SerializeField] Camera hudCam;
    Camera mainCam;
    [Header("Canvas References")]
    [SerializeField] Canvas cameraCanvas;   // MainCamera配下（Screen Space - Camera）
    [SerializeField] Canvas overlayCanvas;  // HUD固定用（Screen Space - Overlay）

    [Header("Player & HUD")]
    public GameObject plane;         // プレイヤー機のスクリプト参照
    public TextMeshProUGUI hudText;          // HUDテキスト
    public RectTransform velocityMarker;     // フライトパスベクトル（進行方向）
    public RectTransform noseMarker;         // ウィスキーピーク（機首方向）

    [Header("Target Settings")]
    public float detectRange = 3000f;        // 探索範囲
    public float lockRange = 850f;           // ロック範囲
    public float SlockRange = 850f;           // ロック範囲
    public float LlockRange = 850f;           // ロック範囲
    public float gunRange = 500f;            // 機銃射程
    public float maxfov = 60f;               // 視界

    [Header("Target Locator")]
    public RectTransform targetLocator;      // ターゲットロケーターUI
    public float edgeOffset = 50f;           // 画面端からのオフセット

    private WeaponSystem weapon;
    private Rigidbody rb;

    private List<GameObject> arrys;
    private List<GameObject> targets;       // 敵機リスト
    public int LockedFrame = 1;             // ロック維持フレーム数
    public List<GameObject> detecttargets;  //コンテナ表示ターゲット
    public List<GameObject> markingtargets; //ターゲット切り替え用配列
    private List<float> targetsfovs;
    public List<GameObject> Lockedtargets;  //ロック条件を満たすターゲット配列

    List<(GameObject target, float fov)> detectPairs;

    bool isBlinking;
    float blinkInterval = 0.4f;
    float blinkTimer = 0f;

    AircraftController ac; 

    AugumentStatus status;
    EnemyNameConverterToUI enemyNameConverter;

    List<(GameObject obj, LineRenderer lr, GameObject boundTarget)> conteiners;


    void Start()
    {
        if (hudText != null && overlayCanvas != null)
        {
            hudText.transform.SetParent(overlayCanvas.transform, false);
        }
        if (targetLocator != null && overlayCanvas != null)
        {
            targetLocator.SetParent(overlayCanvas.transform, false);
        }
        if (velocityMarker != null && overlayCanvas != null)
        {
            velocityMarker.SetParent(overlayCanvas.transform, false);
        }
        if (noseMarker != null && overlayCanvas != null)
        {
            noseMarker.SetParent(overlayCanvas.transform, false);
        }

        if (plane != null)
        {
            rb = plane.GetComponent<Rigidbody>();
            weapon = plane.GetComponent<WeaponSystem>();
            ac = plane.GetComponent<PlayerAircraft>();
        }

        mainCam = Camera.main;

        if (cameraCanvas != null && cameraCanvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            cameraCanvas.worldCamera = mainCam;
        }

        conteiners = new ();
        detecttargets = new ();
        markingtargets.Clear();
        Lockedtargets = new ();
        targetsfovs = new ();
        detectPairs = new ();

        status = plane.GetComponent<AugumentStatus>();
        enemyNameConverter = GetComponent<EnemyNameConverterToUI>();
        if (enemyNameConverter == null)
        {
            enemyNameConverter = FindFirstObjectByType<EnemyNameConverterToUI>();
        }

        if (status.IsInitialized)
        {
            InitFromStatus();
        }
        else
        {
            status.OnInitialized += InitFromStatus;
        }
    }

    void InitFromStatus()
    {

        status.altGetVar("ミサイル：射程（ロック可能距離）", out SlockRange);
        status.altGetVar("長射程マルチロックミサイル：射程（ロック可能距離）", out LlockRange);
    }

    void LateUpdate()
    {
        targets = ObjectManager.Instance.Enemies as List<GameObject>;
        arrys = ObjectManager.Instance.allies;

        // -------- 目標探索 --------
        detecttargets.Clear();

        bool stdm = weapon.mode == WeaponMode.MSL;

        lockRange = stdm ? SlockRange : LlockRange;

        if (targets.Count > 1)
        {
            targetsfovs.Clear();

            detectPairs.Clear();

            foreach (var t in targets)
            {
                if (t == null) continue; 
                if (t.TryGetComponent(out AugumentStatus s))
                {
                    if (!s.isVisible) continue; //フラグを確認
                }
                float fov = ToTargetFov(t.transform.position);
                float sqrdist = (plane.transform.position - t.transform.position).sqrMagnitude;
                if (sqrdist > detectRange * detectRange) continue;
                detectPairs.Add((t, fov));
            }

            detectPairs.Sort((a, b) => a.fov.CompareTo(b.fov)); // 昇順

            detecttargets = detectPairs.Select(p => p.target).ToList();
            //targetsfovs = detectPairs.Select(p => p.fov).ToList();

            var input=InputManager.Instance;

            //ターゲット切り替えボタン押下時
            if (input.targetChange)
            {
                if (detecttargets.Count >= 1)
                {
                    if (markingtargets.Count >= 1)
                    {
                        if (markingtargets[0] == detecttargets[0])
                        {
                            //優先ロック中の目標を保持しつつマーキング配列を更新
                            if (detecttargets.Count >= 2)
                            {
                                GameObject target0 = markingtargets[0];
                                GameObject target1 = detecttargets[1];
                                markingtargets.Clear();
                                markingtargets.Add(target1);
                                markingtargets.Add(target0);
                                foreach (var t in detecttargets)
                                {
                                    if (!markingtargets.Contains(t))
                                    {
                                        markingtargets.Add(t);
                                    }
                                }
                            }
                            else
                            {
                                GameObject target0 = markingtargets[0];
                                markingtargets.Clear();
                                markingtargets.Add(target0);
                            }
                        }
                        else
                        {
                            GameObject target0 = markingtargets[0];
                            GameObject target1 = detecttargets[0];
                            markingtargets.Clear();
                            markingtargets.Add(target1);
                            markingtargets.Add(target0);
                            foreach (var t in detecttargets)
                            {
                                if (!markingtargets.Contains(t))
                                {
                                    markingtargets.Add(t);
                                }
                            }
                        }
                    }
                    else
                    {
                        markingtargets.Clear();
                        markingtargets.AddRange(detecttargets);
                    }
                }
            }
            else
            {
                if (markingtargets.Count >= 1)
                {
                    GameObject target0 = markingtargets[0];
                    markingtargets.Clear();
                    markingtargets.Add(target0);
                    foreach (var t in detecttargets)
                    {
                        if (!markingtargets.Contains(t))
                        {
                            markingtargets.Add(t);
                        }
                    }
                }
                else
                {
                    markingtargets.Clear();
                    markingtargets.AddRange(detecttargets);
                }
            }
            //優先目標消滅時
            if(markingtargets.Count == 0)
            {
                markingtargets.AddRange(detecttargets);
            }
            else if (markingtargets.Count > 0)
            {
                if (markingtargets[0] == null)
                {
                    markingtargets.Remove(markingtargets[0]);
                }
            }

            //優先目標までの距離が遠い場合はマーキング目標を更新
            if(markingtargets.Count > 0)
            {
                float sqrdist = (plane.transform.position - markingtargets[0].transform.position).sqrMagnitude;
                if (sqrdist > detectRange * detectRange)
                {
                    markingtargets.Clear();
                    markingtargets.AddRange(detecttargets);
                }
            }

            //マーキング目標が視野外なら配列から削除
            for (int i = markingtargets.Count - 1; i >= 1/*優先目標は除く*/; i--)
            {
                var t = markingtargets[i];
                float fov = ToTargetFov(t.transform.position);
                if (fov > maxfov)
                {
                    markingtargets.Remove(t);
                }
            }
        }
        else if (targets.Count == 1)
        {
            markingtargets.Clear();
            markingtargets.Add(targets[0]);
        }
        else
        {
            markingtargets.Clear();
        }


        // ロック条件
        if (markingtargets.Count > 0)
        {
            if (markingtargets[0] == null)
            {
                markingtargets.Clear();
                Lockedtargets.Clear();
                goto skip;

            }
            float sqrdist = (plane.transform.position - markingtargets[0].transform.position).sqrMagnitude;
            if (sqrdist < lockRange * lockRange &&
                ToTargetFov(markingtargets[0].transform.position) < maxfov)
            {
                Lockedtargets.Clear();
                for (int i = 0; i < markingtargets.Count; i++)
                {
                    if (markingtargets[i] == null) continue;

                    float sqrdisti = (plane.transform.position - markingtargets[i].transform.position).sqrMagnitude;
                    if (sqrdisti < lockRange * lockRange &&
                    ToTargetFov(markingtargets[i].transform.position) < maxfov)
                    {
                        if (!Lockedtargets.Contains(markingtargets[i]))
                        {
                            Lockedtargets.Add(markingtargets[i]);
                        }
                    }
                    else
                    {
                        if (Lockedtargets.Contains(markingtargets[i]))
                        {
                            Lockedtargets.Remove(markingtargets[i]);
                        }
                    }
                }
            }
            else
            {
                Lockedtargets.Clear();
            }

            //保険　マーキング目標配列とロック目標配列の同期
            if (Lockedtargets.Count > 0)
            {
                for (int i = Lockedtargets.Count - 1; i >= 0; i--)
                {
                    var t = Lockedtargets[i];
                    if (t == null) continue;
                    if (!markingtargets.Contains(t))
                    {
                        Lockedtargets.Remove(t);
                    }
                }

                if (Lockedtargets.Count > LockedFrame)
                {
                    Lockedtargets.RemoveRange(LockedFrame, Lockedtargets.Count - LockedFrame);
                }
            }

            // ロック解除条件
            if (Lockedtargets.Count == 0)
            {
            }
        }
        else if (markingtargets.Count == 0)
        {
        }

        // -------- コンテナ更新 --------
        blinkTimer += Time.deltaTime;
        if (blinkTimer >= blinkInterval)
        {
            blinkTimer = 0f;
            isBlinking = !isBlinking;
        }
        UpdateContainers();

        // -------- HUD表示 --------
        UpdateHUD();

        // -------- ターゲットロケーター --------
        UpdateTargetLocator();

    skip:

        // -------- フライトパスベクター --------
        if (velocityMarker != null && rb != null)
        {
            Vector3 worldPos = plane.transform.position + rb.linearVelocity.normalized * 100f;
            velocityMarker.position = mainCam.WorldToScreenPoint(worldPos);
        }

        // -------- 機首方向（ウィスキーピーク） --------
        if (noseMarker != null)
        {
            Vector3 noseWorld = plane.transform.position + plane.transform.forward * 100f;
            noseMarker.position = mainCam.WorldToScreenPoint(noseWorld);
        }
    }

    #region 座標系変換関数 

    float ToTargetFov(Vector3 worldPos)
    {
        if (rb == null) return -1f;

        Vector3 forward = rb.transform.forward;
        Vector3 dirToTarget = (worldPos - plane.transform.position).normalized;

        // 0〜180°の角度をそのまま返す
        float angle = Vector3.Angle(forward, dirToTarget);
        return angle; // ← 0なら正面、180なら真後ろ
    }

    float GetTargetAngle(Transform target, Camera cam, out bool isOutsideView)
    {
        if (target == null || cam == null)
        {
            isOutsideView = false;
            return 0f;
        }

        Vector3 toTarget = (target.position - cam.transform.position).normalized;
        Vector3 camForward = cam.transform.forward;

        // カメラ前方との角度（0° = 正面, 180° = 真後ろ）
        float angleFromCenter = Vector3.Angle(camForward, toTarget);

        // FOVの半分以内なら視野内、それ以外は視野外
        float halfFOV = cam.fieldOfView * 0.5f;
        isOutsideView = angleFromCenter >= halfFOV;

        return angleFromCenter;
    }

    #endregion
    #region コンテナ表示

    void UpdateTargetLocator()
    {
        if (markingtargets.Count == 0 || targetLocator == null || hudCam == null)
        {
            targetLocator?.gameObject.SetActive(false);
            return;
        }

        // 角度計算と視野判定を共通関数で取得
        float angleFromCenter = GetTargetAngle(markingtargets[0].transform, mainCam, out bool outsideView);

        // 視野内なら非表示
        if (!outsideView)
        {
            targetLocator.gameObject.SetActive(false);
            return;
        }

        // スクリーン座標
        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
        Vector3 screenPos = mainCam.WorldToScreenPoint(markingtargets[0].transform.position);

        // スクリーン中心からの方向
        Vector3 dir = (new Vector3(screenPos.x, screenPos.y, 0f) - screenCenter).normalized;

        if (screenPos.z < 0)
        {
            // 後方の場合は方向を反転
            dir = -dir;
        }

        // スクリーン端の位置
        Vector3 edgePos = screenCenter + dir * (Mathf.Min(Screen.width, Screen.height) / 2f - edgeOffset);

        // 角度に比例して細長く
        float stretch = 1f + (angleFromCenter / 90f);

        // 矢印を表示・変形
        targetLocator.gameObject.SetActive(true);
        targetLocator.position = edgePos;
        targetLocator.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);
        targetLocator.localScale = new Vector3(1f, stretch, 1f);
    }

    Vector3[] vertexs = new Vector3[]
    {
        new Vector3() { x = -1, y = -1, z = 0 },
        new Vector3() { x = 1, y = -1, z = 0 },
        new Vector3() { x = 1, y = 1, z = 0 },
        new Vector3() { x = -1, y = 1, z = 0 }
    };


    void UpdateContainers()
    {
        int needed = targets.Count + arrys.Count;

        // 足りない分は生成
        while (conteiners.Count < needed)
        {

            #region 情報
            GameObject c = new GameObject("TargetInfo", typeof(RectTransform));
            c.transform.SetParent(cameraCanvas.transform, false);
            c.tag = "HUDUI";
            c.layer = LayerMask.NameToLayer("UI");

            GameObject distanceTextObj = new GameObject("DistanceText");
            distanceTextObj.transform.SetParent(c.transform);
            distanceTextObj.layer = LayerMask.NameToLayer("UI");
            var rectT = distanceTextObj.AddComponent<RectTransform>();
            rectT.anchorMin = new Vector2(1f, 0.5f);
            rectT.anchorMax = new Vector2(1f, 0.5f);
            rectT.anchoredPosition = new Vector3(178f, 0f, 0);
            rectT.sizeDelta = new Vector2(400f, 150f);
            rectT.localPosition = new Vector3(rectT.localPosition.x, rectT.localPosition.y, 0);
            rectT.localScale = Vector3.one;

            var textobj = distanceTextObj.AddComponent<TextMeshProUGUI>();
            textobj.fontSize = 16;
            textobj.alignment = TextAlignmentOptions.Left;
            textobj.color = Color.green;

            #endregion
            #region TGT表示
            GameObject tgtText = new GameObject("tgtText");
            tgtText.transform.SetParent(c.transform);
            tgtText.layer = LayerMask.NameToLayer("UI");
            var tgtRectT = tgtText.AddComponent<RectTransform>();
            tgtRectT.anchorMin = new Vector2(0, 0.5f);
            tgtRectT.anchorMax = new Vector2(0, 0.5f);
            tgtRectT.anchoredPosition = new Vector3(57f, 20f, 0);
            tgtRectT.sizeDelta = new Vector2(130f, 50f);
            tgtRectT.localPosition = new Vector3(tgtRectT.localPosition.x, tgtRectT.localPosition.y, 0);
            tgtRectT.localScale = Vector3.one;

            var tgttextobj = tgtText.AddComponent<TextMeshProUGUI>();
            tgttextobj.fontSize = 16;
            tgttextobj.alignment = TextAlignmentOptions.Left;
            tgttextobj.color = Color.red;
            #endregion

            GameObject l = new GameObject("TargetContainerline");
            l.transform.SetParent(null);
            LineRenderer renderer = l.AddComponent<LineRenderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.startWidth = 0.3f;
            renderer.endWidth = 0.3f;
            renderer.enabled = false;
            renderer.loop = true;
            renderer.positionCount = vertexs.Length;
            renderer.SetPositions(vertexs);
            renderer.startColor = Color.green;
            renderer.endColor = Color.green;

            conteiners.Add((c, renderer, null));
        }

        // 必要な分だけアクティブ化して位置更新
        int idx = 0;
        var conteninerobj = conteiners[0].obj;

        if(markingtargets.Count == 0)
        {
            ClearContainer(0);
            return;
        }

        foreach (var obj in targets)
        {
            if (obj == null)
            {
                ClearContainer(idx);
                idx++;
                continue;
            }

            if (obj.TryGetComponent(out AugumentStatus s))
            {
                if (!s.isVisible)
                {
                    ClearContainer(idx);
                    idx++;
                    continue;
                }
            }

            var entry = conteiners[idx];

            if (entry.boundTarget != obj)
            {
                ClearContainer(idx);
                entry.boundTarget = obj;
                conteiners[idx] = entry;
            }


            conteninerobj = conteiners[idx].obj;
            bool isLocked = Lockedtargets.Contains(obj);
            int targetidx = -1;
            if (obj ==markingtargets[0])
            {
                targetidx = 0;
            }
            else if (markingtargets.Count > 1 && obj == markingtargets[1])
            {
                targetidx = 1;
            }

            int nextidx = markingtargets.Count > 0 ? 1 : 0;
            bool isNext = (targetidx == nextidx);

            if (obj == null)
            {
                ClearContainer(idx);
                continue;
            }

            if (targetidx == 0)
            {
                string hptext = "";
                if (obj.TryGetComponent(out AugumentStatus status) &&
                    status.TryGetHP(out float hp, out float max))
                {
                    hptext = $"HP:{hp:F0}/{max:F0}\n";
                }
                else
                {
                    Debug.LogError("DebugHUD: Target AugumentStatus or HP not found.");
                }
                    UpdateContainer(idx, obj,
                        isLocked ? Color.red : Color.green,
                        ConvertEnemyName(obj) + "\n" +
                        $"{Vector3.Distance(plane.transform.position, obj.transform.position):F1}m" + "\n" +
                        hptext);

                if (!isLocked)
                {
                    if (!isBlinking)
                    {
                        conteiners[idx].lr.enabled = false;
                    }
                }
                else
                {
                    conteiners[idx].lr.enabled = true;
                }

            }
            else
            {
                UpdateContainer(idx, obj,
                    isLocked ? Color.red : Color.green,
                    (isNext ? "Next" : ""));

            }
            idx++;
        }
        foreach (var obj in arrys)
        {
            conteninerobj = conteiners[idx].obj;
            if (obj == null)
            {
                ClearContainer(idx);
                continue;
            }
            if (obj.name == "Player") continue;
            UpdateContainer(idx, obj, Color.cyan, "Arry");
            idx++;
        }

        // 余った分は非表示
        for (int i = idx; i < conteiners.Count; i++)
        {
            ClearContainer(i);
        }
    }
    void ClearContainer(int idx)
    {
        var container = conteiners[idx].obj;

        var d = container.transform.Find("DistanceText");
        if (d && d.TryGetComponent(out TextMeshProUGUI tm))
            tm.text = "";

        var t = container.transform.Find("tgtText");
        if (t && t.TryGetComponent(out TextMeshProUGUI tm2))
            tm2.text = "";

        conteiners[idx].lr.enabled = false;
    }


    void UpdateContainer(int idx, GameObject target, Color color, string text)
    {
        var container = conteiners[idx].obj;
        var renderer = conteiners[idx].lr;
        var containerRect = container.GetComponent<RectTransform>();
        if (renderer == null || containerRect == null) 
        { 
            Debug.LogError("DebugHUD: Missing components in container.");
            return; 
        }

        Vector3 viewportPos = mainCam.WorldToViewportPoint(target.transform.position);
        Vector3 screenPos = mainCam.ViewportToScreenPoint(viewportPos);
        float dist = Vector3.Distance(plane.transform.position, target.transform.position);

        if (dist > detectRange || !IsValidViewportPoint(viewportPos))
        {
            renderer.enabled = false;
            ClearTexts(container);
            return;
        }

        // ===== UI（RectTransform）=====
        RectTransform canvasRect = cameraCanvas.GetComponent<RectTransform>();

        Camera uiCamera = cameraCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : cameraCanvas.worldCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPos,
            uiCamera,
            out Vector2 localPos
        ))
        {
            renderer.enabled = false;
            ClearTexts(container);
            return;
        }

        containerRect.localPosition = localPos;

        // ===== LineRenderer（ワールド）=====
        Vector3 dir = (target.transform.position - mainCam.transform.position).normalized;
        Vector3 basePos = mainCam.transform.position + dir * 100f;

        Vector3 baseScreen = mainCam.WorldToScreenPoint(basePos);

        for (int i = 0; i < vertexs.Length; i++)
        {
            Vector3 worldPos = mainCam.ScreenToWorldPoint(
                new Vector3(
                    baseScreen.x + vertexs[i].x * 20f,
                    baseScreen.y + vertexs[i].y * 20f,
                    baseScreen.z));
            renderer.SetPosition(i, worldPos);
        }

        renderer.enabled = true;
        renderer.startColor = color;
        renderer.endColor = color;

        // ===== Text =====
        SetTexts(container, target, text);
    }

    string ConvertEnemyName(GameObject obj)
    {
        if (enemyNameConverter == null)
        {
            enemyNameConverter = FindFirstObjectByType<EnemyNameConverterToUI>();
        }

        return enemyNameConverter != null ? enemyNameConverter.converter(obj) : obj.name;
    }

    bool IsValidViewportPoint(Vector3 viewportPos)
    {
        if (float.IsNaN(viewportPos.x) ||
            float.IsNaN(viewportPos.y) ||
            float.IsNaN(viewportPos.z) ||
            float.IsInfinity(viewportPos.x) ||
            float.IsInfinity(viewportPos.y) ||
            float.IsInfinity(viewportPos.z))
        {
            return false;
        }

        return viewportPos.z > mainCam.nearClipPlane &&
               viewportPos.x >= 0f &&
               viewportPos.x <= 1f &&
               viewportPos.y >= 0f &&
               viewportPos.y <= 1f;
    }

    void ClearTexts(GameObject container)
    {
        if (container.transform.Find("DistanceText")?.TryGetComponent(out TextMeshProUGUI tm) == true)
            tm.text = "";
        if (container.transform.Find("tgtText")?.TryGetComponent(out TextMeshProUGUI tm2) == true)
            tm2.text = "";
    }

    void SetTexts(GameObject container, GameObject target, string text)
    {
        var obj1 = container.transform.Find("DistanceText");
        if (obj1?.TryGetComponent(out TextMeshProUGUI tm) == true)
        { 
            tm.text = text;
            Debug.Log(target.name + ":" + obj1.localRotation);
            obj1.localRotation = Quaternion.identity;
        }

        var obj2 = container.transform.Find("tgtText");
        if (obj2?.TryGetComponent(out TextMeshProUGUI tm2) == true)
        { 
            tm2.text = target.GetComponent<AugumentStatus>().missionObjective ? "TGT" : "";
            obj2.localRotation = Quaternion.identity;
        }
    }
    void UpdateHUD()
    {
        if (rb == null || hudText == null) return;

        float speed = rb.linearVelocity.magnitude;
        float altitude = plane.transform.position.y;
        float pitch = plane.transform.eulerAngles.x;
        float roll = plane.transform.eulerAngles.z;
        float thr = ac.throttle;

        hudText.text =
            $"SPD: {speed:F1} m/s\n" +
            $"ALT: {altitude:F1} m\n" +
            $"THR: {thr:F2}\n" +
            $"PITCH: {pitch:F1}°\n" +
            $"ROLL: {roll:F1}°";
    }
    #endregion
}
