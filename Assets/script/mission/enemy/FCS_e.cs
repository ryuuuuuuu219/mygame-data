using UnityEngine;
using System.Collections.Generic;

public class FCS_e : MonoBehaviour
{
    [Header("Gun Settings")]
    public int maxBullets = 120;
    public int currentBullets;
    public GameObject bulletPrefab;
    public Transform gunMuzzle;
    public float bulletSpeed = 200f;
    public float fireRate = 0.1f; // 秒間発射間隔
    public float spreadAngle = 2f; // ばらつきの角度（度）

    public float gunPower = 10f;
    public float gunSize = 0.5f;

    [Header("Missile Settings")]
    public int maxMissiles = 2;
    public int currentMissiles;
    public GameObject missilePrefab;
    public Transform missileHardpoint;
    public float missileSpeed = 100f;
    public float missilelifeTime = 2f;
    public float missileCooldown = 0.3f; // ミサイル発射間隔

    public float mslPower = 50f;
    public float mslacceleration = 20f;
    public float mslmaxspeed = 150f;
    public float mslturnRate = 90f; // 最大旋回速度 (deg/sec)
    public float mslbreakAngle = 90f; // 誘導解除角度 (deg)
    public float mslProportionalConstant = 3f; // 比例航法定数

    private float nextFireTime = 0f;
    private float nextMissileTimeA = 0f;
    private float nextMissileTimeB = 0f;

    public float detectRange = 3000f;        // 探索範囲（UI表示用）
    public float lockRange = 850f;           // ロック範囲
    public float gunRange = 500f;            // 機銃射程 

    public float LmissilelifeTime = 5f;

    Rigidbody rb;
    [SerializeField] Gun_e_pool bulletpool;

    public bool lockon;

    private List<GameObject> targets;
    public GameObject target, fov1, fov2;
    public GameObject waytarget;

    float changetimer = 0f;
    float changecooldown = 5f;

    public AugumentStatus status;

    float ToTargetFov(Vector3 worldPos)
    {
        if (rb == null || rb.linearVelocity.magnitude < 0.1f) return -1f;

        // 機体の進行方向（速度ベクトルを正規化）
        Vector3 forward = rb.linearVelocity.normalized;

        // 目標方向
        Vector3 dirToTarget = (worldPos - transform.position).normalized;

        // 進行方向と目標方向の角度
        float angle = Vector3.Angle(forward, dirToTarget);

        if (angle <= 60f)
        {
            return angle;
        }

        // 視野外
        return -1f;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        status = GetComponent<AugumentStatus>();
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
        float dummy;

        status.altGetVar("ミサイル：弾数", out dummy);
        maxMissiles = (int)dummy;
        currentMissiles = maxMissiles;

        status.altGetVar("ミサイル：装填時間", out missileCooldown);
        status.altGetVar("ミサイル：初速", out missileSpeed);
        status.altGetVar("ミサイル：飛翔時間", out missilelifeTime);

        status.altGetVar("ミサイル：威力", out mslPower);

        status.altGetVar("ミサイル：加速度", out mslacceleration);
        status.altGetVar("ミサイル：最高速", out mslmaxspeed);
        status.altGetVar("ミサイル：誘導力", out mslturnRate);
        status.altGetVar("ミサイル：誘導象限", out mslbreakAngle);
        status.altGetVar("ミサイル：比例航法定数", out mslProportionalConstant);


        status.altGetVar("長射程マルチロックミサイル：飛翔時間", out LmissilelifeTime);

        status.altGetVar("銃弾：初速", out bulletSpeed);
        status.altGetVar("銃弾：発射レート", out fireRate);
        fireRate = 1f / fireRate;
        status.altGetVar("銃弾：威力", out gunPower);
        status.altGetVar("銃弾：当たり判定サイズ", out gunSize);

        status.altGetVar("銃弾：弾数", out dummy);
        maxBullets = (int)dummy;
        currentBullets = maxBullets;
    }

    void Update()
    {
        status.Velocity = rb.linearVelocity;


        changetimer -= Time.deltaTime;
        // -------- 目標探索 --------
        if (changetimer < 0f)
        {
            changetimer = changecooldown;

            targets = ObjectManager.Instance.allies;

            if (targets.Count > 1)
            {
                // 視野内の敵からFOVが小さい順に2体選択
                fov1 = null; fov2 = null;
                float minFov = 10f, secondMinFov = 10f;
                float waytargetdist = Mathf.Infinity;

                foreach (var t in targets)
                {
                    float fov = ToTargetFov(t.transform.position);
                    float dist = Vector3.Distance(transform.position, t.transform.position);
                    if (fov >= 0f && dist < detectRange)
                    {
                        if (fov < minFov)
                        {
                            secondMinFov = minFov; fov2 = fov1;
                            minFov = fov; fov1 = t;
                        }
                        else if (fov < secondMinFov)
                        {
                            secondMinFov = fov; fov2 = t;
                        }
                    }
                    // 最も近い敵をwaytargetに設定
                    if (dist < waytargetdist)
                    {
                        waytargetdist = dist;
                        waytarget = t;
                    }
                }
                if (fov1 != null)
                {
                    if (fov2 != null)
                    {
                        if (target == fov1)
                        {
                            target = fov2;
                        }
                        else
                        {
                            target = fov1;
                        }
                    }
                    else
                    {
                        target = fov1;
                    }
                }
                else
                {
                    target = null;
                }
            }
            else if (targets.Count == 1)
            {
                float fov = ToTargetFov(targets[0].transform.position);
                if (fov >= 0f)
                {
                    target = targets[0];
                }
                waytarget = targets[0];
            }
            else
            {
                target = null;
                waytarget = null;
            }
        }

        if (target == null &&
            waytarget != null)
        { target = waytarget; }

        // ロック条件: marktargetがロック範囲内かつ視野内
        if (target != null)
        {
            if (Vector3.Distance(transform.position, target.transform.position) < lockRange &&
                                ToTargetFov(target.transform.position) >= 0f)
            {
                if (!lockon)
                {
                    nextMissileTimeA = Time.time + 0.5f;
                    nextMissileTimeB = Time.time + 0.8f;
                }
                lockon = true;
            }
            else
            {
                lockon = false;
            }
        }
        else
        {
            lockon = false;
        }

        // ------------------------

        if (lockon && Time.time >= nextFireTime &&
            bulletPrefab != null)
        {
            FireGun();
            nextFireTime = Time.time + fireRate;
        }

        if (lockon && Time.time >= nextMissileTimeA &&
            missilePrefab != null)
        {
            FireMissile();
            nextMissileTimeA = Time.time + missileCooldown;
            nextMissileTimeB += 0.3f;
        }

        if (lockon && Time.time >= nextMissileTimeB &&
            missilePrefab != null)
        {
            FireMissile();
            nextMissileTimeB = Time.time + missileCooldown;
            nextMissileTimeA += 0.3f;
        }
    }

    void FireGun()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();
        if (gunMuzzle == null)
            gunMuzzle = transform;
        if (bulletpool == null)
            bulletpool = FindFirstObjectByType<Gun_e_pool>();
        if (rb == null || bulletpool == null) return;

        //ばらつき
        Vector3 spread = new Vector3(
            Random.Range(-spreadAngle, spreadAngle),
            Random.Range(-spreadAngle, spreadAngle),
            Random.Range(-spreadAngle, spreadAngle)
        );

        Vector3 velocity = rb.linearVelocity + transform.forward * bulletSpeed + spread;
        bulletpool.bulletpull(gunSize, gunMuzzle.position, velocity, 3f);
    }

    void FireMissile()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();
        if (missileHardpoint == null)
            missileHardpoint = transform;
        if (bulletpool == null)
            bulletpool = FindFirstObjectByType<Gun_e_pool>();
        if (rb == null || bulletpool == null) return;

        Vector3 velocity = rb.linearVelocity + transform.forward * bulletSpeed;
        GameObject MSL = bulletpool.missilepull(missileHardpoint.position, velocity, 10f);
        Missile missile = MSL.GetComponent<Missile>();
        if (missile != null && target != null)
        {
            missile.missileInit(missileHardpoint.position, velocity, missilelifeTime);
            missile.StatusSetting(mslPower, mslacceleration, mslmaxspeed, mslturnRate, mslbreakAngle, mslProportionalConstant);
            missile.target = target.transform;

        }
    }

}
