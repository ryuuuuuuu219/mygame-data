using System.Collections.Generic;
using UnityEngine;

public class AirBattleshipBase : MonoBehaviour
{
    static readonly Vector3 DefaultCoreLocalPosition = new(-1.65999997f, 3.71000004f, -6.23999977f);
    static readonly Vector3 DefaultCoreLocalScale = new(2.387062949f, 0.437062949f, 0.194628254f);
    static readonly Vector3 DefaultTurretLocalScale = new(0.437062949f, 0.437062949f, 0.194628254f);

    [Header("Core Block")]
    public GameObject coreBlock;
    public Vector3 coreBlockLocalPosition = DefaultCoreLocalPosition;
    public Vector3 coreBlockLocalScale = DefaultCoreLocalScale;
    public float coreBlockHp = 5000f;

    [Header("Manual Turrets")]
    public AirBattleshipTurretMount[] manualTurrets;

    [Header("Turret Common")]
    public Vector3 turretLocalScale = DefaultTurretLocalScale;

    [Header("Deck VLS")]
    public AirBattleshipTurretPrefabRef deckVlsPrefab = new() { prefabTypeName = "SAM" };
    public float deckVlsRange = 2000f;
    public float deckVlsRiseDistance = 160f;
    public float deckVlsRiseSpeed = 80f;
    public int deckVlsColumns = 4;
    public int deckVlsRows = 8;
    public Vector3 deckVlsFirstColumnCenter = new(-1.65999997f, 2f, -7f);
    public float deckVlsColumnSpacingX = 1f;
    public float deckVlsRowSpacingZ = 1f;
    public float deckVlsMinZ = -13.4f;

    [Header("Side Turrets")]
    public AirBattleshipTurretPrefabRef sideTurretPrefab = new() { prefabTypeName = "AA_GUN" };
    public float sideTurretGunRange = 2800f;
    public int sideTurretRows = 8;
    public float portSideX = 1.89f;
    public float starboardSideX = -5.35f;
    public float[] sideTurretYPositions = { 1.2f, -1.2f };
    public float sideTurretRearZ = 0f;
    public float sideTurretFrontZ = -15f;

    SpawnPrefabRegistry prefabRegistry;
    ShipKinematics shipKinematics;
    MissileShooterGroupManager vlsGroupManager;
    readonly List<GameObject> deckVlsLaunchers = new();
    bool turretsAttached;

    public GameObject CoreBlock => coreBlock;

    void Awake()
    {
        prefabRegistry = FindAnyObjectByType<SpawnPrefabRegistry>();
        shipKinematics = GetComponent<ShipKinematics>();

        EnsureMissileShooterGroupManager();
        EnsureDamageRelay();
        EnsureCoreBlockEntity();
    }

    void Start()
    {
        EnsureCoreBlockEntity();
        AttachManualTurrets();
        AttachDeckVls();
        AttachSideTurrets();
        turretsAttached = true;
    }

    MissileShooterGroupManager EnsureMissileShooterGroupManager()
    {
        if (vlsGroupManager != null)
            return vlsGroupManager;

        vlsGroupManager = GetComponent<MissileShooterGroupManager>();
        if (vlsGroupManager == null)
            vlsGroupManager = gameObject.AddComponent<MissileShooterGroupManager>();

        return vlsGroupManager;
    }

    public GameObject EnsureCoreBlockEntity()
    {
        if (coreBlock == null)
        {
            coreBlock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            coreBlock.name = "CoreBlock";
            coreBlock.transform.SetParent(transform, false);
        }

        coreBlock.transform.localPosition = coreBlockLocalPosition;
        coreBlock.transform.localRotation = Quaternion.identity;
        coreBlock.transform.localScale = coreBlockLocalScale;
        SetupCoreBlockStatus(coreBlock);
        return coreBlock;
    }

    void EnsureDamageRelay()
    {
        if (!TryGetComponent(out AirBattleshipDamageRelay relay))
            relay = gameObject.AddComponent<AirBattleshipDamageRelay>();

        relay.battleshipBase = this;
    }

    void SetupCoreBlockStatus(GameObject target)
    {
        if (!target.TryGetComponent(out AugumentStatus status))
            status = target.AddComponent<AugumentStatus>();

        status.issortie = true;
        status.isVisible = true;
        status.isPlayer = false;
        status.missionObjective = true;
        status.isEnemy = true;
        if (status.waveID < 0)
            status.waveID = -1;
        status.lifeTime = 0f;
        status.preferInspectorStatus = true;

        if (status.CurrentStatus == null)
            status.CurrentStatus = new StatusTable();

        status.CurrentStatus.Get("Aircraft", "HP").value = coreBlockHp;
    }

    void AttachManualTurrets()
    {
        if (turretsAttached) return;
        if (manualTurrets == null) return;

        for (int i = 0; i < manualTurrets.Length; i++)
        {
            var turret = manualTurrets[i];
            if (turret == null) continue;

            AttachTurret(turret.prefabTypeName, turret.prefab, turret.localPosition, "ManualTurret");
        }
    }

    void AttachDeckVls()
    {
        if (turretsAttached) return;
        if (deckVlsColumns <= 0 || deckVlsRows <= 0) return;

        deckVlsLaunchers.Clear();
        float startX = deckVlsFirstColumnCenter.x - (deckVlsColumns - 1) * deckVlsColumnSpacingX * 0.5f;

        for (int column = 0; column < deckVlsColumns; column++)
        {
            for (int row = 0; row < deckVlsRows; row++)
            {
                float z = deckVlsFirstColumnCenter.z - deckVlsRowSpacingZ * row;
                if (z < deckVlsMinZ) continue;

                var position = new Vector3(
                    startX + deckVlsColumnSpacingX * column,
                    deckVlsFirstColumnCenter.y,
                    z);

                var attachedVls = AttachTurret(deckVlsPrefab.prefabTypeName, deckVlsPrefab.prefab, position, "DeckVLS");
                if (attachedVls != null)
                {
                    ConfigureVlsLauncher(attachedVls);
                    deckVlsLaunchers.Add(attachedVls);
                }
            }
        }

        RegisterDeckVlsLaunchers();
    }

    void RegisterDeckVlsLaunchers()
    {
        EnsureMissileShooterGroupManager().SetSingleGroup(deckVlsLaunchers);
    }

    void AttachSideTurrets()
    {
        if (turretsAttached) return;
        if (sideTurretRows <= 0 || sideTurretYPositions == null || sideTurretYPositions.Length == 0) return;

        for (int row = 0; row < sideTurretRows; row++)
        {
            float z = sideTurretRows == 1
                ? sideTurretRearZ
                : Mathf.Lerp(sideTurretRearZ, sideTurretFrontZ, row / (float)(sideTurretRows - 1));

            for (int yIndex = 0; yIndex < sideTurretYPositions.Length; yIndex++)
            {
                float y = sideTurretYPositions[yIndex];
                ConfigureGunTurret(AttachTurret(sideTurretPrefab.prefabTypeName, sideTurretPrefab.prefab, new Vector3(portSideX, y, z), "PortSideTurret"));
                ConfigureGunTurret(AttachTurret(sideTurretPrefab.prefabTypeName, sideTurretPrefab.prefab, new Vector3(starboardSideX, y, z), "StarboardSideTurret"));
            }
        }
    }

    void ConfigureVlsLauncher(GameObject launcher)
    {
        if (launcher == null) return;

        var missileShooter = EnsureVlsMissileShooter(launcher);
        missileShooter.launchDirectionOverride = Vector3.up;
        missileShooter.guidanceStartDelay = deckVlsRiseSpeed > 0f ? deckVlsRiseDistance / deckVlsRiseSpeed : 0f;
        missileShooter.guidanceStartSwitch = false;
        missileShooter.requireLineOfSight = false;
        missileShooter.minimumLaunchUpDot = 1f;
        missileShooter.missileBreakAngle = Mathf.Max(missileShooter.missileBreakAngle, 140f);

        if (launcher.TryGetComponent(out GroundAntiAirController controller))
        {
            controller.missileRange = deckVlsRange;
            controller.missileShooter = missileShooter;
            controller.SyncTargetSelectorRange();
        }

        ConfigureTargetSelectorRange(launcher, deckVlsRange);
    }

    EnemyMissileShooter EnsureVlsMissileShooter(GameObject launcher)
    {
        if (launcher.TryGetComponent(out EnemyMissileShooter missileShooter))
            return missileShooter;

        return launcher.AddComponent<EnemyMissileShooter>();
    }

    void ConfigureGunTurret(GameObject turret)
    {
        if (turret == null) return;

        if (turret.TryGetComponent(out GroundAntiAirController controller))
            controller.gunRange = sideTurretGunRange;

        ConfigureTargetSelectorRange(turret, sideTurretGunRange);
    }

    static void ConfigureTargetSelectorRange(GameObject turret, float range)
    {
        if (turret == null || !turret.TryGetComponent(out EnemyTargetSelector selector)) return;

        selector.detectRange = Mathf.Max(selector.detectRange, range);
        selector.lockRange = Mathf.Max(selector.lockRange, range);
    }

    GameObject AttachTurret(string prefabTypeName, GameObject prefab, Vector3 localPosition, string fallbackName)
    {
        var resolvedPrefab = prefab != null ? prefab : GetPrefabByType(prefabTypeName);
        var attachedTurret = AttachPart(resolvedPrefab, localPosition, turretLocalScale, fallbackName, true);

        if (attachedTurret != null && shipKinematics != null)
        {
            shipKinematics.RegisterTurret(attachedTurret);
        }

        RegisterTurretAsEnemyPart(attachedTurret);

        return attachedTurret;
    }

    void RegisterTurretAsEnemyPart(GameObject turret)
    {
        if (turret == null) return;
        if (!turret.TryGetComponent(out AugumentStatus status)) return;

        status.issortie = true;
        status.isVisible = true;
        status.isPlayer = false;
        status.isEnemy = true;
        status.missionObjective = false;
        status.lifeTime = 0f;
        status.waveID = -1;

        ObjectManager.Instance?.RegisterEnemy(turret, status.waveID);
    }

    GameObject AttachPart(GameObject prefab, Vector3 localPosition, Vector3 localScale, string fallbackName, bool useTurretDirection)
    {
        if (prefab == null) return null;

        var part = Instantiate(prefab, transform);
        part.name = string.IsNullOrEmpty(prefab.name) ? fallbackName : prefab.name;
        part.transform.localPosition = localPosition;
        part.transform.localRotation = useTurretDirection ? Quaternion.LookRotation(Vector3.back, Vector3.up) : Quaternion.identity;
        part.transform.localScale = localScale;
        part.SetActive(true);
        return part;
    }

    GameObject GetPrefabByType(string prefabTypeName)
    {
        if (prefabRegistry == null) return null;
        return prefabRegistry.GetPrefab(prefabTypeName);
    }
}

[System.Serializable]
public class AirBattleshipTurretPrefabRef
{
    public string prefabTypeName;
    public GameObject prefab;
}

[System.Serializable]
public class AirBattleshipTurretMount
{
    public string prefabTypeName;
    public GameObject prefab;
    public Vector3 localPosition;
}
