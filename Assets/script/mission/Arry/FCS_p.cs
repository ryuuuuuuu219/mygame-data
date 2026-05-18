using UnityEngine;
using System.Collections.Generic;

public class FCS_p : MonoBehaviour
{
    [Header("Gun Settings")]
    public GameObject bulletPrefab;
    public Transform gunMuzzle;
    public float bulletSpeed = 200f;
    public float fireRate = 0.1f; // 秒間発射間隔

    [Header("Missile Settings")]
    public GameObject missilePrefab;
    public Transform missileHardpoint;
    public float missileSpeed = 100f;
    public float missileCooldown = 0.3f; // ミサイル発射間隔
    public float missilelifeTime = 10f;
    public float mslPower = 50f;
    public float mslacceleration = 20f;
    public float mslmaxspeed = 150f;
    public float mslturnRate = 90f;
    public float mslbreakAngle = 90f;
    public float mslProportionalConstant = 3f;

    private float nextFireTime = 0f;
    private float nextMissileTimeA = 0f;
    private float nextMissileTimeB = 0f;

    public float detectRange = 3000f;        // 探索範囲（UI表示用）
    public float lockRange = 850f;           // ロック範囲
    public float gunRange = 500f;            // 機銃射程 


    Rigidbody rb;
    [SerializeField] Gun_p_pool bulletpool;

    bool lockon;

    
    public GameObject target, fov1, fov2;
    public GameObject waytarget;

    float changetimer = 0f;
    float changecooldown = 5f;

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
        ObjectManager.Instance?.RegisterAlly(gameObject);
    }

    void Update()
    {
        if (ObjectManager.Instance == null)
        {
            return;
        }

        changetimer -= Time.deltaTime;
        // -------- 目標探索 --------
        if (changetimer < 0f)
        {
            changetimer = changecooldown;

            var targets = ObjectManager.Instance.Enemies;

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
        // ------------------------

        // ロック条件: marktargetがロック範囲内かつ視野内
        if (target != null)
        {
            if (Vector3.Distance(transform.position, target.transform.position) < lockRange &&
                                ToTargetFov(target.transform.position) >= 0f)
            {
                lockon = true;
            }
            else
            {
                lockon = false;
            }
        }
        else {
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
            bulletpool = FindFirstObjectByType<Gun_p_pool>();

        Vector3 velocity = (rb != null ? rb.linearVelocity : Vector3.zero) + gunMuzzle.forward * bulletSpeed;
        if (bulletpool != null)
        {
            bulletpool.bulletpull(0.5f, gunMuzzle.position, velocity, 3f);
            return;
        }

        GameObject bullet = Instantiate(bulletPrefab, gunMuzzle.position, gunMuzzle.rotation);
        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
        if (bulletRb != null)
            bulletRb.linearVelocity = velocity;
    }

    void FireMissile()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();
        if (missileHardpoint == null)
            missileHardpoint = transform;
        if (bulletpool == null)
            bulletpool = FindFirstObjectByType<Gun_p_pool>();
        if (rb == null || bulletpool == null) return;

        Vector3 velocity = rb.linearVelocity + transform.forward * missileSpeed;
        GameObject MSL = bulletpool.missilepull(missileHardpoint.position, velocity, missilelifeTime);
        Missile_p missile = MSL.GetComponent<Missile_p>();

        if (missile != null)
        {
            missile.StatusSetting(mslPower, mslacceleration, mslmaxspeed, mslturnRate, mslbreakAngle, mslProportionalConstant);
            if (target != null)
            {
                missile.target = target.transform;
            }

        }
    }
}
