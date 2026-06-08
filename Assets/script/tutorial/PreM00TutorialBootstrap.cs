using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class PreM00TutorialBootstrap : MonoBehaviour
{
    [System.Serializable]
    public class GuideLayout
    {
        public Vector2 textPosition;
        public Vector2 from;
        public Vector2 to;
    }

    const string MainScene = "M00";

    public GuideLayout[] guideLayouts =
    {
        // 左1: R2/L2 / ヨー
        new GuideLayout { textPosition = new Vector2(-380f, 170f), from = new Vector2(-150f, 170f), to = new Vector2(-100f, 106f) },
        // 左2: L1 / 減速
        new GuideLayout { textPosition = new Vector2(-380f, 105f), from = new Vector2(-150f, 105f), to = new Vector2(-100f, 63f) },
        // 左3: 左スティック左右 / ロール
        new GuideLayout { textPosition = new Vector2(-380f, 40f), from = new Vector2(-150f, 40f), to = new Vector2(-100f, 20f) },
        // 左4: 左スティック上下 / ピッチ
        new GuideLayout { textPosition = new Vector2(-380f, -25f), from = new Vector2(-150f, -25f), to = new Vector2(-100f, -23f) },
        // 左5: 左スティック押し込み + 減速 / 機動制限解除
        new GuideLayout { textPosition = new Vector2(-380f, -90f), from = new Vector2(-150f, -90f), to = new Vector2(-100f, -66f) },
        // 右1: R1 / 加速
        new GuideLayout { textPosition = new Vector2(380f, 170f), from = new Vector2(150f, 170f), to = new Vector2(110f, 106f) },
        // 右2: △ / 目標切替
        new GuideLayout { textPosition = new Vector2(380f, 105f), from = new Vector2(150f, 105f), to = new Vector2(110f, 63f) },
        // 右3: □ / 兵装切替
        new GuideLayout { textPosition = new Vector2(380f, 40f), from = new Vector2(150f, 40f), to = new Vector2(110f, 20f) },
        // 右4: ○ / ミサイル・選択兵装発射
        new GuideLayout { textPosition = new Vector2(380f, -25f), from = new Vector2(150f, -25f), to = new Vector2(110f, -23f) },
        // 右5: × / 機銃
        new GuideLayout { textPosition = new Vector2(380f, -90f), from = new Vector2(150f, -90f), to = new Vector2(110f, -66f) },
        // 右6: 右スティック / 視点移動
        new GuideLayout { textPosition = new Vector2(380f, -155f), from = new Vector2(150f, -155f), to = new Vector2(110f, -109f) },
    };

    TMP_FontAsset resolvedFont;
    Canvas canvas;

    IEnumerator Start()
    {
        yield return null;
        Setup();
    }

    void Setup()
    {
        resolvedFont = ResolveNotoFont();
        DisableSceneObjects();
        canvas = CreateTutorialOverlayCanvas("PreM00InputCheckCanvas");

        TextMeshProUGUI inputText = CreateOverlayText(
            "TutorialInputVisualizerText",
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(460f, 260f),
            30f,
            TextAlignmentOptions.Center);

        RectTransform leftStick = CreateStickMarker("TutorialInputLeftStick", new Vector2(-120f, -85f));
        RectTransform rightStick = CreateStickMarker("TutorialInputRightStick", new Vector2(120f, -85f));

        var inputVisualizer = inputText.gameObject.AddComponent<InputTextUI>();
        inputVisualizer.text = inputText;
        inputVisualizer.Lstick = leftStick;
        inputVisualizer.Rstick = rightStick;

        TextMeshProUGUI[] checkTexts = CreateGuideTexts();

        TextMeshProUGUI summaryText = CreateOverlayText(
            "TutorialInputCheckSummaryText",
            new Vector2(0f, 1f),
            new Vector2(550f, -100f),
            new Vector2(980f, 190f),
            22f,
            TextAlignmentOptions.TopLeft);

        var controller = gameObject.AddComponent<TutorialInputCheckController>();
        controller.checklistText = summaryText;
        controller.checkTexts = checkTexts;
        controller.nextSceneName = MainScene;
        controller.autoLoadNextScene = false;
    }

    void DisableSceneObjects()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (Canvas sceneCanvas in canvases)
        {
            if (sceneCanvas != null)
                sceneCanvas.gameObject.SetActive(false);
        }

        var managers = FindObjectsByType<SpawnTableManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var manager in managers)
        {
            if (manager != null)
                manager.enabled = false;
        }

        WeaponSystem[] weaponSystems = FindObjectsByType<WeaponSystem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (WeaponSystem weaponSystem in weaponSystems)
        {
            if (weaponSystem != null)
                weaponSystem.enabled = false;
        }
    }

    TextMeshProUGUI[] CreateGuideTexts()
    {
        string[] labels =
        {
            "R2/L2: 左右ヨー",
            "L1: 減速",
            "左スティック左右: ロール",
            "左スティック上下: ピッチ",
            "左スティック押し込み + L1: 機動力制限解除",

            "R1: 加速",
            "△: 目標切替",
            "□: 主兵装切替",
            "○: 主兵装発射",
            "×: 機銃発射",
            "右スティック: 視点移動",
        };

        var texts = new TextMeshProUGUI[labels.Length];
        for (int i = 0; i < labels.Length; i++)
        {
            GuideLayout layout = ResolveGuideLayout(i);
            texts[i] = CreateOverlayText(
                "TutorialInputGuideText" + i,
                new Vector2(0.5f, 0.5f),
                layout.textPosition,
                new Vector2(440f, 48f),
                24f,
                layout.textPosition.x < 0f ? TextAlignmentOptions.Right : TextAlignmentOptions.Left);

            texts[i].text = labels[i];
            CreateGuideLine("TutorialInputGuideLine" + i, layout.from, layout.to);
        }

        return texts;
    }

    GuideLayout ResolveGuideLayout(int index)
    {
        if (guideLayouts != null && index >= 0 && index < guideLayouts.Length && guideLayouts[index] != null)
            return guideLayouts[index];

        return new GuideLayout { textPosition = Vector2.zero, from = Vector2.zero, to = Vector2.zero };
    }

    TextMeshProUGUI CreateOverlayText(
        string objectName,
        Vector2 anchor,
        Vector2 anchoredPosition,
        Vector2 size,
        float fontSize,
        TextAlignmentOptions alignment)
    {
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

    RectTransform CreateStickMarker(string objectName, Vector2 anchoredPosition)
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

    void CreateGuideLine(string objectName, Vector2 from, Vector2 to)
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

        return TMP_Settings.defaultFontAsset;
    }
}
