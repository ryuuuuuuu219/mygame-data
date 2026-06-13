using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class M00TutorialBootstrap : MonoBehaviour
{
    const float AreaSignTextSize = 50f;
    static readonly Color AreaSignTextColor = Color.cyan;
    const string TargetAcquisitionGuideText =
        "目標捕捉\nレーダーの赤い点がミッション目標です。\n画面端の矢印を追って、目標を視界に入れてください。";
    const string TargetLocatorGuideText =
        "ターゲットロケーター\n視野外の追跡目標は、画面端の緑円で方向を示します。\n円が上に来るようにロールして旋回してください。";
    const string HudUiGuideText =
        "HUD / UI確認\n緑枠は追跡対象、赤枠はロックオン完了です。\nTGT / HP / Next / Arry と機首・進行方向マーカーを確認してください。";
    const string MissileWeaponGuideText =
        "ミサイル / 兵装切替\n赤枠になったら○でミサイルを発射できます。\n□で兵装を切り替え、再装填と弾数も確認してください。";
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
        ApplyAreaSignStyle(flightArea);

        CreateBasicControlsDetailArea(player, flightArea);
        AddAreaMarker(
            player,
            new Vector3(0f, 1500f, 1500f),
            800f,
            TargetAcquisitionGuideText);
        AddAreaMarker(
            player,
            new Vector3(-1500f, 1500f, 500f),
            700f,
            TargetLocatorGuideText);
        AddAreaMarker(
            player,
            new Vector3(1500f, 1500f, 900f),
            650f,
            HudUiGuideText);
        AddAreaMarker(
            player,
            new Vector3(1500f, 1500f, -1500f),
            800f,
            MissileWeaponGuideText);

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

        CreateUavTrainingArea(player, CreateUavTrainingEnemyPrefab());

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
            ApplyAreaSignStyle(detailArea);
        }
    }

    void ApplyAreaSignStyle(TutorialBasicFlightArea area)
    {
        if (area == null)
            return;

        area.markerTextFont = resolvedFont;
        area.markerTextSize = AreaSignTextSize;
        area.markerTextColor = AreaSignTextColor;
    }

    TutorialBasicFlightArea AddAreaMarker(
        Transform player,
        Vector3 center,
        float radius,
        string markerText)
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
        ApplyAreaSignStyle(area);
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

    GameObject CreateUavTrainingEnemyPrefab()
    {
        var prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        prefab.name = "TutorialUavTrainingEnemyPrefab";
        prefab.transform.localScale = Vector3.one;
        prefab.SetActive(false);
        return prefab;
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
            UavStorageLauncherGuideText);

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
