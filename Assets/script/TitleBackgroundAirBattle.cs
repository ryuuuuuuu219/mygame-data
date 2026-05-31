using UnityEngine;

public class TitleBackgroundAirBattle : MonoBehaviour
{
    [Header("World")]
    [SerializeField] bool generateMode1Terrain = true;
    [SerializeField] int terrainSize = 6000;
    [SerializeField] int heightmapResolution = 513;

    [Header("Aircraft")]
    [SerializeField] GameObject enemyAcePrefab;
    [SerializeField] GameObject allyAcePrefab;
    [SerializeField] int aircraftPerTeam = 5;
    [SerializeField] float spawnRadius = 750f;
    [SerializeField] float spawnAltitude = 900f;
    [SerializeField] float spawnAltitudeJitter = 220f;
    [SerializeField] float initialSpeed = 180f;

    [Header("Weapons")]
    [SerializeField] GameObject enemyBulletPrefab;
    [SerializeField] GameObject enemyMissilePrefab;
    [SerializeField] GameObject allyBulletPrefab;
    [SerializeField] GameObject allyMissilePrefab;
    [SerializeField] int bulletPoolSize = 160;
    [SerializeField] int missilePoolSize = 40;

    [Header("Camera")]
    [SerializeField] Camera titleCamera;
    [SerializeField] bool usePerspectiveCamera = true;
    [SerializeField] float cameraFieldOfView = 46f;
    [SerializeField] float cameraFarClipPlane = 8000f;

    [Header("Lighting")]
    [SerializeField] bool ensureDirectionalLight = true;
    [SerializeField] Vector3 directionalLightEulerAngles = new Vector3(45f, -30f, 0f);
    [SerializeField] float directionalLightIntensity = 1.1f;

    void Start()
    {
        EnsureObjectManager();
        EnsureWeaponPools();

        if (generateMode1Terrain)
        {
            EnsureMode1Terrain();
        }

        SpawnTeam(enemyAcePrefab, true);
        SpawnTeam(allyAcePrefab, false);
        EnsureDirectionalLight();
        ConfigureCamera();
    }

    void EnsureObjectManager()
    {
        if (ObjectManager.Instance != null) return;

        GameObject manager = new GameObject("TitleObjectManager");
        manager.AddComponent<ObjectManager>();
        ObjectManager.Instance?.UnregisterAlly(manager);
    }

    void EnsureWeaponPools()
    {
        if (FindFirstObjectByType<Gun_e_pool>() == null)
        {
            GameObject poolObject = new GameObject("TitleEnemyWeaponPool");
            Gun_e_pool pool = poolObject.AddComponent<Gun_e_pool>();
            pool.bulletPrefab = enemyBulletPrefab;
            pool.missilePrefab = enemyMissilePrefab;
            pool.poolSize1 = bulletPoolSize;
            pool.poolSize2 = missilePoolSize;
        }

        if (FindFirstObjectByType<Gun_p_pool>() == null)
        {
            GameObject poolObject = new GameObject("TitleAllyWeaponPool");
            Gun_p_pool pool = poolObject.AddComponent<Gun_p_pool>();
            pool.bulletPrefab = allyBulletPrefab;
            pool.missilePrefab = allyMissilePrefab;
            pool.poolSize1 = bulletPoolSize;
            pool.poolSize2 = missilePoolSize;
        }
    }

    void EnsureMode1Terrain()
    {
        if (FindFirstObjectByType<WorldGenerator>() != null) return;

        GameObject world = new GameObject("TitleMode1WorldGenerator");
        WorldGenerator generator = world.AddComponent<WorldGenerator>();
        generator.usePlayerPrefsSeed = false;
        generator.seed = 20260519;
        generator.terrainSize = terrainSize;
        generator.heightmapResolution = heightmapResolution;
        generator.heightScale = 420f;
        generator.waterHeight = 12f;
        generator.ceilingHeight = 2200f;
        generator.cloudCount = 8;
        generator.generateClouds = true;
        generator.generateOnStart = true;
    }

    void SpawnTeam(GameObject prefab, bool enemyTeam)
    {
        if (prefab == null) return;

        for (int i = 0; i < aircraftPerTeam; i++)
        {
            float angle = ((float)i / Mathf.Max(1, aircraftPerTeam)) * Mathf.PI * 2f;
            float side = enemyTeam ? 1f : -1f;
            Vector3 position = new Vector3(
                Mathf.Cos(angle) * spawnRadius * side,
                spawnAltitude + Random.Range(-spawnAltitudeJitter, spawnAltitudeJitter),
                Mathf.Sin(angle) * spawnRadius + side * 450f
            );

            Vector3 targetPoint = new Vector3(-position.x * 0.35f, spawnAltitude, -position.z * 0.35f);
            Quaternion rotation = Quaternion.LookRotation((targetPoint - position).normalized, Vector3.up);
            GameObject aircraft = Instantiate(prefab, position, rotation);
            aircraft.name = enemyTeam ? $"TitleEnemyAce_{i + 1:00}" : $"TitleAllyAce_{i + 1:00}";

            ConfigureAircraft(aircraft, enemyTeam, rotation * Vector3.forward * initialSpeed);
        }
    }

    void ConfigureAircraft(GameObject aircraft, bool enemyTeam, Vector3 initialVelocity)
    {
        if (aircraft.TryGetComponent(out Rigidbody rb))
        {
            rb.useGravity = false;
            rb.linearVelocity = initialVelocity;
        }

        if (aircraft.TryGetComponent(out AugumentStatus status))
        {
            status.isEnemy = enemyTeam;
            status.isPlayer = !enemyTeam;
            status.issortie = true;
            status.lifeTime = -1f;
            status.waveID = 0;
        }

        if (enemyTeam)
        {
            ObjectManager.Instance?.RegisterEnemy(aircraft, 0);
            FCS_e fcs = aircraft.GetComponent<FCS_e>();
            if (fcs != null)
            {
                fcs.detectRange = 2500f;
                fcs.lockRange = 1400f;
                fcs.gunRange = 700f;
                fcs.missileSpeed = 220f;
                fcs.missileCooldown = 8f;
                fcs.mslmaxspeed = 420f;
            }
        }
        else
        {
            ObjectManager.Instance?.RegisterAlly(aircraft);
            FCS_p fcs = aircraft.GetComponent<FCS_p>();
            if (fcs != null)
            {
                fcs.detectRange = 2500f;
                fcs.lockRange = 1400f;
                fcs.gunRange = 700f;
                fcs.missileSpeed = 220f;
                fcs.missileCooldown = 8f;
                fcs.mslmaxspeed = 420f;
            }
        }
    }

    void ConfigureCamera()
    {
        Camera cam = titleCamera != null ? titleCamera : Camera.main;
        if (cam == null) return;

        if (usePerspectiveCamera)
        {
            cam.orthographic = false;
            cam.fieldOfView = cameraFieldOfView;
        }

        cam.farClipPlane = cameraFarClipPlane;
    }

    void EnsureDirectionalLight()
    {
        if (!ensureDirectionalLight || FindFirstObjectByType<Light>() != null) return;

        GameObject lightObject = new GameObject("Title Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = directionalLightIntensity;
        light.transform.rotation = Quaternion.Euler(directionalLightEulerAngles);
    }

}
