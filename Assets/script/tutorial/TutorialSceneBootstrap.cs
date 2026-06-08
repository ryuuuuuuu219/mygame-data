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
        TextMeshProUGUI text = CreateOverlayText(
            "TutorialInputCheckText",
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 0f),
            new Vector2(860f, 640f),
            28f,
            TextAlignmentOptions.TopLeft);

        var controller = gameObject.AddComponent<TutorialInputCheckController>();
        controller.checklistText = text;
        controller.nextSceneName = MainScene;
        controller.autoLoadNextScene = true;
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

        CreateUavTrainingArea();

        var switchSpawner = gameObject.AddComponent<TutorialTargetSwitchSpawner>();
        switchSpawner.player = player;
        switchSpawner.enemyPrefabA = CreateTargetSwitchPrefab(player.transform.position + player.forward * 700f);
        switchSpawner.missionObjectiveA = false;
        switchSpawner.scoreZero = true;

        Vector3 twoOClockDirection = Quaternion.Euler(0f, 60f, 0f) * player.forward;
        switchSpawner.enemyPrefabB = CreateTargetSwitchPrefab(player.transform.position + twoOClockDirection * 700f);
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

    GameObject CreateTargetSwitchPrefab(Vector3 position)
    {
        var prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        prefab.name = "TutorialTargetSwitchEnemyPrefab";
        prefab.transform.localScale = Vector3.one;
        prefab.SetActive(false);
        prefab.transform.position = position;
        prefab.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);

        var rb = prefab.AddComponent<Rigidbody>();
        rb.useGravity = false;

        var status = prefab.AddComponent<AugumentStatus>();
        status.isEnemy = true;
        status.issortie = true;
        status.hp = 300f;
        status.maxhp = 300f;
        status.lifeTime = 0f;
        status.SetScoreReward(0f);

        var orbit = prefab.AddComponent<Orbitcruise>();
        orbit.center = position;
        orbit.useStartDistanceAsRadius = true;
        orbit.cruiseThrottle = 0.8f;
        return prefab;
    }

    void CreateUavTrainingArea()
    {
        var storage = GameObject.CreatePrimitive(PrimitiveType.Cube);
        storage.name = "TutorialUavStorageLauncher";
        storage.transform.position = new Vector3(-800f, 1500f, -750f);
        storage.transform.localScale = new Vector3(80f, 24f, 80f);
        storage.SetActive(true);

        var rb = storage.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;

        var status = storage.AddComponent<AugumentStatus>();
        status.isEnemy = true;
        status.issortie = true;
        status.missionObjective = false;
        status.lifeTime = 0f;
        status.hp = 2000f;
        status.maxhp = 2000f;
        status.SetScoreReward(0f);

        TextMeshPro text = storage.AddComponent<TextMeshPro>();
        if (resolvedFont != null)
            text.font = resolvedFont;
        text.text = "マルチロックミサイルを試してみましょう\n複数のUAVを視界に入れて、一斉発射できます。";
        text.fontSize = 36f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.cyan;
        text.transform.localPosition = Vector3.up * 120f;
        text.transform.rotation = Quaternion.LookRotation(Vector3.back, Vector3.up);
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
