using UnityEngine;
using System.Collections.Generic;

public class WeaponSystem : MonoBehaviour
{
    public enum WeaponMode
    {
        MSL,
        MultiAAM,
        UGB
    }

    [Header("Mode")]
    public WeaponMode mode = WeaponMode.MSL;

    // ========================
    // Gun
    [Header("Gun Settings")]
    public int maxBullets;
    public int currentBullets;
    public Transform gunMuzzle;
    public float bulletSpeed;
    public float fireRate;
    public float spreadAngle = 2f;
    public float gunPower;
    public float gunSize;
    float nextFireTime;

    // ========================
    // Missile
    [Header("Missile Settings")]
    public int maxMissiles;
    public int currentMissiles;
    public Transform missileHardpoint;
    public float missileSpeed;
    public float missilelifeTime;
    public float missileCooldown;

    public float mslPower;
    public float mslacceleration;
    public float mslmaxspeed;
    public float mslturnRate;
    public float mslbreakAngle;
    public float mslProportionalConstant;
    public bool mslFixedTarget;

    public float missileTimerA;
    public float missileTimerB;

    // ========================
    // Multi AAM
    [Header("Multi AAM")]
    public int maxnAAM;
    public int currentnAAM;
    public float multiCooldown = 5f;
    public List<float> multiTimers = new();

    // ========================
    // UGB
    [Header("UGB")]
    public GameObject UGBPrefab;
    public int maxUGB;
    public int currentUGB;
    public float damageRadius;
    public float damage;
    public float proximityRadius;
    public bool useProximityFuse = true;
    public float ugbTimer;
    public float ugbcooldown = 2f;

    // ========================
    Rigidbody rb;
    [SerializeField] Gun_p_pool bulletpool;
    public DebugHUD debugHUD;
    public AugumentStatus status;

    bool systemReady;    // ステータス・初期化
    bool inputSafe;      // 押しっぱなし解除
    bool timeSafe;       // ロード後猶予
    float inputLockTimer=0.25f;

    // ========================
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        status = GetComponent<AugumentStatus>();

        if (status.IsInitialized)
            InitFromStatus();
        else
            status.OnInitialized += InitFromStatus;
    }

    // ========================
    void InitFromStatus()
    {
        float dummy;

        // --- Missile ---
        status.altGetVar("ミサイル：弾数", out dummy);
        maxMissiles = currentMissiles = (int)dummy;

        status.altGetVar("ミサイル：装填時間", out missileCooldown);
        status.altGetVar("ミサイル：初速", out missileSpeed);
        status.altGetVar("ミサイル：飛翔時間", out missilelifeTime);
        status.altGetVar("ミサイル：威力", out mslPower);
        status.altGetVar("ミサイル：加速度", out mslacceleration);
        status.altGetVar("ミサイル：最高速", out mslmaxspeed);
        status.altGetVar("ミサイル：誘導力", out mslturnRate);
        status.altGetVar("ミサイル：誘導象限", out mslbreakAngle);
        status.altGetVar("ミサイル：比例航法定数", out mslProportionalConstant);
        status.altGetVar("ミサイル：誘導目標の固定", out dummy);
        mslFixedTarget = dummy >= 1f;

        // --- Gun ---
        status.altGetVar("銃弾：初速", out bulletSpeed);
        status.altGetVar("銃弾：発射レート", out fireRate);
        fireRate = 1f / fireRate;
        status.altGetVar("銃弾：威力", out gunPower);
        status.altGetVar("銃弾：当たり判定サイズ", out gunSize);

        status.altGetVar("銃弾：弾数", out dummy);
        maxBullets = currentBullets = (int)dummy;

        // --- Multi ---
        status.altGetVar("長射程マルチロックミサイル：弾数", out dummy);
        maxnAAM = currentnAAM = (int)dummy;
        multiTimers.Clear();
        for (int i = 0; i < maxnAAM; i++) multiTimers.Add(0f);

        // --- UGB ---
        status.altGetVar("UGB：弾数", out dummy);
        maxUGB = currentUGB = (int)dummy;

        status.altGetVar("UGB：加害範囲", out damageRadius);
        status.altGetVar("UGB：威力", out damage);
        status.altGetVar("UGB：炸裂範囲", out proximityRadius);

        systemReady = true;
    }

    // ========================
    void Update()
    {
        var input = InputManager.Instance;

        if(inputLockTimer > 0f)
        {
            inputLockTimer -= Time.deltaTime;
        }
        else
        {
            timeSafe = true;
        }
        if (!input.fireMissile)
        {
            inputSafe = true;
        }

        if (!systemReady) return;
        if (!timeSafe) return;
        if (!inputSafe) return;

        if (input.changeWeapon)
        {
            mode = (WeaponMode)(((int)mode + 1) % 3);
            GeneratedAudioManager.Play(GeneratedAudioCue.WeaponChange);
        }

        if (input.fireGun)
            TryFireGun();

        if (input.fireMissile)
            FireByMode();

        missileTimerA = Mathf.Max(0, missileTimerA - Time.deltaTime);
        missileTimerB = Mathf.Max(0, missileTimerB - Time.deltaTime);
        ugbTimer = Mathf.Max(0, ugbTimer - Time.deltaTime);

        for (int i = 0; i < multiTimers.Count; i++)
            multiTimers[i] = Mathf.Max(0, multiTimers[i] - Time.deltaTime);
    }

    // ========================
    void TryFireGun()
    {
        if (Time.time < nextFireTime) return;
        if (currentBullets <= 0)
        {
            GeneratedAudioManager.Play(GeneratedAudioCue.Empty, null, 0.45f);
            return;
        }

        nextFireTime = Time.time + fireRate;
        currentBullets--;

        Vector3 spread = Random.insideUnitSphere * spreadAngle;
        Vector3 vel = rb.linearVelocity + transform.forward * bulletSpeed + spread;

        GameObject bullet = bulletpool.bulletpull(1f, gunMuzzle.position, vel, 3f);
        bullet.GetComponent<Gun_p>()?.Init(gunPower, gunSize);
        GeneratedAudioManager.Play(GeneratedAudioCue.GunFire, gunMuzzle.position, 0.55f);
    }

    // ========================
    void FireByMode()
    {
        switch (mode)
        {
            case WeaponMode.MSL:
                FireSingleMissile();
                break;
            case WeaponMode.MultiAAM:
                FireMultiMissile();
                break;
            case WeaponMode.UGB:
                FireUGB();
                break;
        }
    }

    // ========================
    void FireSingleMissile()
    {
        if (currentMissiles <= 0)
        {
            GeneratedAudioManager.Play(GeneratedAudioCue.Empty, null, 0.45f);
            return;
        }
        bool launched = false;

        if (missileTimerA <= 0)
        {
            LaunchMissile(0);
            missileTimerA = missileCooldown;
            launched = true;
        }
        else if (missileTimerB <= 0)
        {
            LaunchMissile(0);
            missileTimerB = missileCooldown;
            launched = true;
        }
        if (launched)
            currentMissiles--;
    }

    void FireMultiMissile()
    {
        if (currentnAAM <= 0)
        {
            GeneratedAudioManager.Play(GeneratedAudioCue.Empty, null, 0.45f);
            return;
        }
        int id = 0;
        bool launched = false;
        for (int i = 0; i < multiTimers.Count; i++)
        {
            if (multiTimers[i] > 0f) continue;
            if (id >= debugHUD.Lockedtargets.Count) break;

            LaunchMissile(id++);
            multiTimers[i] = multiCooldown;
            launched = true;
        }
        if (launched)
            currentnAAM--;
    }

    void LaunchMissile(int index)
    {

        Vector3 vel = rb.linearVelocity + transform.forward * missileSpeed;
        GameObject m = bulletpool.missilepull(missileHardpoint.position, vel, missilelifeTime);

        var missile = m.GetComponent<Missile_p>();
        if (missile == null) return;

        missile.missileInit(missileHardpoint.position, vel, missilelifeTime);
        missile.StatusSetting(mslPower, mslacceleration, mslmaxspeed,
                              mslturnRate, mslbreakAngle, mslProportionalConstant);
        missile.allowHeatRetargeting = !mslFixedTarget;

        if (index < debugHUD.Lockedtargets.Count)
            missile.target = debugHUD.Lockedtargets[index]?.transform;

        GeneratedAudioManager.Play(GeneratedAudioCue.MissileLaunch, missileHardpoint.position, 0.8f);
    }

    // ========================
    void FireUGB()
    {
        if (ugbTimer > 0) return;

        if (currentUGB <= 0)
        {
            GeneratedAudioManager.Play(GeneratedAudioCue.Empty, null, 0.45f);
            return;
        }
        currentUGB--;

        GameObject obj = Instantiate(UGBPrefab, missileHardpoint.position, Quaternion.identity);

        var bomb = obj.GetComponent<BombProjectile>();
        if (bomb != null)
        {
            bomb.damageRadius = damageRadius;
            bomb.damage = damage;
            bomb.Initialize(rb.linearVelocity);
        }

        ugbTimer = ugbcooldown;
        GeneratedAudioManager.Play(GeneratedAudioCue.BombDrop, missileHardpoint.position, 0.7f);
    }
}
