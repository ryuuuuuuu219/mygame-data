using System;
using UnityEngine;
using Random = UnityEngine.Random;



public class EnemyAceAircraft : AircraftController
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

    [System.Serializable]
    public struct AIStateParam
    {
        public AIState state;
        public float threshold;   // 例：高度・距離・条件値
    }

    [SerializeField]
    //難易度設定
    AIStateParam[] stateParams = 
    {
        new AIStateParam{ state=AIState.Pursuit, threshold = 0f },
        new AIStateParam{ state=AIState.Offset, threshold = 900f },
        new AIStateParam{ state=AIState.Overshoot, threshold = 400f },
        new AIStateParam{ state=AIState.RecoverAltitude, threshold = 1000f },
        new AIStateParam{ state=AIState.Evade, threshold = 0f },
        new AIStateParam{ state=AIState.Exit, threshold = 300f }
    };

    AIState currentstate = AIState.Pursuit;

    public float statetimer;
    float stateinterval = 5f;

    public float distdetecttimer = 0;

    float accelthrottle = 5f;
    float decelthrottle = 0.01f;

    Vector3 leadPos;//偏差射撃安定化用の相対座標　格納変数

    FCS_e weapon;
    private Rigidbody targetRb;

    float saving_t=0;
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
        if (target == null) return;

        if (statetimer > 0f)
        {
            statetimer -= Time.deltaTime;
        }
        else
        {
            currentstate = AIState.none;

            if (transform.position.y < stateParams[(int)AIState.RecoverAltitude].threshold)
            {
                currentstate = AIState.RecoverAltitude;
                statetimer = Random.Range(1f, 3f);
            }

            float distance = Vector3.Distance(transform.position, target.position);
            float angleToTarget = Vector3.Angle(rb.linearVelocity.normalized, (target.position - transform.position).normalized);

            if (distance < stateParams[(int)AIState.Offset].threshold)
            {
                currentstate = AIState.Offset;
                leadPos = new Vector3(
                    Random.Range(-500f, 500f),
                    Random.Range(-500f, 500f),
                    Random.Range(-500f, 500f));
                statetimer = Random.Range(1f, 3f);
            }
            else if (transform.position.y > 10000f)
            {
                currentstate = AIState.Pursuit;
                leadPos = new Vector3(
                    Random.Range(-500f, 500f),
                    Random.Range(-500f, 500f),
                    Random.Range(-500f, 500f));
                statetimer = Random.Range(1f, 3f);
            }
            else if (distance < stateParams[(int)AIState.Exit].threshold)
            {
                currentstate = AIState.Exit;
                statetimer = Random.Range(1f, 3f);
            }
            else if (distance < stateParams[(int)AIState.Overshoot].threshold &&
                        angleToTarget > 90f)
            {
                currentstate = AIState.Overshoot;
                statetimer = Random.Range(1f, 3f);
            }

            if (currentstate == AIState.none)
            {
                currentstate = AIState.Pursuit;
                statetimer = stateinterval;
            }
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
        if (target == null) return Vector3.zero;
        Vector3 lead;

        Vector3 localDir = ToLeadpoint(out lead);

        if (lead != Vector3.zero &&
            currentstate != AIState.Offset)
        {
            leadPos = lead;
        }
        else
        {
            if (float.IsNaN(localDir.x) || float.IsNaN(localDir.y) || float.IsNaN(localDir.z))
            {
                localDir = target.position - transform.position;
            }
        }

        Vector3 manuverInput;
        switch (currentstate)
        {
            case AIState.Offset:
            {
                Vector3 offsetDir = localDir + leadPos * 0.001f;
                manuverInput = manuver(offsetDir);
            }
            break;
            case AIState.Overshoot:
            {
                manuverInput = manuver(localDir);
                manuverInput *= 0.4f;
            }
            break;
            case AIState.RecoverAltitude:
            {
                Vector3 upDir = Vector3.up * 1000f - transform.position;
                manuverInput = manuver(upDir);
            }
            break;
            case AIState.Evade:
            {
                Vector3 evadeDir = lead;
                manuverInput = manuver(evadeDir);
            }
            break;
            case AIState.Exit:
            {
                Vector3 exitDir = -localDir;
                manuverInput = manuver(exitDir);
            }
            break;
            default:
            case AIState.Pursuit:
            {
                manuverInput = manuver(localDir);
            }
            break;
        }

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
                if (distance < stateParams[(int)AIState.Offset].threshold)
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