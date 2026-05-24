using System;
using UnityEngine;
using Random = UnityEngine.Random;



public class EnemyAceAircraftM03 : AircraftController
{
    public Transform target; // プレイヤー機

    public enum AIState
    {
        Pursuit,//純粋追尾
        Offset,//緩めの追尾
        Overshoot,//減速
        RecoverAltitude,//上昇
        Evade,//回避
        Exit,//離脱
        none
    }

    public bool isUseAugument;

    [Header("AIpersonalBehaviour")]
    public float BORDER_Pursuit = 1600f;
    public float BORDER_Exit = 700f;
    public float transitiondelay = 2f;
    public float transitiondelaytimer = 0f;

    [System.Serializable]
    public struct AIStateParam
    {
        public AIState state;
        public float level;
        public float delay;
    }

    [Header("AIJudge")]
    [SerializeField]AIStateParam[] stateParams = new AIStateParam[]
    {
            new AIStateParam(){ state = AIState.Pursuit, level = 0f, delay = 0.5f },
            new AIStateParam(){ state = AIState.Offset, level = 0f, delay = 0.2f },
            new AIStateParam(){ state = AIState.Overshoot, level = 0f, delay = 1.2f },
            new AIStateParam(){ state = AIState.RecoverAltitude, level = 0f, delay = 0.3f },
            new AIStateParam(){ state = AIState.Evade, level = 0f, delay = 0.1f },
            new AIStateParam(){ state = AIState.Exit, level = 0f, delay = 0.8f },
    };

    [SerializeField] AIState currentstate = AIState.Pursuit;
    [SerializeField] AIState bookingstate = AIState.Pursuit;

    public float distdetecttimer = 0;
    bool isBaseVectordefined = false;

    Vector3 targetdir;

    float accelthrottle = 5f;
    float decelthrottle = 0.01f;

    Vector3 leadPos;//偏差射撃安定化用の相対座標　格納変数
    float saving_t=0;

    FCS_e weapon;
    private Rigidbody targetRb;


    private void Update()
    {
        if(target == null)
        {
            weapon = GetComponent<FCS_e>();
            if (weapon == null) return;
            if (weapon.target != null)
            {
                target = weapon.target.transform;
            }
            else if (weapon.waytarget != null)
            {
                target = weapon.waytarget.transform;
            }
        }
        if (target == null) return;

        // AIステート評価
        levelCalc();
        if (!isBaseVectordefined)
        {
            isBaseVectordefined=true;
            // ステート決定後に1回だけ目標ベクトルを算出
            vectorCalc();
        }
        if (isUseAugument)
        {
            // 遠距離用の補助挙動
            levelCalc_augument();
        }

        if (weapon == null)
        {
            weapon = GetComponent<FCS_e>();
            if (weapon == null) return;
        }
        if (target != weapon.waytarget)
        {
            if (weapon.target != null)
            {
                target = weapon.target.transform;
            }
            else if (weapon.waytarget != null)
            {
                target = weapon.waytarget.transform;
            }
        }

    }

    void levelCalc()
    {
        if (target == null)
        {
            currentstate = AIState.none;
            return;
        }
        if (transform.position.y < 1000f)
        {
            stateParams[(int)AIState.RecoverAltitude].level = (1000f - transform.position.y);
        }
        else if (transform.position.y > 8000f)
        {
            stateParams[(int)AIState.RecoverAltitude].level = (transform.position.y - 8000f);
        }

        float Dist = (transform.position - target.position).magnitude;
        float angleToTransform = Vector3.Angle(target.transform.forward, (transform.position - target.position).normalized);
        float angleToTarget = Vector3.Angle(transform.forward, (target.position - transform.position).normalized);

        stateParams[(int)AIState.Pursuit].level = Mathf.Clamp(Dist - BORDER_Pursuit, 0f, 1000f);
        stateParams[(int)AIState.Exit].level = Mathf.Clamp(BORDER_Exit - Dist, 0f, 200f) * 5f;
        stateParams[(int)AIState.Offset].level = Mathf.Clamp(800f - Dist, 0f, 800f) * 1.5f;
        float overshoot = Mathf.Clamp(300f - Dist, 0f, 300f)
            * Mathf.Clamp(throttle, 0f, 5f)
            * Mathf.Clamp(60f - angleToTransform, 0f, 60f)
            * Mathf.Clamp(angleToTarget - 120f, 0f, 60f);

        // 0~300*5*60*60（最大値）= 540000


        overshoot /= 540;

        stateParams[(int)AIState.Overshoot].level =
            Mathf.Clamp(overshoot, 0f, 800f);

        // ミサイル回避優先度を積算
        bool missileThreat = false;

        float nearestMissile = float.MaxValue;

        foreach (var v in ObjectManager.Instance.missiles_a)
        {
            if (v == null) continue;

            missileThreat = true;
            float d = Vector3.Distance(transform.position, v.transform.position);
            nearestMissile = Mathf.Min(nearestMissile, d);
        }

        stateParams[(int)AIState.Evade].level +=
            (missileThreat ? Mathf.Clamp(1500f - nearestMissile, 0f, 1200f) : 0f)
            * Time.deltaTime;


        float maxlevel = -1f;
        AIState nextState = stateParams[0].state;
        // 最大レベルのステートを選択
        for (int i = 0; i < stateParams.Length; i++)
        {
            if (stateParams[i].level > maxlevel)
            {
                maxlevel = stateParams[i].level;
                nextState = stateParams[i].state;
            }
        }
        if (nextState != currentstate)
        {
            if (bookingstate != nextState)
            {
                transitiondelaytimer = 0f;
                bookingstate = nextState;
            }
            transitiondelaytimer += Time.deltaTime;
            if (transitiondelaytimer >= stateParams[(int)bookingstate].delay)
            {
                currentstate = bookingstate;
                transitiondelaytimer = 0f;
                isBaseVectordefined = false;
            }
        }
        for (int i = 0; i < stateParams.Length; i++)
        {
            stateParams[i].level = 0;
        }
    }

    public float basedist = 3000f;
    public float basedist_close = 1000f;
    public float notchingangle = 98f;
    public float seminotchingangle = 45f;
    enum AIphase
    {
        None = 0,
        Exiting,
        Aheading,
        Attacking_notching,
        Attacking_seminotching,
        Intercepting_seminotching,
        Intercepting_headon
    }
    AIphase currentphase = AIphase.None;

    void levelCalc_augument()//遠距離専用挙動アドオン
    {
        float Dist = (transform.position - target.position).magnitude;
        float angleToTransform = Vector3.Angle(target.transform.forward, (transform.position - target.position).normalized);
        float angleToTarget = Vector3.Angle(transform.forward, (target.position - transform.position).normalized);

        if (currentstate == AIState.Evade)
        {
            // 回避時は補助挙動を停止
            currentphase = AIphase.None;
            return;
        }
        if (currentphase == AIphase.None)
        {
            // 離脱→正面→ノッチング→接近の順で遷移
            currentphase = AIphase.Exiting;
            targetdir = notchingVec();
        }
        else if (currentphase == AIphase.Exiting)
        {
            currentstate = AIState.Exit;
            if (currentstate == AIState.Evade)
            {
                currentphase = AIphase.None;
            }
            else if (Dist > basedist)
            {
                currentphase = AIphase.Aheading;
                targetdir = notchingVec();
            }
        }
        else if (currentphase == AIphase.Aheading)
        {
            currentstate=AIState.Offset;
            if (angleToTarget < notchingangle)
            {
                currentphase = AIphase.Attacking_notching;
                targetdir = notchingVec(notchingangle);
            }
        }
        else if (currentphase == AIphase.Attacking_notching)
        {
            currentstate = AIState.Offset;
            if (angleToTarget < seminotchingangle)
            {
                currentphase = AIphase.Attacking_seminotching;
                targetdir = notchingVec(seminotchingangle);
            }
        }
        else if (currentphase == AIphase.Attacking_seminotching)
        {
            currentstate = AIState.Offset;
            if (Dist < basedist)
            {
                currentphase = AIphase.Intercepting_seminotching;
                targetdir = notchingVec(seminotchingangle);
            }
            else if(angleToTarget > seminotchingangle)
            {
                currentphase = AIphase.Attacking_notching;
                targetdir = notchingVec(notchingangle);
            }
        }
        else if (currentphase == AIphase.Intercepting_seminotching)
        {
            currentstate = AIState.Offset;
            if (Dist > basedist)
            {
                currentphase = AIphase.Attacking_notching;
                targetdir = notchingVec(notchingangle);
            }
            else if (angleToTransform > 5f)
            {
                currentphase = AIphase.Intercepting_headon;
                targetdir = notchingVec();
            }
            else if (Dist < basedist_close)
            {
                currentphase = AIphase.None;
                targetdir = notchingVec();
            }
        }
        else if (currentphase == AIphase.Intercepting_headon)
        {
            currentstate = AIState.Offset;
            if (angleToTransform < 5f)
            {
                currentphase = AIphase.Intercepting_seminotching;
                targetdir = notchingVec(seminotchingangle);
            }
            else if (Dist < basedist_close)
            {
                currentphase = AIphase.None;
                targetdir = notchingVec();
            }
        }
    }

    Vector3 notchingVec(float angle = 0f)
    {
        Vector3 toTarget = (target.position - transform.position).normalized;

        // 左右どちらかのノッチ方向
        Vector3 notchDir =
            Vector3.Cross(Vector3.up, toTarget).normalized;

        bool isRight = Vector3.Dot(transform.right, notchDir) > 0f;

        // 右でなければ左に反転
        notchDir = isRight ? notchDir : -notchDir;

        Quaternion fromRot = Quaternion.LookRotation(transform.forward);
        Quaternion toRot = Quaternion.LookRotation(notchDir);

        Quaternion rot = Quaternion.RotateTowards(
            fromRot,
            toRot,
            angle * Time.deltaTime
        );

        return rot * Vector3.forward;
    }

    void vectorCalc()
    {
        Vector3 localDir = ToLeadpoint(out Vector3 lead);


        if (lead != Vector3.zero &&
            currentstate != AIState.Offset)
        {
            // 偏差が取れている間は保持して安定化
            leadPos = lead;
        }
        else
        {
            if (float.IsNaN(localDir.x) || float.IsNaN(localDir.y) || float.IsNaN(localDir.z))
            {
                // NaN回避: 直進補正
                localDir = target.position - transform.position;
            }
        }

        switch (currentstate)
        {
            case AIState.Evade:
            {
                // 近距離ミサイルを避けつつ前進成分も残す
                Vector3 evade = Vector3.zero;

                foreach (var v in ObjectManager.Instance.missiles_a)
                {
                    if (v == null) continue;

                    Vector3 toMe = transform.position - v.transform.position;
                    float dist = toMe.magnitude;

                    // 距離カット
                    if (dist > 1500f) continue;

                    // ミサイルが自分を向いていないなら無視
                    Vector3 missileVel = v.GetComponent<Rigidbody>()?.linearVelocity ?? Vector3.zero;
                    if (Vector3.Dot(missileVel.normalized, toMe.normalized) < 0.5f)
                        continue;

                    evade += toMe.normalized / Mathf.Max(dist * dist, 1f);
                }


                Vector3 forward = rb.linearVelocity.normalized;

                targetdir =
                (
                    evade.normalized * 1.0f +
                    lead.normalized * 0.4f +
                    forward * 0.3f
                ).normalized;

            }
            break;
            case AIState.Overshoot:
            {
                // 減速で直進維持
                targetdir = transform.forward;
            }
            break;
            case AIState.RecoverAltitude:
            {
                // 上限/下限の高さに戻す
                targetdir = Vector3.up * (transform.position.y < 5000 ? 1 : -1);
            }
            break;
            case AIState.Exit:
            {
                // 目標から離脱
                targetdir = (transform.position - target.position).normalized;
            }
            break;
            default:
            case AIState.Offset:
            case AIState.Pursuit:
            {
                // 偏差＋現在方向の合成
                targetdir = localDir + leadPos * 0.002f;
            }
            break;
        }
    }


    Vector3 manuver(Vector3 worldDir)
    {
        Vector3 result = Vector3.zero;

        Vector3 localDir = transform.InverseTransformDirection(worldDir.normalized);

        // =========================
        // 下方向にいる度合い（0〜1）
        // =========================
        float downFactor = Mathf.Clamp01(-localDir.y);

        // =========================
        // ロール：常に主役（姿勢作り）
        // =========================
        float roll = Mathf.Clamp(localDir.x, -1f, 1f);

        // =========================
        // ピッチ：
        // 下にいる間は 0.3 倍まで抑制
        // =========================
        float pitchScale = Mathf.Lerp(0.3f, 1f, 1f - downFactor);
        float pitch = Mathf.Clamp(localDir.y, -1f, 1f) * pitchScale;

        // =========================
        // ヨー：
        // ロール中かつ下方向ほど効かせる
        // =========================
        float yaw = Mathf.Clamp(localDir.x, -1f, 1f)
                  * downFactor
                  * Mathf.Abs(roll);

        // =========================
        // 出力
        // =========================
        result.x = pitch;   // pitch
        result.y = roll;    // roll
        result.z = yaw;     // yaw

        return result;
    }


    protected override Vector3 GetControlInput()
    {
        Vector3 manuverInput = manuver(targetdir);

        return manuverInput;
    }

    protected override float GetThrottleInput()
    {
        if (target == null) return 1f;

        float distance = Vector3.Distance(transform.position, target.position);

        switch (currentstate)
        {
            case AIState.Pursuit:
                return accelthrottle;
            case AIState.Offset:
                if (distance < BORDER_Pursuit)
                {
                    return decelthrottle;
                }
                else
                {
                    return accelthrottle;
                }
            case AIState.Overshoot:
                return decelthrottle;
            case AIState.RecoverAltitude:
                return accelthrottle;
            case AIState.Exit:
                return accelthrottle;
            default:
                return 1f;
        }
    }



    // ガンリード計算（軽量反復）
    Vector3 ToLeadpoint(out Vector3 lead)
    {
        if (target == null || rb == null)
        {
            lead = Vector3.zero;
            return transform.forward;
        }

        targetRb = target.GetComponent<Rigidbody>();
        if (targetRb == null)
        {
            lead = Vector3.zero; return transform.forward;
        }

        if (weapon == null)
        {
            weapon = GetComponent<FCS_e>();
            if (weapon == null)
            {
                lead = Vector3.zero; return transform.forward;
            }
        }

        float bulletSpeed = Mathf.Max(1f, weapon.bulletSpeed); // 速度ゼロ防止

        Vector3 muzzlePos = transform.position;
        Vector3 bulletVel0 = rb.linearVelocity + transform.forward * bulletSpeed;

        float t = PredictIntercept(
            muzzlePos,
            bulletVel0,
            targetRb.position,
            targetRb.linearVelocity,
            bulletSpeed
        );

        Vector3 aimDir = targetRb.position + targetRb.linearVelocity * t
                         - muzzlePos;

        if (aimDir.sqrMagnitude < 1e-6f)  // ゼロベクトル防止
        {
            lead = Vector3.zero;
            return transform.forward;
        }

        lead = targetRb.position + targetRb.linearVelocity * t - transform.position;
        return aimDir.normalized;
    }

    float PredictIntercept(
    Vector3 muzzlePos,
    Vector3 bulletVel0,
    Vector3 targetPos,
    Vector3 targetVel,
    float bulletSpeed)
    {
        if (bulletSpeed <= 0.01f) return 0f;

        float t = (saving_t == 0 ? Vector3.Distance(muzzlePos, targetPos) / (bulletSpeed + rb.linearVelocity.magnitude) : saving_t);

        // 少回数の反復で着弾時間を近似
        for (int i = 0; i < 5; i++)
        {
            Vector3 futureTarget = targetPos + targetVel * t;
            Vector3 bulletFuture = muzzlePos + bulletVel0 * t + 0.5f * Physics.gravity * t * t;

            float error = Vector3.Distance(bulletFuture, futureTarget);
            if (error < 0.5f) break;

            float dist = Vector3.Distance(muzzlePos, futureTarget);
            if (bulletSpeed > 0.01f)
                t = dist / (bulletSpeed + rb.linearVelocity.magnitude);
            else
                t = 0f;
        }

        // 無限大/NaN防止
        if (float.IsNaN(t) || float.IsInfinity(t) || t < 0f)
            t = 0f;

        t = Mathf.Clamp(t, 0f, 30f); // 上限リミット

        saving_t = t;
        return t;
    }


}
