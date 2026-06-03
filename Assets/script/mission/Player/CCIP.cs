using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;
using NUnit.Framework;
using static WeaponSystem;

public class CCIP : MonoBehaviour
{
    const int HudSortingOrder = short.MaxValue;
    [SerializeField] Camera hudCam;
    Camera mainCam;
    [SerializeField] DebugHUD debugHUD;

    public RectTransform gunMarker;          // ガンリードマーカー

    [Header("Player")]
    public GameObject plane;

    [Header("CCIP")]
    public bool showCCIP = true;
    public GameObject ccipLine;
    public GameObject linkLine;
    public LayerMask groundMask;
    public float ccipSimTime = 30f;
    public float ccipDt = 0.8f; 
    public int ccipMaxPoints = 40;

    int cutFrames = 3; // 「弾道計算をカットするフレーム数」

    float ccipTimer;
    const float CCIP_INTERVAL = 1/24f;

    [Header("Bomb")]
    public float bombDamageRadius = 50f; // meters

    List<GameObject> leftlines,rightlines;

    private WeaponSystem weapon;
    private Rigidbody rb;

    AugumentStatus status;

    GameObject tgt;
    Material hudLineMaterial;

    void Start()
    {
        mainCam = Camera.main;

        weapon = plane.GetComponent<WeaponSystem>();
        rb = plane.GetComponent<Rigidbody>();

        leftlines = new List<GameObject>();
        rightlines = new List<GameObject>();

        ConfigureHudLine(ccipLine);
        ConfigureHudLine(linkLine);

        for (int i = 0; i < ccipMaxPoints; i++)
        {

            var left = Instantiate(ccipLine, transform);
            ConfigureHudLine(left);
            left.SetActive(false);
            leftlines.Add(left);

            var right = Instantiate(ccipLine, transform);
            ConfigureHudLine(right);
            right.SetActive(false);
            rightlines.Add(right);

        }

        status = plane.GetComponent<AugumentStatus>();
        if (status.IsInitialized)
        {
        }
        else
        {
        }
    }

    void ConfigureHudLine(GameObject lineObject)
    {
        if (lineObject == null) return;
        if (!lineObject.TryGetComponent(out LineRenderer line)) return;

        line.material = GetHudLineMaterial();
        line.sortingOrder = HudSortingOrder;
    }

    Material GetHudLineMaterial()
    {
        if (hudLineMaterial != null)
            return hudLineMaterial;

        Shader shader = Shader.Find("HUD/AlwaysOnTop");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        hudLineMaterial = new Material(shader);
        hudLineMaterial.renderQueue = 5000;
        return hudLineMaterial;
    }

    void LateUpdate()
    {
        showCCIP = weapon.mode == WeaponMode.UGB;

        if (debugHUD.Lockedtargets.Count > 0)
        { 
            tgt = debugHUD.Lockedtargets[0]; 
        }

        // -------- ガンリード更新 --------
        if (gunMarker != null && weapon != null && tgt != null)
        {
            if (Vector3.Distance(plane.transform.position, tgt.transform.position) < debugHUD.gunRange)
            {
                gunMarker.gameObject.SetActive(true);
                UpdateGunReticle();
            }
            else
            {
                gunMarker.gameObject.SetActive(false);
            }
        }
        else
        {
            gunMarker.gameObject.SetActive(false);
        }

        ccipTimer += Time.deltaTime;
        if (ccipTimer >= CCIP_INTERVAL)
        {
            ccipTimer = 0f;
            if (showCCIP)
            {
                UpdateCCIP();
            }
            else
            {
                ccipLine.GetComponent<LineRenderer>().enabled = false;
                linkLine.GetComponent<LineRenderer>().enabled = false;
                foreach (var line in leftlines)
                {
                    line.SetActive(false);
                }
                foreach (var line in rightlines)
                {
                    line.SetActive(false);
                }
            }
        }
    }

    #region CCIP関連関数

    void UpdateCCIP()
    {
        LineRenderer line = ccipLine.GetComponent<LineRenderer>();

        if (ccipLine == null) return;

        Vector3 startPos =
            plane.transform.position
          + plane.transform.forward * 2f
          + -plane.transform.up * 1.5f;

        Vector3 startVel =
            rb.linearVelocity;

        Vector3? targetPos =
            (tgt != null)
            ? tgt.transform.position
            : null;

        var result = CCIPBallisticPredictor.Predict(
            startPos,
            startVel,
            ccipSimTime,
            ccipDt,
            groundMask,
            targetPos
        );

        // 表示する点数を制限（重要）
        int maxPoints = Mathf.Min(result.trajectory.Count, ccipMaxPoints);

        line.positionCount = maxPoints;

        int j = 0;
        for (int i = 0; i < maxPoints * cutFrames; i++)
        {

            if (i % cutFrames != 0)
            {
                continue;
            }
            if (j >= leftlines.Count) break; 
            
            Vector3 d = result.trajectory[i] - plane.transform.position;
            Vector3 v = rb.linearVelocity;

            if (v.sqrMagnitude < 0.01f) continue; // 低速保険

            Vector3 vDir = v.normalized;
            float forwardDist = Vector3.Dot(d, vDir);

            // 「弾道計算をカットするフレーム数」
            float minForwardDist =
                rb.linearVelocity.magnitude * ccipDt * cutFrames;

            if (forwardDist < minForwardDist)
            {
                continue; // 自機近傍・後方は描かない
            }

            Vector3 locallineendPos = result.trajectory[i]; 

            Vector3 horizontalRight =
                Vector3.Cross(Vector3.up, hudCam.transform.forward).normalized;

            Vector3 offset1 = horizontalRight * (5f - (i * 4f / (maxPoints * cutFrames)));
            Vector3 offset2 = horizontalRight * 10f;

            //左
            Drowlines(leftlines[j], false, locallineendPos, offset1, offset2);

            //右
            Drowlines(rightlines[j], true, locallineendPos, offset1, offset2);

            j++;

        }

        // ティックは j 本だけ
        for (int i = 0; i < leftlines.Count; i++)
        {
            bool active = i < j;
            leftlines[i].SetActive(active);
            rightlines[i].SetActive(active);
        }

        line.enabled = true;

        if (tgt != null && result.targetAltitudeHit.HasValue)
        {
            var link = linkLine.GetComponent<LineRenderer>();

            Vector3 hitPos = result.targetAltitudeHit.Value;

            float dist = Vector3.Distance(hitPos, (Vector3)targetPos);

            link.positionCount = 2;
            link.SetPosition(0, hitPos);
            link.SetPosition(1, (Vector3)targetPos);

            // --- 色判定 ---
            if (dist <= bombDamageRadius)
            {
                link.startColor = Color.red;
                link.endColor = Color.red;
            }
            else
            {
                link.startColor = Color.green;
                link.endColor = Color.green;
            }

            link.enabled = true;
        }
        else
        {
            linkLine.GetComponent<LineRenderer>().enabled = false;
        }
    }

    void Drowlines(GameObject lineObj, bool isright, Vector3 centor, Vector3 p1, Vector3 p2)
    {
        lineObj.SetActive(true);
        var lr = lineObj.GetComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.GetComponent<LineRenderer>().SetPosition(0, centor + p1 * (isright ? 1 : -1));//左
        lr.GetComponent<LineRenderer>().SetPosition(1, centor + p2 * (isright ? 1 : -1));
    }

    #endregion
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
    // ガンリード計算（軽量反復）
    void UpdateGunReticle()
    {
        if (rb == null || tgt == null || weapon == null) return;

        var targetStatus = tgt.GetComponent<AugumentStatus>();
        if (targetStatus == null)
        {
            gunMarker.gameObject.SetActive(false);
            return;
        }

        float bulletSpeed = weapon.bulletSpeed;
        if (bulletSpeed <= 0f)
        {
            gunMarker.gameObject.SetActive(false);
            return;
        }

        Transform muzzle = weapon.gunMuzzle != null ? weapon.gunMuzzle : plane.transform;
        Vector3 muzzlePos = muzzle.position;
        Vector3 targetVelocity = GetTargetVelocity(tgt, targetStatus);

        if (!TryPredictGunAimPoint(
            muzzlePos,
            rb.linearVelocity,
            tgt.transform.position,
            targetVelocity,
            bulletSpeed,
            out Vector3 aimPoint))
        {
            gunMarker.gameObject.SetActive(false);
            return;
        }

        Vector3 screenLead = mainCam.WorldToScreenPoint(aimPoint);
        gunMarker.position = screenLead;
        gunMarker.gameObject.SetActive(screenLead.z > 0);
    }

    Vector3 GetTargetVelocity(GameObject target, AugumentStatus targetStatus)
    {
        Rigidbody targetRb = target.GetComponent<Rigidbody>();
        if (targetRb != null)
            return targetRb.linearVelocity;

        return targetStatus.Velocity / Mathf.Max(Time.deltaTime, 0.0001f);
    }

    bool TryPredictGunAimPoint(
        Vector3 muzzlePos,
        Vector3 shooterVelocity,
        Vector3 targetPos,
        Vector3 targetVel,
        float bulletSpeed,
        out Vector3 aimPoint)
    {
        Vector3 toTarget = targetPos - muzzlePos;
        Vector3 relativeVelocity = targetVel - shooterVelocity;

        float a = Vector3.Dot(relativeVelocity, relativeVelocity) - bulletSpeed * bulletSpeed;
        float b = 2f * Vector3.Dot(toTarget, relativeVelocity);
        float c = Vector3.Dot(toTarget, toTarget);

        if (!TrySolvePositiveInterceptTime(a, b, c, out float t))
        {
            aimPoint = Vector3.zero;
            return false;
        }

        aimPoint = targetPos + targetVel * t;
        return true;
    }

    bool TrySolvePositiveInterceptTime(float a, float b, float c, out float t)
    {
        const float epsilon = 0.0001f;
        t = 0f;

        if (Mathf.Abs(a) < epsilon)
        {
            if (Mathf.Abs(b) < epsilon) return false;

            t = -c / b;
            return t > epsilon;
        }

        float discriminant = b * b - 4f * a * c;
        if (discriminant < 0f) return false;

        float sqrt = Mathf.Sqrt(discriminant);
        float t1 = (-b - sqrt) / (2f * a);
        float t2 = (-b + sqrt) / (2f * a);

        bool t1ok = t1 > epsilon;
        bool t2ok = t2 > epsilon;
        if (!t1ok && !t2ok) return false;

        t = t1ok && t2ok ? Mathf.Min(t1, t2) : (t1ok ? t1 : t2);
        return true;
    }

}

public class CCIPBallisticPredictor
{
    public struct Result
    {
        public List<Vector3> trajectory;
        public Vector3? groundHit;
        public Vector3? targetAltitudeHit;
    }

    public static Result Predict(
        Vector3 startPos,
        Vector3 startVel,
        float simTime,
        float dt,
        LayerMask groundMask,
        Vector3? targetPos)
    {
        var r = new Result
        {
            trajectory = new List<Vector3>()
        };

        Vector3 p = startPos;
        Vector3 v = startVel;

        bool hasTarget = targetPos.HasValue;

        // --- 解析解で「目標高度交点」を先に計算 ---
        if (hasTarget)
        {
            float targetY = targetPos.Value.y;

            if (SolveTargetAltitude(startPos.y, startVel.y, targetY, out float tHit))
            {
                // simTime外なら「表示なし」にしたいならここで弾く
                if (tHit <= simTime)
                {
                    Vector3 hit =
                        startPos
                      + startVel * tHit
                      + 0.5f * Physics.gravity * tHit * tHit;

                    r.targetAltitudeHit = hit;
                }
            }
        }

        r.trajectory.Add(p);

        for (float t = 0; t < simTime; t += dt)
        {
            Vector3 prev = p;

            v += Physics.gravity * dt;
            p += v * dt;

            r.trajectory.Add(p);
        }

        return r;
    }

    static bool SolveTargetAltitude(
    float y0,
    float v0y,
    float targetY,
    out float tHit)
    {
        float a = 0.5f * Physics.gravity.y;
        float b = v0y;
        float c = y0 - targetY;

        float D = b * b - 4f * a * c;

        tHit = 0f;
        if (D < 0f) return false;

        float sqrtD = Mathf.Sqrt(D);
        float t1 = (-b - sqrtD) / (2f * a);
        float t2 = (-b + sqrtD) / (2f * a);

        // 正の時間のみ
        bool t1ok = t1 > 0f;
        bool t2ok = t2 > 0f;

        if (!t1ok && !t2ok) return false;

        // 後半（大きい方）を採用
        tHit = (t1ok && t2ok) ? Mathf.Max(t1, t2)
                              : (t1ok ? t1 : t2);
        return true;
    }
}
