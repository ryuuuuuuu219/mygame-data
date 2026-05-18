using System.Collections.Generic;
using UnityEngine;

public class Missile_p : MonoBehaviour
{
    [Header("基本パラメータ")]
    public Transform target;
    public float power = 50f;
    public float acceleration = 20f;
    public float maxspeed;
    public float lifeTime = 10f;
    public float turnRate = 90f; // 最大旋回速度 (deg/sec)
    public float breakAngle = 90f; // 誘導解除角度 (deg)
    public float ProportionalConstant = 3f; // 比例航法定数

    [Header("内部状態")]
    private Vector3 previousPos;
    private Vector3 currentPos;
    private Vector3 newDir;
    public float speed;

    private List<GameObject> enemys;
    private Vector3 lastDirToTarget;

    public bool isheatseeker = true;

    LineRenderer lr;
    Rigidbody rb;

    // 初期化
    public void missileInit(Vector3 startPos, Vector3 startVelocity, float lifetime = 10f)
    {
        ObjectManager.Instance?.RegisterMissile_a(gameObject);
        lifeTime = lifetime;
        target = null;

        rb ??= GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        transform.position = startPos;
        previousPos = currentPos = transform.position;
        speed = startVelocity.magnitude;

        lr = (lr == null) ? gameObject.AddComponent<LineRenderer>() : lr;
        lr.enabled = false;
        lr.positionCount = 2;

        speed = startVelocity.magnitude;
        newDir = startVelocity.normalized;

        lastDirToTarget = Vector3.zero;
        seekerDir = newDir;

        gameObject.SetActive(true);

        transform.rotation = Quaternion.LookRotation(startVelocity.normalized);

    }

    public void StatusSetting(float Power, float accel, float maxspe, float turn, float breakAng, float pConst)
    {
        power = Power;

        acceleration = accel;
        maxspeed = maxspe;

        turnRate = turn;
        breakAngle = breakAng;
        ProportionalConstant = pConst;
    }

    void FixedUpdate()
    {
        lr.enabled = true;
        lr.SetPosition(0, target != null ? target.position : currentPos + newDir * 100f);
        lr.SetPosition(1, currentPos);

        // --- 寿命 ---
        lifeTime -= Time.fixedDeltaTime;
        if (lifeTime <= 0f)
        {
            rangeover();
            return;
        }

        // --- 誘導処理 ---
        if (target != null)
        {
            Vector3 dirToTarget = (target.position - transform.position).normalized;
            float angleDiff = Vector3.Angle(newDir, dirToTarget);

            // 規定角度以上で誘導解除
            if (angleDiff > breakAngle)
            {
                target = null;
                return;
            }

            // 比例航法近似
            if (lastDirToTarget != Vector3.zero)
            {
                Vector3 LOSrate = Vector3.Cross(lastDirToTarget, dirToTarget);
                Vector3 rotAxis = LOSrate.sqrMagnitude > 0.000001f
                    ? LOSrate.normalized
                    : Vector3.Cross(newDir, dirToTarget).normalized;
                float rotMag = LOSrate.magnitude * ProportionalConstant * Mathf.Rad2Deg / Time.fixedDeltaTime;

                // 旋回速度上限
                rotMag = Mathf.Min(rotMag, turnRate);

                // 進行方向更新
                if (rotAxis.sqrMagnitude > 0.000001f)
                    newDir = Quaternion.AngleAxis(rotMag * Time.fixedDeltaTime, rotAxis) * newDir;
            }
            else
            {
                newDir = Vector3.RotateTowards(
                    newDir,
                    dirToTarget,
                    turnRate * Mathf.Deg2Rad * Time.fixedDeltaTime,
                    0f
                );
            }

            lastDirToTarget = dirToTarget;
        }

        // --- 推進 ---
        speed += acceleration * Time.fixedDeltaTime;
        speed = Mathf.Clamp(speed, 0f, maxspeed);

        previousPos = transform.position;
        transform.position += newDir * speed * Time.fixedDeltaTime;
        currentPos = transform.position;

        // --- 回転更新 ---
        if (speed > 0.001f)
            transform.rotation = Quaternion.LookRotation(newDir);

        // --- 衝突判定 ---
        RaycastHitCheck();

        detectflare(isheatseeker);
    }

    // 命中判定
    void RaycastHitCheck()
    {
        Vector3 dir = (currentPos - previousPos).normalized;
        float dist = Vector3.Distance(previousPos, currentPos);
        if (dist <= 0.0001f) return;

        float radius = GetProjectileRadius();
        RaycastHit[] hits = Physics.SphereCastAll(
            previousPos,
            radius,
            dir,
            dist,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Collide);

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null) continue;
            if (hit.collider.transform.IsChildOf(transform)) continue;

            if (hit.collider is TerrainCollider)
            {
                rangeover();
                return;
            }

            var status = hit.collider.GetComponentInParent<AugumentStatus>();
            if (status == null || !status.isEnemy) continue;

            status.damage(power); // ダメージ量は適宜調整
            if (ObjectManager.Instance != null)
                ObjectManager.Instance.hitUIflag = true;
            GeneratedAudioManager.Play(GeneratedAudioCue.Hit, hit.point, 0.65f);
            rangeover();
            return;
        }
    }

    private float GetProjectileRadius()
    {
        Collider collider = GetComponent<Collider>();
        if (collider == null)
            return Mathf.Max(0.01f, transform.lossyScale.magnitude * 0.5f);

        Vector3 extents = collider.bounds.extents;
        return Mathf.Max(0.01f, Mathf.Max(extents.x, extents.y, extents.z));
    }

    // 消滅処理
    void rangeover()
    {
        target = null;
        ObjectManager.Instance?.UnregisterMissile_a(gameObject);
        gameObject.SetActive(false);
    }

    void OnDisable()
    {
        ObjectManager.Instance?.UnregisterMissile_a(gameObject);
    }

    [Header("シーカー設定")]
    public float seekerFoV = 30f;          // シーカーの視野角
    public float flareHeatModifier = 1f;   // フレア熱補正倍率
    public Vector3 seekerDir = Vector3.forward;
    public List<DetectedObject> detectedObjects;
    void detectflare(bool enable = false)
    {
        if (!enable) return;

        seekerDir = target != null ? (target.position - transform.position).normalized : newDir;
        detectedObjects = new();

        var flares = ObjectManager.Instance.Enemies;
        foreach (var i in flares)
        {
            if (i == null) continue;

            // --- (1) 方向と角度の事前判定 ---
            Vector3 dirToFlare = (i.transform.position - transform.position).normalized;
            float angleToFlare = Vector3.Angle(seekerDir, dirToFlare);
            if (angleToFlare > seekerFoV * 0.5f) continue; // 視野外スキップ

            float distance = Vector3.Distance(transform.position, i.transform.position);

            // --- (2) flare_e（純粋なフレア）を先にチェック ---
            flare f = i.GetComponent<flare>();
            if (f != null && f.currentHeat > 0f)
            {
                float heatEffectiveness = f.currentHeat * flareHeatModifier / (distance * distance);
                detectedObjects.Add(new DetectedObject(i, heatEffectiveness));
            }

            // --- (3) AugumentStatus（エンジン熱源）をチェック ---
            var status = i.GetComponent<AugumentStatus>();
            if (status != null && status.currentHeat > 0f)
            {
                // 仮のエンジン熱源計算（スロットルに比例、距離の2乗に反比例）
                float engineHeat = status.currentHeat; // スロットル100%で100の熱源と仮定
                float heatEffectiveness = engineHeat * flareHeatModifier / (distance * distance);
                detectedObjects.Add(new DetectedObject(i, heatEffectiveness));
            }
        }

        // --- (4) 最も強い熱源をロック ---
        if (detectedObjects.Count > 0)
        {
            detectedObjects.Sort((a, b) => b.strength.CompareTo(a.strength));
            target = detectedObjects[0].obj.transform;
        }
    }

}

[System.Serializable]
public struct DetectedObject
{
    public GameObject obj;
    public float strength;
    public DetectedObject(GameObject o, float s)
    {
        obj = o;
        strength = s;
    }
}
