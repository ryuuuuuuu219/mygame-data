using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TutorialSceneBootstrap : MonoBehaviour
{
    const string PreScene = "preM00";
    const string MainScene = "M00";
    const string UavStorageLauncherGuideText =
        "マルチロックミサイルを試してみましょう\n複数のUAVを視界に入れて、一斉発射できます。";

    static bool registered;
    string bootSceneName;
    TMP_FontAsset resolvedFont;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Initialize()
    {
        if (!registered)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            registered = true;
        }

        Bootstrap(SceneManager.GetActiveScene());
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Bootstrap(scene);
    }

    static void Bootstrap(Scene scene)
    {
        if (scene.name != PreScene && scene.name != MainScene) return;
        if (FindFirstObjectByType<TutorialSceneBootstrap>() != null) return;

        var root = new GameObject("TutorialSceneBootstrap");
        var bootstrap = root.AddComponent<TutorialSceneBootstrap>();
        bootstrap.bootSceneName = scene.name;
    }

    IEnumerator Start()
    {
        yield return null;
        Setup(bootSceneName);
    }

    void Setup(string sceneName)
    {
        resolvedFont = ResolveNotoFont();

        if (sceneName == PreScene)
            SetupPreM00();
        else if (sceneName == MainScene)
            SetupM00();
    }

    void SetupPreM00()
    {
        DisablePreM00SceneObjects();
        Canvas canvas = CreateTutorialOverlayCanvas("PreM00InputCheckCanvas");

        TextMeshProUGUI inputText = CreateOverlayText(
            "TutorialInputVisualizerText",
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 0f),
            new Vector2(460f, 260f),
            30f,
            TextAlignmentOptions.Center);

        RectTransform leftStick = CreateStickMarker(canvas, "TutorialInputLeftStick", new Vector2(-120f, -78f));
        RectTransform rightStick = CreateStickMarker(canvas, "TutorialInputRightStick", new Vector2(120f, -78f));

        var inputVisualizer = inputText.gameObject.AddComponent<InputTextUI>();
        inputVisualizer.text = inputText;
        inputVisualizer.Lstick = leftStick;
        inputVisualizer.Rstick = rightStick;

        TextMeshProUGUI[] checkTexts = CreatePreM00GuideTexts(canvas);

        TextMeshProUGUI summaryText = CreateOverlayText(
            "TutorialInputCheckSummaryText",
            new Vector2(0.5f, 0.14f),
            new Vector2(0f, 0f),
            new Vector2(980f, 190f),
            22f,
            TextAlignmentOptions.TopLeft);

        var controller = gameObject.AddComponent<TutorialInputCheckController>();
        controller.checklistText = summaryText;
        controller.checkTexts = checkTexts;
        controller.nextSceneName = MainScene;
        controller.autoLoadNextScene = false;
    }

    void DisablePreM00SceneObjects()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (Canvas canvas in canvases)
        {
            if (canvas != null)
                canvas.gameObject.SetActive(false);
        }

        DisableSpawnManagers();

        WeaponSystem[] weaponSystems = FindObjectsByType<WeaponSystem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (WeaponSystem weaponSystem in weaponSystems)
        {
            if (weaponSystem != null)
                weaponSystem.enabled = false;
        }
    }

    Canvas CreateTutorialOverlayCanvas(string objectName)
    {
        var obj = new GameObject(objectName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var created = obj.GetComponent<Canvas>();
        created.renderMode = RenderMode.ScreenSpaceOverlay;
        created.sortingOrder = short.MaxValue;

        var scaler = obj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        return created;
    }

    TextMeshProUGUI[] CreatePreM00GuideTexts(Canvas canvas)
    {
        string[] labels =
        {
            "左スティック左右: ロール",
            "左スティック上下: ピッチ",
            "右スティック: 視点移動",
            "R1: 加速",
            "L1: 減速",
            "R2/L2: ヨー",
            "左スティック押し込み + 減速: 機動制限解除",
            "△: 目標切替",
            "○: ミサイル / 選択兵装発射",
            "×: 機銃",
            "□: 兵装切替",
        };

        Vector2[] positions =
        {
            new Vector2(-610f, 170f),
            new Vector2(-610f, 105f),
            new Vector2(610f, 170f),
            new Vector2(610f, 105f),
            new Vector2(-610f, 40f),
            new Vector2(610f, 40f),
            new Vector2(-610f, -25f),
            new Vector2(610f, -25f),
            new Vector2(610f, -90f),
            new Vector2(610f, -155f),
            new Vector2(-610f, -90f),
        };

        var texts = new TextMeshProUGUI[labels.Length];
        for (int i = 0; i < labels.Length; i++)
        {
            texts[i] = CreateOverlayText(
                "TutorialInputGuideText" + i,
                new Vector2(0.5f, 0.5f),
                positions[i],
                new Vector2(440f, 48f),
                24f,
                positions[i].x < 0f ? TextAlignmentOptions.Right : TextAlignmentOptions.Left);

            texts[i].text = labels[i];
            CreateGuideLine(canvas, "TutorialInputGuideLine" + i, positions[i], Vector2.zero);
        }

        return texts;
    }

    RectTransform CreateStickMarker(Canvas canvas, string objectName, Vector2 anchoredPosition)
    {
        var obj = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.transform.SetParent(canvas.transform, false);

        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(24f, 24f);

        var image = obj.GetComponent<Image>();
        image.color = new Color(0.2f, 0.85f, 1f, 0.9f);
        image.raycastTarget = false;
        return rect;
    }

    void CreateGuideLine(Canvas canvas, string objectName, Vector2 from, Vector2 to)
    {
        var obj = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.transform.SetParent(canvas.transform, false);

        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        Vector2 delta = to - from;
        rect.anchoredPosition = from + delta * 0.5f;
        rect.sizeDelta = new Vector2(delta.magnitude, 3f);
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

        var image = obj.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.35f);
        image.raycastTarget = false;
    }

    void SetupM00()
    {
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
            new Vector2(0f, 0f),
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
            new Vector2(0f, 0f),
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

        var switchSpawner = gameObject.AddComponent<TutorialTargetSwitchSpawner>();
        switchSpawner.player = player;
        switchSpawner.enemyPrefabA = CreateTargetSwitchPrefab(player.transform.position + player.forward * 700f, player);
        switchSpawner.missionObjectiveA = false;
        switchSpawner.scoreZero = true;

        Vector3 twoOClockDirection = Quaternion.Euler(0f, 60f, 0f) * player.forward;
        switchSpawner.enemyPrefabB = CreateTargetSwitchPrefab(player.transform.position + twoOClockDirection * 700f, player);
        switchSpawner.missionObjectiveB = true;
        switchSpawner.scoreZero = true;
    }

    Transform FindPlayer()
    {
        GameObject playerObject = GameObject.Find("Player");
        if (playerObject != null) return playerObject.transform;

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

        if (ObjectManager.Instance != null)
            ObjectManager.Instance.RegisterEnemy(enemy, -1);

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
