using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class M00TutorialBootstrap : MonoBehaviour
{
    const string UavStorageLauncherGuideText =
        "UAV発着場\nマルチロックミサイルを試してみましょう\n複数のUAVを視界に入れて、一斉発射できます。";
    static readonly string[][] BasicControlsGuideTexts =
    {
        new[]
        {
            "ピッチ",
            "左スティック上下で機首を上げ下げします。",
            "機首を上げると上昇、下げると降下します。"
        },
        new[]
        {
            "ロール",
            "左スティック左右で機体を傾けます。",
            "傾けた状態で機首を上げると旋回できます。"
        },
        new[]
        {
            "ヨー",
            "R2 / L2で機首を左右に向けます。",
            "細かい向きの調整に使います。"
        },
        new[]
        {
            "加減速",
            "R1で加速、L1で減速します。",
            "速度が落ちすぎると曲がりにくくなります。"
        },
        new[]
        {
            "機動制限解除",
            "左スティック押し込み + 減速で機動制限を解除します。",
            "急旋回できますが、速度低下に注意してください。"
        }
    };
    TMP_FontAsset resolvedFont;

    IEnumerator Start()
    {
        yield return null;
        Setup();
    }

    void Setup()
    {
        resolvedFont = ResolveNotoFont();
        DisableSpawnManagers();

        Transform player = FindPlayer();
        if (player != null)
        {
            player.position = new Vector3(0f, 1500f, 0f);
            player.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);

            if (player.TryGetComponent(out Rigidbody rb))
                rb.linearVelocity = player.forward * 180f;

            PrepareTutorialWeapons(player);
        }

        TextMeshProUGUI reminder = CreateOverlayText(
            "TutorialReminderText",
            new Vector2(0.5f, 0.78f),
            Vector2.zero,
            new Vector2(900f, 120f),
            30f,
            TextAlignmentOptions.Center);

        var zeroScore = gameObject.AddComponent<TutorialZeroScoreEnemies>();
        zeroScore.keepApplying = true;

        var flightArea = gameObject.AddComponent<TutorialBasicFlightArea>();
        flightArea.player = player;
        flightArea.areaText = CreateOverlayText(
            "TutorialAreaText",
            new Vector2(0.5f, 0.9f),
            Vector2.zero,
            new Vector2(900f, 80f),
            24f,
            TextAlignmentOptions.Center);
        flightArea.center = new Vector3(0f, 1500f, 0f);
        flightArea.radius = 700f;
        flightArea.markerXZ = new Vector2(0f, 0f);
        flightArea.markerY = 10000f;
        flightArea.markerHeight = 20000f;
        flightArea.markerText = "基本操作確認エリア";
        flightArea.markerTextFont = resolvedFont;
        flightArea.markerTextColor = new Color(1f, 0.35f, 0.25f, 1f);

        CreateBasicControlsDetailArea(player, flightArea);

        var reminderHud = gameObject.AddComponent<TutorialFlightReminderHUD>();
        reminderHud.player = player;
        reminderHud.reminderText = reminder;

        GameObject gunTarget = CreateTutorialEnemy(
            "TutorialAttractorEnemy",
            new Vector3(800f, 1500f, -750f),
            missionTarget: false);

        var attractor = gunTarget.AddComponent<TutorialAttitudeAttractor>();
        attractor.player = player;
        attractor.range = 900f;
        attractor.strength = 0.22f;

        AddAreaMarker(
            player,
            attractor.transform.position,
            attractor.range,
            "機銃補助エリア\n誘引付きの標的");

        CreateUavTrainingArea(player, CreateTargetSwitchPrefab(new Vector3(-800f, 1500f, -950f), player));

        if (player == null)
            return;

        var switchSpawner = gameObject.AddComponent<TutorialTargetSwitchSpawner>();
        switchSpawner.player = player;
        switchSpawner.enemyPrefabA = CreateTargetSwitchPrefab(player.position + player.forward * 700f, player);
        switchSpawner.missionObjectiveA = false;
        switchSpawner.scoreZero = true;

        Vector3 twoOClockDirection = Quaternion.Euler(0f, 60f, 0f) * player.forward;
        switchSpawner.enemyPrefabB = CreateTargetSwitchPrefab(player.position + twoOClockDirection * 700f, player);
        switchSpawner.missionObjectiveB = true;
        switchSpawner.scoreZero = true;
    }

    Transform FindPlayer()
    {
        GameObject playerObject = GameObject.Find("Player");
        if (playerObject != null)
            return playerObject.transform;

        var status = FindFirstObjectByType<AugumentStatus>();
        return status != null && status.isPlayer ? status.transform : null;
    }

    void PrepareTutorialWeapons(Transform player)
    {
        if (player == null) return;
        WeaponSystem weaponSystem = player.GetComponent<WeaponSystem>();
        if (weaponSystem == null) return;

        weaponSystem.maxnAAM = Mathf.Max(weaponSystem.maxnAAM, 4);
        weaponSystem.currentnAAM = Mathf.Max(weaponSystem.currentnAAM, weaponSystem.maxnAAM);
        while (weaponSystem.multiTimers.Count < weaponSystem.maxnAAM)
            weaponSystem.multiTimers.Add(0f);
    }

    static string BuildGuideText(string[] guideTexts)
    {
        if (guideTexts == null)
            return "";

        return string.Join("\n", guideTexts);
    }

    void CreateBasicControlsDetailArea(Transform player, TutorialBasicFlightArea parentArea)
    {
        if (parentArea == null || BasicControlsGuideTexts.Length == 0)
            return;

        var detailRoot = new GameObject("TutorialBasicControlsDetailAreas");
        detailRoot.transform.SetParent(parentArea.transform, false);

        float placementRadius = parentArea.radius * 0.5f;
        float detailRadius = Mathf.Min(parentArea.radius * 0.22f, placementRadius * 0.45f);
        float angleStep = 360f / BasicControlsGuideTexts.Length;

        for (int i = 0; i < BasicControlsGuideTexts.Length; i++)
        {
            Vector3 offset = Quaternion.Euler(0f, angleStep * i, 0f) * Vector3.forward * placementRadius;
            Vector3 detailCenter = parentArea.center + offset;
            var detailObject = new GameObject($"TutorialBasicControlsDetailArea_{i + 1:00}");
            detailObject.transform.SetParent(detailRoot.transform, false);

            var detailArea = detailObject.AddComponent<TutorialBasicFlightArea>();
            detailArea.player = player;
            detailArea.center = detailCenter;
            detailArea.radius = detailRadius;
            detailArea.markerXZ = new Vector2(detailCenter.x, detailCenter.z);
            detailArea.markerY = parentArea.markerY;
            detailArea.markerHeight = parentArea.markerHeight;
            detailArea.markerColor = new Color(1f, 0.45f, 0.05f, 0.13f);
            detailArea.markerText = BuildGuideText(BasicControlsGuideTexts[i]);
            detailArea.markerTextFont = resolvedFont;
            detailArea.markerTextSize = 8f;
            detailArea.markerTextColor = new Color(1f, 0.95f, 0.65f, 1f);
        }
    }

    TutorialBasicFlightArea AddAreaMarker(
        Transform player,
        Vector3 center,
        float radius,
        string markerText,
        float markerTextSize = 12f,
        Color? markerTextColor = null)
    {
        var markerXZ = new Vector2(center.x, center.z);
        var area = gameObject.AddComponent<TutorialBasicFlightArea>();
        area.player = player;
        area.center = center;
        area.radius = radius;
        area.markerXZ = markerXZ;
        area.markerY = 10000f;
        area.markerHeight = 20000f;
        area.markerText = markerText;
        area.markerTextFont = resolvedFont;
        area.markerTextSize = markerTextSize;
        area.markerTextColor = markerTextColor ?? new Color(1f, 0.35f, 0.25f, 1f);
        return area;
    }

    TextMeshProUGUI CreateOverlayText(
        string objectName,
        Vector2 anchor,
        Vector2 anchoredPosition,
        Vector2 size,
        float fontSize,
        TextAlignmentOptions alignment)
    {
        Canvas canvas = GetOrCreateOverlayCanvas();
        var obj = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        obj.transform.SetParent(canvas.transform, false);

        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        var text = obj.GetComponent<TextMeshProUGUI>();
        if (resolvedFont != null)
            text.font = resolvedFont;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        text.text = "";
        return text;
    }

    Canvas GetOrCreateOverlayCanvas()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (Canvas canvas in canvases)
        {
            if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return canvas;
        }

        var obj = new GameObject("TutorialOverlayCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var created = obj.GetComponent<Canvas>();
        created.renderMode = RenderMode.ScreenSpaceOverlay;
        created.sortingOrder = short.MaxValue;

        var scaler = obj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        return created;
    }

    GameObject CreateTutorialEnemy(string objectName, Vector3 position, bool missionTarget)
    {
        var enemy = GameObject.CreatePrimitive(PrimitiveType.Cube);
        enemy.name = objectName;
        enemy.transform.position = position;
        enemy.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
        enemy.transform.localScale = Vector3.one;

        var rb = enemy.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.mass = 1f;

        var status = enemy.AddComponent<AugumentStatus>();
        status.isEnemy = true;
        status.isPlayer = false;
        status.issortie = true;
        status.missionObjective = missionTarget;
        status.lifeTime = 0f;
        status.hp = 400f;
        status.maxhp = 400f;
        status.SetScoreReward(0f);

        var orbit = enemy.AddComponent<Orbitcruise>();
        orbit.center = position;
        orbit.orbitRadius = 280f;
        orbit.cruiseThrottle = 0.8f;
        orbit.lowSpeedThrottle = 1.5f;

        ObjectManager.Instance?.RegisterEnemy(enemy, -1);
        return enemy;
    }

    GameObject CreateTargetSwitchPrefab(Vector3 position, Transform player)
    {
        var prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        prefab.name = "TutorialTargetSwitchEnemyPrefab";
        prefab.transform.localScale = Vector3.one;
        prefab.SetActive(false);
        prefab.transform.position = position;
        prefab.transform.rotation = player != null
            ? player.rotation
            : Quaternion.LookRotation(Vector3.forward, Vector3.up);

        var rb = prefab.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.linearVelocity = prefab.transform.forward * ResolvePlayerInitialSpeed(player);

        var status = prefab.AddComponent<AugumentStatus>();
        status.isEnemy = true;
        status.issortie = true;
        status.hp = 300f;
        status.maxhp = 300f;
        status.lifeTime = 0f;
        status.SetScoreReward(0f);
        ApplyPlayerFlightStatus(status, player);

        var orbit = prefab.AddComponent<Orbitcruise>();
        orbit.center = position;
        orbit.useStartDistanceAsRadius = true;
        orbit.cruiseThrottle = 1f;
        orbit.lowSpeedThrottle = 2.5f;
        return prefab;
    }

    float ResolvePlayerInitialSpeed(Transform player)
    {
        if (player != null && player.TryGetComponent(out Rigidbody playerRb))
        {
            float speed = playerRb.linearVelocity.magnitude;
            if (speed > 0.1f)
                return speed;
        }

        if (player != null && player.TryGetComponent(out AircraftController aircraft) && aircraft.maxSpeed > 0f)
            return Mathf.Min(aircraft.maxSpeed, 180f);

        return 180f;
    }

    void ApplyPlayerFlightStatus(AugumentStatus targetStatus, Transform player)
    {
        if (targetStatus == null)
            return;

        var targetTable = new StatusTable();
        targetStatus.CurrentStatus = targetTable;

        CopyFlightStatFromPlayer(targetTable, player, "機動性(ピッチ)", 4f);
        CopyFlightStatFromPlayer(targetTable, player, "機動性(ロール)", 4f);
        CopyFlightStatFromPlayer(targetTable, player, "機動性(ヨー)", 4f);
        CopyFlightStatFromPlayer(targetTable, player, "加速度", 100f);
        CopyFlightStatFromPlayer(targetTable, player, "最高速度", 300f);
    }

    void CopyFlightStatFromPlayer(StatusTable targetTable, Transform player, string key, float fallback)
    {
        ref float targetValue = ref targetTable.GetVar(key);
        targetValue = ResolvePlayerFlightStat(player, key, fallback);
    }

    float ResolvePlayerFlightStat(Transform player, string key, float fallback)
    {
        if (player == null)
            return fallback;

        if (player.TryGetComponent(out AugumentStatus playerStatus) && playerStatus.IsInitialized)
        {
            playerStatus.altGetVar(key, out float statusValue);
            if (statusValue > 0f)
                return statusValue;
        }

        if (player.TryGetComponent(out AircraftController aircraft))
        {
            switch (key)
            {
                case "機動性(ピッチ)": return aircraft.torquePower.x > 0f ? aircraft.torquePower.x : fallback;
                case "機動性(ロール)": return aircraft.torquePower.y > 0f ? aircraft.torquePower.y : fallback;
                case "機動性(ヨー)": return aircraft.torquePower.z > 0f ? aircraft.torquePower.z : fallback;
                case "加速度": return aircraft.thrustPower > 0f ? aircraft.thrustPower : fallback;
                case "最高速度": return aircraft.maxSpeed > 0f ? aircraft.maxSpeed : fallback;
            }
        }

        return fallback;
    }

    void CreateUavTrainingArea(Transform player, GameObject uavPrefab)
    {
        var storage = GameObject.CreatePrimitive(PrimitiveType.Cube);
        storage.name = "TutorialUavStorageLauncher";
        storage.transform.position = new Vector3(-800f, 240f, -750f);
        storage.transform.localScale = new Vector3(80f, 24f, 80f);
        storage.SetActive(true);

        MeshRenderer renderer = storage.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.enabled = true;
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = new Color(0f, 0.85f, 1f, 1f);
        }

        var rb = storage.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;

        var status = storage.AddComponent<AugumentStatus>();
        status.isEnemy = true;
        status.isPlayer = false;
        status.issortie = true;
        status.isVisible = true;
        status.missionObjective = false;
        status.waveID = -1;
        status.lifeTime = 0f;
        status.hp = 2000f;
        status.maxhp = 2000f;
        status.SetScoreReward(0f);

        ObjectManager.Instance?.RegisterEnemy(storage, status.waveID);

        AddAreaMarker(
            player,
            storage.transform.position,
            520f,
            UavStorageLauncherGuideText,
            10f,
            Color.cyan);

        var spawner = gameObject.AddComponent<TutorialUavTrainingSpawner>();
        spawner.player = player;
        spawner.enemyPrefab = uavPrefab;
        spawner.center = new Vector3(-800f, 1500f, -950f);
        spawner.progressText = CreateOverlayText(
            "TutorialUavProgressText",
            new Vector2(0.5f, 0.84f),
            Vector2.zero,
            new Vector2(1000f, 80f),
            22f,
            TextAlignmentOptions.Center);
    }

    void DisableSpawnManagers()
    {
        var managers = FindObjectsByType<SpawnTableManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var manager in managers)
        {
            if (manager != null)
                manager.enabled = false;
        }
    }

    TMP_FontAsset ResolveNotoFont()
    {
#if UNITY_EDITOR
        TMP_FontAsset asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/NotoSansJP-Regular SDF.asset");
        if (asset != null)
            return asset;
#endif

        var uiTexts = FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var text in uiTexts)
        {
            if (text != null && text.font != null && text.font.name.Contains("NotoSansJP-Regular SDF"))
                return text.font;
        }

        var worldTexts = FindObjectsByType<TextMeshPro>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var text in worldTexts)
        {
            if (text != null && text.font != null && text.font.name.Contains("NotoSansJP-Regular SDF"))
                return text.font;
        }

        TMP_FontAsset resourceFont = Resources.Load<TMP_FontAsset>("NotoSansJP-Regular SDF");
        if (resourceFont != null)
            return resourceFont;

        return TMP_Settings.defaultFontAsset;
    }
}
