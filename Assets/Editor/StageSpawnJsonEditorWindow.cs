using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StageSpawnJsonEditorWindow : EditorWindow
{
    const string JsonAssetPath = "Assets/StreamingAssets/stage_spawns.json";

    StageRoot root;
    string jsonPath;
    int selectedStageIndex;
    int selectedWaveIndex;
    int selectedEnemyIndex;
    Vector2 stageScroll;
    Vector2 detailScroll;
    bool isDirty;

    static readonly string[] PlacementModes = { "fixed", "terrainRandom" };
    static readonly Regex EnemyNameRegex = new(@"beforeName\s*=\s*""([^""]+)""", RegexOptions.Compiled);
    static readonly Regex BriefingTextRegex = new(@"\{\s*""([^""]+)""\s*,\s*""((?:\\.|[^""])*)""\s*\}", RegexOptions.Compiled);
    static string[] prefabTypeCandidates;

    StageData SelectedStage => IsValidIndex(root?.stages, selectedStageIndex) ? root.stages[selectedStageIndex] : null;
    WaveDefinition SelectedWave => IsValidIndex(SelectedStage?.spawns, selectedWaveIndex) ? SelectedStage.spawns[selectedWaveIndex] : null;
    EnemySpawnDefinition SelectedEnemy => IsValidIndex(SelectedWave?.enemies, selectedEnemyIndex) ? SelectedWave.enemies[selectedEnemyIndex] : null;

    [MenuItem("Window/はこんばっと/敵配置JSONエディター")]
    public static void Open()
    {
        GetWindow<StageSpawnJsonEditorWindow>("敵配置JSON");
    }

    void OnEnable()
    {
        jsonPath = Path.GetFullPath(JsonAssetPath);
        SceneView.duringSceneGui += OnSceneGUI;
        LoadJson();
    }

    void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    void OnGUI()
    {
        DrawToolbar();

        if (root == null)
        {
            EditorGUILayout.HelpBox("JSONを読み込めませんでした。ファイルを確認してください。", MessageType.Warning);
            return;
        }

        EditorGUILayout.BeginHorizontal();
        DrawStageList();
        DrawDetails();
        EditorGUILayout.EndHorizontal();
    }

    void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        EditorGUI.BeginDisabledGroup(!isDirty);
        if (GUILayout.Button("保存", EditorStyles.toolbarButton, GUILayout.Width(60)))
            SaveJson();
        EditorGUI.EndDisabledGroup();

        if (GUILayout.Button("再読込", EditorStyles.toolbarButton, GUILayout.Width(70)))
            ConfirmDiscardAndLoad();

        GUILayout.Space(8);
        GUILayout.Label(jsonPath, EditorStyles.miniLabel);
        GUILayout.FlexibleSpace();

        if (isDirty)
            GUILayout.Label("未保存", EditorStyles.boldLabel, GUILayout.Width(52));

        EditorGUILayout.EndHorizontal();
    }

    void DrawStageList()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(330));
        stageScroll = EditorGUILayout.BeginScrollView(stageScroll);

        EditorGUILayout.LabelField("Stages", EditorStyles.boldLabel);
        if (root.stages == null)
            root.stages = new List<StageData>();

        for (int i = 0; i < root.stages.Count; i++)
        {
            StageData stage = root.stages[i];
            string label = $"{stage.sceneName}  waves:{SafeCount(stage.spawns)}";
            if (GUILayout.Toggle(selectedStageIndex == i, label, "Button"))
            {
                if (selectedStageIndex != i)
                {
                    selectedStageIndex = i;
                    selectedWaveIndex = 0;
                    selectedEnemyIndex = 0;
                    RepaintScene();
                }
            }
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ Stage"))
        {
            root.stages.Add(NewStage());
            selectedStageIndex = root.stages.Count - 1;
            selectedWaveIndex = 0;
            selectedEnemyIndex = 0;
            MarkDirty();
        }

        EditorGUI.BeginDisabledGroup(SelectedStage == null);
        if (GUILayout.Button("複製"))
            DuplicateStage();
        if (GUILayout.Button("削除"))
            DeleteStage();
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);
        DrawWaveList();

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    void DrawWaveList()
    {
        StageData stage = SelectedStage;
        if (stage == null) return;

        stage.spawns ??= new List<WaveDefinition>();

        EditorGUILayout.LabelField("Waves", EditorStyles.boldLabel);
        for (int i = 0; i < stage.spawns.Count; i++)
        {
            WaveDefinition wave = stage.spawns[i];
            string label = $"Wave {wave.waveId}  enemies:{SafeCount(wave.enemies)}";
            if (GUILayout.Toggle(selectedWaveIndex == i, label, "Button"))
            {
                if (selectedWaveIndex != i)
                {
                    selectedWaveIndex = i;
                    selectedEnemyIndex = 0;
                    RepaintScene();
                }
            }
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ Wave"))
        {
            stage.spawns.Add(NewWave(stage.spawns.Count));
            selectedWaveIndex = stage.spawns.Count - 1;
            selectedEnemyIndex = 0;
            MarkDirty();
        }

        EditorGUI.BeginDisabledGroup(SelectedWave == null);
        if (GUILayout.Button("複製"))
            DuplicateWave();
        if (GUILayout.Button("削除"))
            DeleteWave();
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();
    }

    void DrawDetails()
    {
        detailScroll = EditorGUILayout.BeginScrollView(detailScroll);

        StageData stage = SelectedStage;
        if (stage == null)
        {
            EditorGUILayout.HelpBox("ステージを選択してください。", MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }

        EditorGUI.BeginChangeCheck();
        stage.sceneName = EditorGUILayout.TextField("Scene Name", stage.sceneName);
        stage.randomSeed = EditorGUILayout.IntField("Random Seed", stage.randomSeed);
        if (EditorGUI.EndChangeCheck())
            MarkDirty();

        DrawSceneIntegration(stage);
        DrawBriefingText(stage);

        WaveDefinition wave = SelectedWave;
        if (wave == null)
        {
            EditorGUILayout.HelpBox("Waveを選択してください。", MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }

        EditorGUILayout.Space(8);
        DrawWaveDetails(wave);

        EditorGUILayout.Space(8);
        DrawEnemyList(wave);

        EnemySpawnDefinition enemy = SelectedEnemy;
        if (enemy != null)
        {
            EditorGUILayout.Space(8);
            DrawEnemyDetails(enemy);
        }

        EditorGUILayout.EndScrollView();
    }

    void DrawSceneIntegration(StageData stage)
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Scene Integration", EditorStyles.boldLabel);

        string sceneName = stage.sceneName;
        bool hasSceneAsset = HasSceneAsset(sceneName);
        bool inBuildSettings = IsSceneInBuildSettings(sceneName);
        bool inMenu = IsStageListedInScene<selectmenuUI>("Assets/Scenes/Menu.unity", sceneName, ui => ui.stage_name);
        bool inBriefing = IsStageListedInScene<selectmenuUI>("Assets/Scenes/Briefing.unity", sceneName, ui => ui.stage_name);
        bool inSetup = IsStageListedInScene<SetupUI>("Assets/Scenes/SetUp.unity", sceneName, ui => ui.scene_name);

        EditorGUILayout.LabelField("Scene Asset", hasSceneAsset ? "OK" : "Missing Assets/Scenes/<SceneName>.unity");
        EditorGUILayout.LabelField("Build Settings", inBuildSettings ? "OK" : "未登録");
        EditorGUILayout.LabelField("Menu stage_name", inMenu ? "OK" : "未登録");
        EditorGUILayout.LabelField("Briefing stage_name", inBriefing ? "OK" : "未登録");
        EditorGUILayout.LabelField("SetUp scene_name", inSetup ? "OK" : "未登録");

        EditorGUI.BeginDisabledGroup(string.IsNullOrWhiteSpace(sceneName) || !hasSceneAsset);
        if (GUILayout.Button("選択Stageをシーン選択へ同期"))
            SyncSelectedStageSceneReferences(sceneName);
        if (GUILayout.Button("Title Ally PrefabをこのSceneのRegistryへ登録"))
            SyncTitleAllyPrefabToStageRegistry(sceneName);
        EditorGUI.EndDisabledGroup();

        if (!hasSceneAsset)
            EditorGUILayout.HelpBox("先に Assets/Scenes に同名の .unity シーンを作成してください。JSONのsceneNameは実シーン名と一致している必要があります。", MessageType.Warning);
    }

    void DrawBriefingText(StageData stage)
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Briefing Text", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.TextArea(GetBriefingText(stage.sceneName), GUILayout.MinHeight(52));
        }
    }

    void DrawWaveDetails(WaveDefinition wave)
    {
        EditorGUILayout.LabelField("Wave", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        wave.waveId = EditorGUILayout.IntField("Wave Id", wave.waveId);
        DrawIntList("Require Cleared Waves", wave.requireClearedWaves ??= new List<int>());
        if (EditorGUI.EndChangeCheck())
            MarkDirty();
    }

    void DrawEnemyList(WaveDefinition wave)
    {
        wave.enemies ??= new List<EnemySpawnDefinition>();
        EditorGUILayout.LabelField("Enemies", EditorStyles.boldLabel);

        for (int i = 0; i < wave.enemies.Count; i++)
        {
            EnemySpawnDefinition enemy = wave.enemies[i];
            string areaId = enemy?.placement != null ? enemy.placement.areaId : "";
            string label = $"{i:00}  {enemy?.prefabType}  {areaId}";
            if (GUILayout.Toggle(selectedEnemyIndex == i, label, "Button"))
            {
                selectedEnemyIndex = i;
                RepaintScene();
            }
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ Enemy"))
        {
            wave.enemies.Add(NewEnemy());
            selectedEnemyIndex = wave.enemies.Count - 1;
            MarkDirty();
        }

        EditorGUI.BeginDisabledGroup(SelectedEnemy == null);
        if (GUILayout.Button("複製"))
            DuplicateEnemy();
        if (GUILayout.Button("削除"))
            DeleteEnemy();
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();
    }

    void DrawEnemyDetails(EnemySpawnDefinition enemy)
    {
        EditorGUILayout.LabelField("Enemy", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        enemy.enemyId = EditorGUILayout.IntField("Enemy Id", enemy.enemyId);
        enemy.prefabType = DrawPrefabTypePopup(enemy.prefabType);
        enemy.spawnAsAlly = EditorGUILayout.Toggle("Spawn As Ally", enemy.spawnAsAlly);
        enemy.missionTarget = EditorGUILayout.Toggle("Mission Target", enemy.missionTarget);
        enemy.hideFromHud = EditorGUILayout.Toggle("Hide From HUD", enemy.hideFromHud);
        enemy.lifetime = EditorGUILayout.FloatField("Lifetime", enemy.lifetime);
        enemy.useUnknownPhaseTrigger = EditorGUILayout.Toggle("Use Unknown Phase Trigger", enemy.useUnknownPhaseTrigger);
        enemy.isPhaseTrrigersParent = EditorGUILayout.Toggle("Is Phase Trigger Parent", enemy.isPhaseTrrigersParent);
        enemy.phaseTriggerId = EditorGUILayout.TextField("Phase Trigger Id", enemy.phaseTriggerId);
        enemy.originName = EditorGUILayout.TextField("Origin Name", enemy.originName);
        enemy.approachDistance = EditorGUILayout.FloatField("Approach Distance", enemy.approachDistance);

        enemy.placement ??= NewPlacement();
        DrawPlacement(enemy.placement);

        if (enemy.uavLaunch != null || GUILayout.Button("UAV Launch設定を追加"))
            DrawUavLaunch(enemy);

        if (EditorGUI.EndChangeCheck())
        {
            MarkDirty();
            RepaintScene();
        }
    }

    string DrawPrefabTypePopup(string currentValue)
    {
        string[] candidates = GetPrefabTypeCandidates();
        var labels = new List<string>(candidates);
        int selectedIndex = labels.IndexOf(currentValue);
        bool isCustom = selectedIndex < 0;

        if (isCustom)
        {
            labels.Insert(0, string.IsNullOrWhiteSpace(currentValue) ? "Custom" : $"Custom: {currentValue}");
            selectedIndex = 0;
        }

        EditorGUILayout.BeginHorizontal();
        int nextIndex = EditorGUILayout.Popup("Prefab Type", selectedIndex, labels.ToArray());
        string nextValue = currentValue;

        if (isCustom)
        {
            if (nextIndex > 0)
                nextValue = candidates[nextIndex - 1];
        }
        else if (nextIndex >= 0 && nextIndex < candidates.Length)
        {
            nextValue = candidates[nextIndex];
        }

        if (GUILayout.Button("更新", GUILayout.Width(44)))
            prefabTypeCandidates = null;
        EditorGUILayout.EndHorizontal();

        if (isCustom && nextIndex == 0)
            nextValue = EditorGUILayout.TextField("Custom Prefab Type", currentValue);

        return nextValue;
    }

    void DrawPlacement(PlacementDefinition placement)
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Placement", EditorStyles.boldLabel);

        int modeIndex = Mathf.Max(0, System.Array.IndexOf(PlacementModes, placement.mode));
        modeIndex = EditorGUILayout.Popup("Mode", modeIndex, PlacementModes);
        placement.mode = PlacementModes[modeIndex];
        placement.count = Mathf.Max(1, EditorGUILayout.IntField("Count", placement.count));
        placement.position = DrawVector3("Position", placement.position ?? new SerializableVector3());
        placement.rotate = DrawVector3("Rotate", placement.rotate ?? new SerializableVector3());
        placement.vector = DrawVector3("Vector", placement.vector ?? new SerializableVector3());
        placement.isstoped = EditorGUILayout.Toggle("Is Stoped", placement.isstoped);
        placement.snapToTerrain = EditorGUILayout.Toggle("Snap To Terrain", placement.snapToTerrain);
        placement.areaId = EditorGUILayout.TextField("Area Id", placement.areaId);
        placement.terrainLayer = EditorGUILayout.TextField("Terrain Layer", placement.terrainLayer);
        placement.radius = Mathf.Max(0f, EditorGUILayout.FloatField("Radius", placement.radius));
        placement.altitudeOffset = EditorGUILayout.FloatField("Altitude Offset", placement.altitudeOffset);
    }

    void DrawUavLaunch(EnemySpawnDefinition enemy)
    {
        enemy.uavLaunch ??= new UAVLaunchDefinition();
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("UAV Launch", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        enemy.uavLaunch.enabled = EditorGUILayout.Toggle("Enabled", enemy.uavLaunch.enabled);
        if (GUILayout.Button("削除", GUILayout.Width(60)))
        {
            enemy.uavLaunch = null;
            return;
        }
        EditorGUILayout.EndHorizontal();

        enemy.uavLaunch.launchOnPhaseActivate = EditorGUILayout.Toggle("Launch On Phase Activate", enemy.uavLaunch.launchOnPhaseActivate);
        enemy.uavLaunch.capacity = EditorGUILayout.IntField("Capacity", enemy.uavLaunch.capacity);
        enemy.uavLaunch.waveId = EditorGUILayout.IntField("Wave Id", enemy.uavLaunch.waveId);
        enemy.uavLaunch.launchDelay = EditorGUILayout.FloatField("Launch Delay", enemy.uavLaunch.launchDelay);
        enemy.uavLaunch.fighterCount = EditorGUILayout.IntField("Fighter Count", enemy.uavLaunch.fighterCount);
        enemy.uavLaunch.fighterSpacingAngle = EditorGUILayout.FloatField("Fighter Spacing Angle", enemy.uavLaunch.fighterSpacingAngle);
        enemy.uavLaunch.fighterSpawnRadius = EditorGUILayout.FloatField("Fighter Spawn Radius", enemy.uavLaunch.fighterSpawnRadius);
        enemy.uavLaunch.fighterSpawnAltitude = EditorGUILayout.FloatField("Fighter Spawn Altitude", enemy.uavLaunch.fighterSpawnAltitude);
        enemy.uavLaunch.fighterSpeed = EditorGUILayout.FloatField("Fighter Speed", enemy.uavLaunch.fighterSpeed);
        enemy.uavLaunch.fighterPrefabType = EditorGUILayout.TextField("Fighter Prefab Type", enemy.uavLaunch.fighterPrefabType);
    }

    SerializableVector3 DrawVector3(string label, SerializableVector3 value)
    {
        Vector3 vector = new Vector3(value.x, value.y, value.z);
        vector = EditorGUILayout.Vector3Field(label, vector);
        value.x = vector.x;
        value.y = vector.y;
        value.z = vector.z;
        return value;
    }

    void DrawIntList(string label, List<int> values)
    {
        EditorGUILayout.LabelField(label);
        int removeIndex = -1;

        EditorGUI.indentLevel++;
        for (int i = 0; i < values.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            values[i] = EditorGUILayout.IntField($"Element {i}", values[i]);
            if (GUILayout.Button("-", GUILayout.Width(24)))
                removeIndex = i;
            EditorGUILayout.EndHorizontal();
        }
        EditorGUI.indentLevel--;

        if (removeIndex >= 0)
            values.RemoveAt(removeIndex);

        if (GUILayout.Button("+ Require Wave", GUILayout.Width(130)))
            values.Add(0);
    }

    void OnSceneGUI(SceneView sceneView)
    {
        EnemySpawnDefinition enemy = SelectedEnemy;
        PlacementDefinition placement = enemy?.placement;
        if (placement?.position == null) return;

        Vector3 position = placement.position.ToVector3();
        Handles.color = enemy.missionTarget ? Color.red : Color.cyan;

        EditorGUI.BeginChangeCheck();
        Vector3 moved = Handles.PositionHandle(position, Quaternion.identity);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(this, "Move Spawn Placement");
            placement.position.x = moved.x;
            placement.position.y = moved.y;
            placement.position.z = moved.z;
            MarkDirty();
            Repaint();
        }

        Handles.DrawWireDisc(moved, Vector3.up, Mathf.Max(1f, placement.radius));
        Handles.Label(moved + Vector3.up * 40f, $"{enemy.prefabType}\n{placement.areaId}");
    }

    void LoadJson()
    {
        if (!File.Exists(jsonPath))
        {
            root = new StageRoot { stages = new List<StageData>() };
            isDirty = true;
            return;
        }

        string json = File.ReadAllText(jsonPath).Trim('\uFEFF', '\u200B', '\u0000', ' ', '\r', '\n', '\t');
        root = JsonUtility.FromJson<StageRoot>(json);
        root ??= new StageRoot();
        root.stages ??= new List<StageData>();
        selectedStageIndex = Mathf.Clamp(selectedStageIndex, 0, Mathf.Max(0, root.stages.Count - 1));
        selectedWaveIndex = 0;
        selectedEnemyIndex = 0;
        isDirty = false;
        RepaintScene();
    }

    void SaveJson()
    {
        NormalizeBeforeSave();
        File.WriteAllText(jsonPath, JsonUtility.ToJson(root, true));
        AssetDatabase.Refresh();
        isDirty = false;
    }

    void ConfirmDiscardAndLoad()
    {
        if (!isDirty || EditorUtility.DisplayDialog("再読込", "未保存の変更を破棄してJSONを再読込しますか？", "再読込", "キャンセル"))
            LoadJson();
    }

    void NormalizeBeforeSave()
    {
        if (root?.stages == null) return;

        foreach (StageData stage in root.stages)
        {
            stage.spawns ??= new List<WaveDefinition>();
            foreach (WaveDefinition wave in stage.spawns)
            {
                wave.requireClearedWaves ??= new List<int>();
                wave.enemies ??= new List<EnemySpawnDefinition>();
                foreach (EnemySpawnDefinition enemy in wave.enemies)
                    enemy.placement ??= NewPlacement();
            }
        }
    }

    void DuplicateStage()
    {
        string json = JsonUtility.ToJson(SelectedStage);
        StageData copy = JsonUtility.FromJson<StageData>(json);
        copy.sceneName += "_copy";
        root.stages.Insert(selectedStageIndex + 1, copy);
        selectedStageIndex++;
        selectedWaveIndex = 0;
        selectedEnemyIndex = 0;
        MarkDirty();
    }

    void DeleteStage()
    {
        if (!EditorUtility.DisplayDialog("Stage削除", $"{SelectedStage.sceneName} を削除しますか？", "削除", "キャンセル"))
            return;

        root.stages.RemoveAt(selectedStageIndex);
        selectedStageIndex = Mathf.Clamp(selectedStageIndex, 0, Mathf.Max(0, root.stages.Count - 1));
        selectedWaveIndex = 0;
        selectedEnemyIndex = 0;
        MarkDirty();
    }

    void DuplicateWave()
    {
        StageData stage = SelectedStage;
        string json = JsonUtility.ToJson(SelectedWave);
        WaveDefinition copy = JsonUtility.FromJson<WaveDefinition>(json);
        copy.waveId = stage.spawns.Count;
        stage.spawns.Insert(selectedWaveIndex + 1, copy);
        selectedWaveIndex++;
        selectedEnemyIndex = 0;
        MarkDirty();
    }

    void DeleteWave()
    {
        StageData stage = SelectedStage;
        stage.spawns.RemoveAt(selectedWaveIndex);
        selectedWaveIndex = Mathf.Clamp(selectedWaveIndex, 0, Mathf.Max(0, stage.spawns.Count - 1));
        selectedEnemyIndex = 0;
        MarkDirty();
    }

    void DuplicateEnemy()
    {
        WaveDefinition wave = SelectedWave;
        string json = JsonUtility.ToJson(SelectedEnemy);
        EnemySpawnDefinition copy = JsonUtility.FromJson<EnemySpawnDefinition>(json);
        if (copy.placement != null)
            copy.placement.areaId += "_copy";
        wave.enemies.Insert(selectedEnemyIndex + 1, copy);
        selectedEnemyIndex++;
        MarkDirty();
    }

    void DeleteEnemy()
    {
        WaveDefinition wave = SelectedWave;
        wave.enemies.RemoveAt(selectedEnemyIndex);
        selectedEnemyIndex = Mathf.Clamp(selectedEnemyIndex, 0, Mathf.Max(0, wave.enemies.Count - 1));
        MarkDirty();
    }

    void MarkDirty()
    {
        isDirty = true;
        RepaintScene();
    }

    void RepaintScene()
    {
        SceneView.RepaintAll();
    }

    void SyncSelectedStageSceneReferences(string sceneName)
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        AddSceneToBuildSettings(sceneName);
        AddStageToSelectMenuScene("Assets/Scenes/Menu.unity", sceneName);
        AddStageToSelectMenuScene("Assets/Scenes/Briefing.unity", sceneName);
        AddStageToSelectMenuScene("Assets/Scenes/Title.unity", sceneName);
        AddStageToSetupScene("Assets/Scenes/SetUp.unity", sceneName);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("同期完了", $"{sceneName} をBuild Settings/Menu/Briefing/SetUpへ登録しました。", "OK");
    }

    void AddSceneToBuildSettings(string sceneName)
    {
        string scenePath = GetScenePath(sceneName);
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

        foreach (var scene in scenes)
        {
            if (scene.path == scenePath)
            {
                scene.enabled = true;
                EditorBuildSettings.scenes = scenes.ToArray();
                return;
            }
        }

        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    void AddStageToSelectMenuScene(string scenePath, string sceneName)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        bool changed = false;

        foreach (var rootObject in scene.GetRootGameObjects())
        {
            foreach (var ui in rootObject.GetComponentsInChildren<selectmenuUI>(true))
            {
                ui.stage_name ??= new List<string>();
                if (ui.stage_name.Contains(sceneName)) continue;

                Undo.RecordObject(ui, "Add Stage Name");
                ui.stage_name.Add(sceneName);
                EditorUtility.SetDirty(ui);
                changed = true;
            }
        }

        if (changed)
            EditorSceneManager.SaveScene(scene);
    }

    void AddStageToSetupScene(string scenePath, string sceneName)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        bool changed = false;

        foreach (var rootObject in scene.GetRootGameObjects())
        {
            foreach (var ui in rootObject.GetComponentsInChildren<SetupUI>(true))
            {
                ui.scene_name ??= new List<string>();
                if (ui.scene_name.Contains(sceneName)) continue;

                Undo.RecordObject(ui, "Add Setup Scene Name");
                ui.scene_name.Add(sceneName);
                EditorUtility.SetDirty(ui);
                changed = true;
            }
        }

        if (changed)
            EditorSceneManager.SaveScene(scene);
    }

    void SyncTitleAllyPrefabToStageRegistry(string sceneName)
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        GameObject allyPrefab = LoadTitleAllyPrefab();
        if (allyPrefab == null)
        {
            EditorUtility.DisplayDialog("登録できません", "Title.unity の TitleBackgroundAirBattle に allyAcePrefab が設定されていません。", "OK");
            return;
        }

        string scenePath = GetScenePath(sceneName);
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        bool changed = false;

        foreach (var rootObject in scene.GetRootGameObjects())
        {
            foreach (var registry in rootObject.GetComponentsInChildren<SpawnPrefabRegistry>(true))
            {
                registry.entries ??= new List<SpawnPrefabEntry>();
                SpawnPrefabEntry entry = registry.entries.Find(x => x != null && x.prefabTypeName == "ALLY_ACE");
                if (entry == null)
                {
                    entry = new SpawnPrefabEntry { prefabTypeName = "ALLY_ACE" };
                    registry.entries.Add(entry);
                }

                if (entry.prefab == allyPrefab)
                    continue;

                Undo.RecordObject(registry, "Register Title Ally Prefab");
                entry.prefab = allyPrefab;
                EditorUtility.SetDirty(registry);
                changed = true;
            }
        }

        if (changed)
            EditorSceneManager.SaveScene(scene);

        EditorUtility.DisplayDialog("登録完了", $"{sceneName} の SpawnPrefabRegistry に ALLY_ACE を登録しました。", "OK");
    }

    GameObject LoadTitleAllyPrefab()
    {
        const string titleScenePath = "Assets/Scenes/Title.unity";
        if (!File.Exists(titleScenePath))
            return null;

        Scene titleScene = EditorSceneManager.GetSceneByPath(titleScenePath);
        bool closeAfterRead = false;
        if (!titleScene.IsValid() || !titleScene.isLoaded)
        {
            titleScene = EditorSceneManager.OpenScene(titleScenePath, OpenSceneMode.Additive);
            closeAfterRead = true;
        }

        GameObject prefab = null;
        foreach (var rootObject in titleScene.GetRootGameObjects())
        {
            foreach (var titleBattle in rootObject.GetComponentsInChildren<TitleBackgroundAirBattle>(true))
            {
                SerializedObject serialized = new SerializedObject(titleBattle);
                SerializedProperty allyPrefabProperty = serialized.FindProperty("allyAcePrefab");
                prefab = allyPrefabProperty != null ? allyPrefabProperty.objectReferenceValue as GameObject : null;
                if (prefab != null)
                    break;
            }

            if (prefab != null)
                break;
        }

        if (closeAfterRead)
            EditorSceneManager.CloseScene(titleScene, true);

        return prefab;
    }

    static StageData NewStage()
    {
        return new StageData
        {
            sceneName = SceneManager.GetActiveScene().name,
            randomSeed = 260531,
            spawns = new List<WaveDefinition> { NewWave(0) }
        };
    }

    static WaveDefinition NewWave(int waveId)
    {
        return new WaveDefinition
        {
            waveId = waveId,
            requireClearedWaves = new List<int>(),
            enemies = new List<EnemySpawnDefinition>()
        };
    }

    static EnemySpawnDefinition NewEnemy()
    {
        return new EnemySpawnDefinition
        {
            enemyId = -1,
            prefabType = "AA_GUN",
            spawnAsAlly = false,
            missionTarget = false,
            lifetime = 0f,
            placement = NewPlacement()
        };
    }

    static PlacementDefinition NewPlacement()
    {
        return new PlacementDefinition
        {
            mode = "fixed",
            count = 1,
            isstoped = true,
            position = new SerializableVector3(),
            rotate = new SerializableVector3(),
            vector = new SerializableVector3(),
            snapToTerrain = false,
            terrainLayer = "Terrain",
            radius = 0f,
            altitudeOffset = 5f
        };
    }

    static bool IsValidIndex<T>(List<T> list, int index)
    {
        return list != null && index >= 0 && index < list.Count;
    }

    static int SafeCount<T>(List<T> list)
    {
        return list != null ? list.Count : 0;
    }

    static string[] GetPrefabTypeCandidates()
    {
        if (prefabTypeCandidates != null)
            return prefabTypeCandidates;

        var values = new List<string>();
        string sourcePath = "Assets/script/mission/Player/EnemyNameConverterToUI.cs";
        if (File.Exists(sourcePath))
        {
            string source = File.ReadAllText(sourcePath);
            foreach (Match match in EnemyNameRegex.Matches(source))
            {
                string value = match.Groups[1].Value;
                if (!string.IsNullOrWhiteSpace(value) && !values.Contains(value))
                    values.Add(value);
            }
        }

        AddIfMissing(values, "AIR_BATTLESHIP");
        AddIfMissing(values, "ALLY_ACE");
        AddIfMissing(values, "TRIGGER_EMPTY");
        AddIfMissing(values, "UAV_STORAGE");
        values.Sort(System.StringComparer.OrdinalIgnoreCase);
        prefabTypeCandidates = values.ToArray();
        return prefabTypeCandidates;
    }

    static void AddIfMissing(List<string> values, string value)
    {
        if (!values.Contains(value))
            values.Add(value);
    }

    static string GetBriefingText(string sceneName)
    {
        const string fallback = "作戦空域内のすべての敵目標を撃破せよ。";
        if (string.IsNullOrWhiteSpace(sceneName))
            return fallback;

        string sourcePath = "Assets/script/Set/selectmenuUI.cs";
        if (!File.Exists(sourcePath))
            return fallback;

        string source = File.ReadAllText(sourcePath);
        foreach (Match match in BriefingTextRegex.Matches(source))
        {
            if (match.Groups[1].Value == sceneName)
                return Regex.Unescape(match.Groups[2].Value);
        }

        return fallback;
    }

    static bool HasSceneAsset(string sceneName)
    {
        return !string.IsNullOrWhiteSpace(sceneName) && File.Exists(GetScenePath(sceneName));
    }

    static bool IsSceneInBuildSettings(string sceneName)
    {
        string scenePath = GetScenePath(sceneName);
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.path == scenePath && scene.enabled)
                return true;
        }

        return false;
    }

    static bool IsStageListedInScene<T>(string scenePath, string sceneName, System.Func<T, List<string>> listSelector)
        where T : Component
    {
        if (string.IsNullOrWhiteSpace(sceneName) || !File.Exists(scenePath))
            return false;

        Scene scene = EditorSceneManager.GetSceneByPath(scenePath);
        bool closeAfterCheck = false;

        if (!scene.IsValid() || !scene.isLoaded)
        {
            scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            closeAfterCheck = true;
        }

        bool found = false;
        foreach (var rootObject in scene.GetRootGameObjects())
        {
            foreach (var component in rootObject.GetComponentsInChildren<T>(true))
            {
                List<string> values = listSelector(component);
                if (values != null && values.Contains(sceneName))
                {
                    found = true;
                    break;
                }
            }

            if (found)
                break;
        }

        if (closeAfterCheck)
            EditorSceneManager.CloseScene(scene, true);

        return found;
    }

    static string GetScenePath(string sceneName)
    {
        return $"Assets/Scenes/{sceneName}.unity";
    }
}
