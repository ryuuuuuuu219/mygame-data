using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class Missile : MonoBehaviour
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
    public float turnRateDecay = 1f; // 誘導力減衰率 (deg/sec/sec)
    public float totalDeltaTheta = 90f; // 累積誘導旋回角度上限 (deg)
    public Vector3 launchDirectionOverride;
    public float guidanceStartDelay;
    public bool guidanceStartSwitch;
    public bool usePurePursuitAssistForLaunchOverride = true;
    public float initialTurnRate = 95f;
    public float initialTurnBreakAngle = 15f;
    public float effectRadius = 0f;

    [Header("内部状態")]
    private Vector3 previousPos;
    private Vector3 currentPos;
    private Vector3 newDir;
    public Vector3 velocity;
    public float speed;

    private List<GameObject> allies;
    private Vector3 lastDirToTarget;
    private float currentTurnRate;
    private float usedDeltaTheta;
    private bool useInitialPurePursuit;

    public bool isheatseeker = true;

    // 初期化
    public void missileInit(Vector3 startPos, Vector3 startVelocity, float lifetime = 10f)
    {
        ObjectManager.Instance?.RegisterMissile_e(gameObject);
        lifeTime = lifetime;
        target = null;

        velocity = startVelocity;
        if (launchDirectionOverride.sqrMagnitude > 0.001f)
            velocity = launchDirectionOverride.normalized * startVelocity.magnitude;

        transform.position = startPos;
        transform.rotation = Quaternion.LookRotation(velocity.normalized);

        previousPos = currentPos = transform.position;
        speed = startVelocity.magnitude;

        newDir = velocity.normalized;
        lastDirToTarget = Vector3.zero;
        currentTurnRate = turnRate;
        usedDeltaTheta = 0f;
        useInitialPurePursuit = ShouldUsePurePursuitAssist();
        gameObject.SetActive(true);
    }

    public void StatusSetting(float Power, float accel, float maxspe, float turn, float breakAng, float pConst)
    {
        StatusSetting(Power, accel, maxspe, turn, breakAng, pConst, turnRateDecay, totalDeltaTheta);
    }

    public void StatusSetting(float Power, float accel, float maxspe, float turn, float breakAng, float pConst, float turnDecay, float totalTheta)
    {
        power = Power;

        acceleration = accel;
        maxspeed = maxspe;

        turnRate = turn;
        breakAngle = breakAng;
        ProportionalConstant = pConst;
        turnRateDecay = turnDecay;
        totalDeltaTheta = totalTheta;
        currentTurnRate = turnRate;
        usedDeltaTheta = 0f;
        useInitialPurePursuit = ShouldUsePurePursuitAssist();
    }

    void FixedUpdate()
    {
        // --- 寿命 ---
        lifeTime -= Time.fixedDeltaTime;
        if (lifeTime <= 0f)
        {
            rangeover();
            return;
        }

        if (newDir == Vector3.zero)
            newDir = velocity.normalized;

        bool manualGuidanceStart = float.IsInfinity(guidanceStartDelay);
        bool skipGuidance = manualGuidanceStart && guidanceStartSwitch;
        if (!skipGuidance && !manualGuidanceStart && guidanceStartDelay > 0f)
        {
            guidanceStartDelay -= Time.fixedDeltaTime;
            skipGuidance = guidanceStartDelay > 0f;
        }

        // --- 誘導処理 ---
        if (!skipGuidance && target != null)
        {
            Vector3 dirToTarget = (target.position - transform.position).normalized;
            Vector3 guidanceDir = newDir.sqrMagnitude > 0.001f ? newDir.normalized : velocity.normalized;
            float angleDiff = Vector3.Angle(guidanceDir, dirToTarget);

            // 規定角度以上で誘導解除
            if (!useInitialPurePursuit && angleDiff > breakAngle)
            {
                target = null;
                return;
            }

            if (useInitialPurePursuit)
            {
                ApplyPurePursuitAssist(dirToTarget, initialTurnRate);
                if (Vector3.Angle(newDir, dirToTarget) <= initialTurnBreakAngle)
                {
                    useInitialPurePursuit = false;
                    lastDirToTarget = dirToTarget;
                }
            }
            else
            {
                // 比例航法近似
                if (lastDirToTarget != Vector3.zero)
                {
                    Vector3 LOSrate = Vector3.Cross(lastDirToTarget, dirToTarget);
                    Vector3 rotAxis = LOSrate.sqrMagnitude > 0.000001f
                        ? LOSrate.normalized
                        : Vector3.Cross(velocity.normalized, dirToTarget).normalized;
                    float rotMag = LOSrate.magnitude * ProportionalConstant * Mathf.Rad2Deg / Time.fixedDeltaTime;

                    float remainingTheta = Mathf.Max(0f, totalDeltaTheta - usedDeltaTheta);
                    currentTurnRate = Mathf.Max(0f, currentTurnRate - turnRateDecay * Time.fixedDeltaTime);

                    // 旋回速度上限と累積旋回角度上限
                    rotMag = Mathf.Min(rotMag, currentTurnRate);
                    float frameDeltaTheta = Mathf.Min(rotMag * Time.fixedDeltaTime, remainingTheta);

                    // 進行方向更新
                    if (frameDeltaTheta > 0f && rotAxis.sqrMagnitude > 0.000001f)
                    {
                        newDir = Quaternion.AngleAxis(frameDeltaTheta, rotAxis) * velocity.normalized;
                        usedDeltaTheta += frameDeltaTheta;
                    }
                }
            }

            lastDirToTarget = dirToTarget;
        }

        // --- 推進 ---
        speed += acceleration * Time.fixedDeltaTime;
        Mathf.Clamp(speed, 0f, maxspeed);
        velocity = newDir.normalized * speed;

        previousPos = transform.position;
        transform.position += velocity * Time.fixedDeltaTime;
        currentPos = transform.position;

        // --- 回転更新 ---
        if (velocity.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(velocity.normalized);

        // --- 衝突判定 ---
        RaycastHitCheck();
        if (!gameObject.activeSelf) return;

        if (ProjectileGroundBounds.IsBelowWorldOrTerrain(transform.position))
        {
            rangeover();
            return;
        }

        detectflare(isheatseeker);
    }

    private bool ShouldUsePurePursuitAssist()
    {
        return usePurePursuitAssistForLaunchOverride && launchDirectionOverride.sqrMagnitude > 0.001f;
    }

    private void ApplyPurePursuitAssist(Vector3 dirToTarget, float purePursuitTurnRate)
    {
        Vector3 currentDir = newDir.sqrMagnitude > 0.001f ? newDir.normalized : velocity.normalized;
        if (currentDir.sqrMagnitude <= 0.001f) return;

        float frameDeltaTheta = Mathf.Max(0f, purePursuitTurnRate * Time.fixedDeltaTime);
        if (frameDeltaTheta <= 0f) return;

        Vector3 assistedDir = Vector3.RotateTowards(
            currentDir,
            dirToTarget,
            frameDeltaTheta * Mathf.Deg2Rad,
            0f
        );

        float actualDeltaTheta = Vector3.Angle(currentDir, assistedDir);
        if (actualDeltaTheta <= 0.0001f) return;

        newDir = assistedDir.normalized;
    }

    // 命中判定
    void RaycastHitCheck()
    {
        Vector3 dir = (currentPos - previousPos).normalized;
        float dist = Vector3.Distance(previousPos, currentPos);
        if (dist <= 0.0001f) return;

        if (Physics.Raycast(previousPos, dir, out RaycastHit hit, dist, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) &&
            ProjectileGroundBounds.IsGroundCollider(hit.collider))
        {
            ImpactEffectFactory.Spawn(hit.point, effectRadius);
            rangeover();
            return;
        }

        allies = ObjectManager.Instance.allies;
        foreach (GameObject ally in allies)
        {
            if (ally == null) continue;

            Vector3 allyPos = ally.transform.position;
            float radius = 0.5f * (transform.localScale.x + ally.transform.localScale.x);

            Vector3 closestPoint = previousPos + Vector3.Project(allyPos - previousPos, dir);

            if (Vector3.Distance(closestPoint, allyPos) < radius &&
                Vector3.Dot(allyPos - previousPos, dir) > 0 &&
                Vector3.Distance(previousPos, allyPos) <= dist)
            {
                var status = ally.GetComponent<AugumentStatus>();
                if (status != null)
                {
                    status.damage(power); // ダメージ量は適宜調整
                    ImpactEffectFactory.Spawn(closestPoint, effectRadius);
                    rangeover();
                }
                return;
            }
        }
    }

    // 消滅処理
    void rangeover()
    {
        target = null;
        ObjectManager.Instance?.UnregisterMissile_e(gameObject);
        gameObject.SetActive(false);
    }

    void OnDisable()
    {
        ObjectManager.Instance?.UnregisterMissile_e(gameObject);
    }

    [Header("シーカー設定")]
    public float seekerFoV = 30f;          // シーカーの視野角
    public float flareHeatModifier = 1f;   // フレア熱補正倍率
    public Vector3 seekerDir = Vector3.forward;
    public List<DetectedObject> detectedObjects;
    void detectflare(bool enable = false)
    {
        if (!enable) return;

        seekerDir = target != null ? (target.position - transform.position).normalized : velocity.normalized;
        detectedObjects = new();

        List<GameObject> flares = ObjectManager.Instance.allies;
        foreach (GameObject flare in flares)
        {
            if (flare == null) continue;

            // --- (1) 方向と角度の事前判定 ---
            Vector3 dirToFlare = (flare.transform.position - transform.position).normalized;
            float angleToFlare = Vector3.Angle(seekerDir, dirToFlare);
            if (angleToFlare > seekerFoV) continue; // 視野外スキップ

            float distance = Vector3.Distance(transform.position, flare.transform.position);

            // --- (2) flare_e（純粋なフレア）を先にチェック ---
            flare f = flare.GetComponent<flare>();
            if (f != null && f.currentHeat > 0f)
            {
                float heatEffectiveness = f.currentHeat * flareHeatModifier / (distance * distance);
                detectedObjects.Add(new DetectedObject(flare, heatEffectiveness));
            }

            // --- (3) スクリプト全探索（throttle探索） ---
            foreach (var component in flare.GetComponents<MonoBehaviour>())
            {
                if (component == null) continue;

                Type type = component.GetType();
                FieldInfo throttleField = type.GetField("throttle",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (throttleField == null) continue;

                object value = throttleField.GetValue(component);
                if (value is float currentHeat && currentHeat > 0f)
                {
                    float heatEffectiveness = currentHeat / (distance * distance);
                    detectedObjects.Add(new DetectedObject(flare, heatEffectiveness));
                }
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
