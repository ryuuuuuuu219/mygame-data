using System.Collections.Generic;
using UnityEngine;

public class GraundTGT : MonoBehaviour
{
    public Vector3 Velocity;

    public AugumentStatus status;

    [Header("Gun Settings")]
    public bool isgun;
    public Transform gunMuzzle;
    public float bulletSpeed = 200f;
    public float fireRate = 0.1f; // 秒間発射間隔
    public float spreadAngle = 2f; // ばらつきの角度（度）

    [Header("Missile Settings")]
    public bool ismissile;
    public Transform missileHardpoint;
    public float missileCooldown = 0.3f; // ミサイル発射間隔

    private float nextFireTime = 0f;
    private float nextMissileTimeA = 0f;
    private float nextMissileTimeB = 0f;

    public float detectRange = 3000f;        // 探索範囲（UI表示用）
    public float lockRange = 850f;           // ロック範囲
    public float gunRange = 500f;            // 機銃射程 


    [SerializeField]Gun_e_pool bulletpool;

    public bool lockon;
    Vector3 lastDirToTarget;

    private List<GameObject> targets;
    public GameObject target;
    public GameObject closetarget,close2target;

    public float fireRange = 600f;        // 発射可能距離
    public float fireInterval = 5f;       // 発射間隔（秒）

    void ToTargetdist()
    {
        targets = ObjectManager.Instance.allies;

        if (targets.Count > 1)
        {
            float dist1 = Mathf.Infinity, dist2 = Mathf.Infinity;

            foreach (var t in targets)
            {
                float dist = Vector3.Distance(transform.position, t.transform.position);
                if (dist < detectRange)
                {
                    if (dist < dist1)
                    {
                        dist1 = dist;
                        closetarget = t;
                    }
                    else if (dist < dist2)
                    {
                        dist2 = dist;
                        close2target = t;
                    }
                }
            }
            if (closetarget != null)
            {
                if (close2target != null)
                {
                    if (target == closetarget)
                    {
                        target = close2target;
                    }
                    else
                    {
                        target = closetarget;
                    }
                }
                else
                {
                    target = closetarget;
                }
            }
            else
            {
                target = null;
            }
        }
        else if (targets.Count == 1)
        {
            target = targets[0];
        }
        else
        {
            target = null;
        }
    }


    void Start()
    {
        Velocity = Vector3.zero;
        status = GetComponent<AugumentStatus>();
    }

    void Update()
    {
        ToTargetdist();

        // ロック条件: marktargetがロック範囲内かつ視野内
        if (target != null)
        {
            if (Vector3.Distance(transform.position, target.transform.position) < lockRange)
            {
                if (!lockon)
                {
                    nextMissileTimeA = Time.time + 0.5f;
                    nextMissileTimeB = Time.time + 0.8f;
                }
                lockon = true;
                lastDirToTarget = (target.transform.position - transform.position).normalized;
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



        if (lockon && Time.time >= nextFireTime &&
             isgun)
        {
            FireGun();
            nextFireTime = Time.time + fireRate;
        }

        if (lockon && Time.time >= nextMissileTimeA &&
            ismissile)
        {
            FireMissile();
            nextMissileTimeA = Time.time + missileCooldown;
            nextMissileTimeB += 0.3f;
        }

        if (lockon && Time.time >= nextMissileTimeB &&
            ismissile)
        {
            FireMissile();
            nextMissileTimeB = Time.time + missileCooldown;
            nextMissileTimeA += 0.3f;
        }
    }

    void FireGun()
    {

        //ばらつき
        Vector3 spread = Random.insideUnitCircle * Mathf.Tan(spreadAngle * Mathf.Deg2Rad);
        Vector3 shootDirection = (lastDirToTarget + spread).normalized;
        Vector3 initvelocity = Velocity + shootDirection * bulletSpeed;
        GameObject bullet = bulletpool.bulletpull(1f, gunMuzzle.position, initvelocity, 3f);
    }

    void FireMissile()
    {
        Vector3 initvelocity = Velocity + lastDirToTarget.normalized * bulletSpeed;
        GameObject MSL = bulletpool.missilepull(missileHardpoint.position, initvelocity, 10f);
        Missile missile = MSL.GetComponent<Missile>();

        if (missile != null && target != null)
        {
            missile.target = target.transform;

        }
    }
}
