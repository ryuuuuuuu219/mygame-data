using UnityEngine;

public class EnemyLRAceAircraft : AircraftController
{
    public Transform target; // プレイヤー機

    bool Overshoot;
    public float Overshoottimer;
    float savingdistanceToTarget;
    public float distdetecttimer = 0;
    float interval = 5f;
    public int pattern;


    float accelthrottle = 5f;
    float decelthrottle = 0.01f;

    Vector3 savingcordinate;

    float randomRoll=0;

    FCS_e weapon;
    private Rigidbody targetRb;

    float saving_t=0;
    private void Update()
    {
        if(weapon == null)
        {
            weapon = GetComponent<FCS_e>();
            if (weapon == null) return;
        }
            if (weapon.waytarget != null)
            {
                target = weapon.waytarget.transform;
            }

        //範囲制限
        if (transform.position.y < 600f)
        {
            Overshoot = true;
            Overshoottimer = Random.Range(1f, 3f);
            pattern = 3;
        }
        if (transform.position.y > 10000f)
        {
            Overshoot = true;
            Overshoottimer = Random.Range(1f, 3f);
            pattern = 4;
        }
    }

    protected override Vector3 GetControlInput()
    {
        if (target == null) return Vector3.zero;

        Vector3 localDir = ToLeadpoint();
        Torque = localDir;

        if (float.IsNaN(localDir.x) || float.IsNaN(localDir.y) || float.IsNaN(localDir.z))
        {
            localDir = target.position - transform.position;
        }

        float pitch = 0f;
        float yaw = 0f;
        float roll = 0f;

        localDir = transform.InverseTransformDirection(localDir);

        
        if (Overshoot)
        {
            switch (pattern)
            {
                case 1:
                // 全力旋回：反比例制御
                {
                    float rollError = Mathf.Clamp(localDir.x, -1f, 1f);
                    float rollStrength = Mathf.Abs(rollError);
                    float pitchStrength = 1f - rollStrength;

                    roll = rollError;
                    pitch = Mathf.Clamp(localDir.y, -1f, 1f) * pitchStrength;
                    yaw = rollError * 0.3f;
                }
                break;

                case 2:
                    // 直進 → 制御なし
                    break;

                case 3:
                // 上昇：反比例制御を適用
                {
                    // 上昇方向を優先
                    Vector3 upDir = Vector3.up;
                    Vector3 localUpDir = transform.InverseTransformDirection(upDir);

                    float rollError = Mathf.Clamp(localUpDir.x, -1f, 1f);
                    float rollStrength = Mathf.Abs(rollError);
                    float pitchStrength = 1f - rollStrength;

                    roll = rollError;
                    pitch = Mathf.Clamp(localUpDir.y, -1f, 1f) * pitchStrength;
                    yaw = rollError * 0.3f;
                }
                break;
                case 4:
                // 上昇：反比例制御を適用
                {
                    // 上昇方向を優先
                    Vector3 downDir = Vector3.down;
                    Vector3 localdownDir = transform.InverseTransformDirection(downDir);

                    float rollError = Mathf.Clamp(localdownDir.x, -1f, 1f);
                    float rollStrength = Mathf.Abs(rollError);
                    float pitchStrength = 1f - rollStrength;

                    roll = rollError;
                    pitch = Mathf.Clamp(localdownDir.y, -1f, 1f) * pitchStrength;
                    yaw = rollError * 0.3f;
                }
                break;

                case 5:
                    // ランダムロール：簡易的な制御
                    roll = randomRoll;
                    pitch = Mathf.Clamp(localDir.y, -1f, 1f);
                    break;

                case 6:
                    //逃げる
                {
                    // 上昇方向を優先
                    Vector3 localUpDir = -localDir;

                    float rollError = Mathf.Clamp(localUpDir.x, -1f, 1f);
                    float rollStrength = Mathf.Abs(rollError);
                    float pitchStrength = 1f - rollStrength;

                    roll = rollError;
                    pitch = Mathf.Clamp(localUpDir.y, -1f, 1f) * pitchStrength;
                    yaw = rollError * 0.3f;
                }
                break;

                default:
                case 7:
                    break;
            }
        }
        else
        {
            // 通常時も反比例制御
            float rollError = Mathf.Clamp(localDir.x, -1f, 1f);
            float rollStrength = Mathf.Abs(rollError);
            float pitchStrength = 1f - rollStrength;

            roll = rollError;
            pitch = Mathf.Clamp(localDir.y, -1f, 1f) * pitchStrength;
            yaw = rollError * 0.3f;
        }

        return new Vector3(pitch, roll, yaw);
    }

    protected override float GetThrottleInput()
    {
        if (target == null) return 1f;

        float distance = Vector3.Distance(transform.position, target.position);


        //軌道の揺らぎ
        if(distdetecttimer <= 0)
        {
            distdetecttimer = interval;
            if (Mathf.Abs(distance - savingdistanceToTarget) < 150f &&
                distance < 1000f)
            {
                if (!Overshoot)
                {
                    Overshoottimer = Random.Range(1f, 3f);
                    pattern = Random.Range(1, 7);
                    randomRoll = Random.Range(-1f, 1f);
                }
                Overshoot = true;
            }
            else if (distance < 5000f)
            {
                if (!Overshoot)
                {
                    Overshoottimer = Random.Range(1f, 3f);
                    pattern = 6;
                    randomRoll = Random.Range(-1f, 1f);
                }
                Overshoot = true;
            }

            else
            {
                Overshoot = false;
            }
            //スタック判定
            if (Vector3.Distance(savingcordinate, transform.position) < 1f)
            {
                transform.position += new Vector3(0, 50f, 0);
                rb.linearVelocity = Vector3.up * 50f;
            }

            if (pattern == 6)
            {
                Overshoottimer = Random.Range(13f, 15f);
            }

            savingcordinate = transform.position;
            savingdistanceToTarget = distance;
        }
        else
        {
            distdetecttimer -= Time.deltaTime;
        }
        if (Overshoot)
        {
            Overshoottimer -= Time.deltaTime;
            distdetecttimer = 13f;
            if (Overshoottimer <= 0) Overshoot = false;
            switch (pattern)
            {
                case 1:
                    return 0f; // 減速
                case 2:
                case 3:
                case 6:
                case 7:
                    return accelthrottle; // 加速
                default:
                    return decelthrottle;
            }
        }

        if (distance > 8000f ||
            rb.linearVelocity.magnitude < GetComponent<AircraftController>().stallSpeed) return accelthrottle;  // 追尾時は加速
        if (distance < 6000f) return randomRoll+1.5f; // 接近しすぎたら減速
        return 1f; // 巡航
    }



    // ガンリード計算（軽量反復）
    Vector3 ToLeadpoint()
    {
        if (target == null || rb == null) return transform.forward;

        targetRb = target.GetComponent<Rigidbody>();
        if (targetRb == null) return transform.forward;

        if (weapon == null)
        {
            weapon = GetComponent<FCS_e>();
            if (weapon == null) return transform.forward;
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
                         - (muzzlePos + 0.5f * Physics.gravity * t * t);

        if (aimDir.sqrMagnitude < 1e-6f)  // ゼロベクトル防止
            return transform.forward;

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

        float t = (saving_t == 0 ? Vector3.Distance(muzzlePos, targetPos) / bulletSpeed : saving_t);

        for (int i = 0; i < 5; i++)
        {
            Vector3 futureTarget = targetPos + targetVel * t;
            Vector3 bulletFuture = muzzlePos + bulletVel0 * t + 0.5f * Physics.gravity * t * t;

            float error = Vector3.Distance(bulletFuture, futureTarget);
            if (error < 0.5f) break;

            float dist = Vector3.Distance(muzzlePos, futureTarget);
            if (bulletSpeed > 0.01f)
                t = dist / bulletSpeed;
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