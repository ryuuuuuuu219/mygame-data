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
        "\u30de\u30eb\u30c1\u30ed\u30c3\u30af\u30df\u30b5\u30a4\u30eb\u3092\u8a66\u3057\u3066\u307f\u307e\u3057\u3087\u3046\n\u8907\u6570\u306eUAV\u3092\u8996\u754c\u306b\u5165\u308c\u3066\u3001\u4e00\u6589\u767a\u5c04\u3067\u304d\u307e\u3059\u3002";

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

        CreateUavTrainingArea(player);

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

        CopyFlightStatFromPlayer(targetTable, player, "\u6a5f\u52d5\u6027(\u30d4\u30c3\u30c1)", 4f);
        CopyFlightStatFromPlayer(targetTable, player, "\u6a5f\u52d5\u6027(\u30ed\u30fc\u30eb)", 4f);
        CopyFlightStatFromPlayer(targetTable, player, "\u6a5f\u52d5\u6027(\u30e8\u30fc)", 4f);
        CopyFlightStatFromPlayer(targetTable, player, "\u52a0\u901f\u5ea6", 100f);
        CopyFlightStatFromPlayer(targetTable, player, "\u6700\u9ad8\u901f\u5ea6", 300f);
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
                case "\u6a5f\u52d5\u6027(\u30d4\u30c3\u30c1)": return aircraft.torquePower.x > 0f ? aircraft.torquePower.x : fallback;
                case "\u6a5f\u52d5\u6027(\u30ed\u30fc\u30eb)": return aircraft.torquePower.y > 0f ? aircraft.torquePower.y : fallback;
                case "\u6a5f\u52d5\u6027(\u30e8\u30fc)": return aircraft.torquePower.z > 0f ? aircraft.torquePower.z : fallback;
                case "\u52a0\u901f\u5ea6": return aircraft.thrustPower > 0f ? aircraft.thrustPower : fallback;
                case "\u6700\u9ad8\u901f\u5ea6": return aircraft.maxSpeed > 0f ? aircraft.maxSpeed : fallback;
            }
        }

        return fallback;
    }

    void CreateUavTrainingArea(Transform player)
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

        CreateWorldTutorialText(
            storage.transform,
            "TutorialUavStorageLauncherLabel",
            Vector3.up * 30f,
            player,
            UavStorageLauncherGuideText,
            10f,
            Color.cyan);
    }

    TextMeshPro CreateWorldTutorialText(
        Transform parent,
        string objectName,
        Vector3 localPosition,
        Transform player,
        string message,
        float fontSize,
        Color color)
    {
        var label = new GameObject(objectName);
        label.transform.SetParent(parent, false);
        label.transform.localPosition = localPosition;
        label.transform.localRotation = Quaternion.LookRotation(Vector3.back, Vector3.up);

        var yawToPlayer = label.AddComponent<TutorialTextYawToPlayer>();
        yawToPlayer.player = player;

        TextMeshPro text = label.AddComponent<TextMeshPro>();
        if (resolvedFont != null)
            text.font = resolvedFont;
        text.text = message;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        return text;
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
