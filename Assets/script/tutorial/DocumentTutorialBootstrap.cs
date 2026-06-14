using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class DocumentTutorialBootstrap : MonoBehaviour
{
    const string NextScene = "preM00";
    const string BackScene = "Menu";
    const float StickScrollSpeed = 0.85f;
    const float ButtonScrollSpeed = 1.2f;

    static readonly string[][] DocumentSections =
    {
        new[]
        {
            "基本操作",
            "左スティック上下で機首を上げ下げします。左スティック左右でロールし、機体の傾きを作ります。",
            "L1 / R1 で減速・加速します。速度を落としすぎると失速しやすいため、旋回中も速度表示を確認してください。",
            "スティック押し込みで機動制限を解除できます。大きく姿勢を崩せる代わりに、回復操作も必要になります。"
        },
        new[]
        {
            "目標捕捉",
            "レーダーの赤い点がミッション目標です。画面端の矢印は、視界外にある目標の方向を示します。",
            "まず矢印を追って目標を視界（距離5000以内）に入れ、緑枠で追跡できる状態を作ります。"
        },
        new[]
        {
            "地形",
            "敵味方を問わず、機銃とミサイルは地形でかき消されます。",
            "地上目標を狙うときは、遮られていないかを確認してから照準してください。"
        },
        new[]
        {
            "失速",
            "一定以下まで速度が落ちると操作性が極端に低下します。",
            "旋回中も速度表示を見て、落としすぎないように注意してください。"
        },
        new[]
        {
            "ターゲットロケーター",
            "追跡中の目標が画面外に出ると、画面端の緑円で方向を示します。",
            "緑円が画面上側に来るようにロールしてから旋回すると、目標へ戻りやすくなります。"
        },
        new[]
        {
            "敵の殲滅",
            "ミッションクリアに必須なのは、TGTが左上についた敵の排除だけです。",
            "それ以外の敵は必須ではありませんが、攻撃してくるので安全確保や撃破スコアのために殲滅すると有利です。"
        },
        new[]
        {
            "HUD / UI確認",
            "緑枠は敵機、点滅中の緑枠は追跡対象、赤枠はロックオン完了です。TGT は目標情報、HP は耐久値を示します。",
            "Next は目標切替ボタン押下時、次に追跡する敵機です"
        },
        new[]
        {
            "ECM",
            "レーダーが緑色の円で妨害される場合があります。この範囲内の敵へのロックオンは阻害されます。",
            "JAMMERという名称の敵を優先して撃破し、妨害を破ってください。"
        },
        new[]
        {
            "ミサイル / 兵装切替",
            "赤枠になったら○でミサイルを発射できます。距離、向き、再装填状況を見てから撃つと命中しやすくなります。",
            "□で兵装を切り替えます。弾数と再装填を確認しながら、状況に合う武装を選んでください。"
        },
        new[]
        {
            "機銃",
            "機銃は照準点と目標の動きを合わせて撃ちます。近距離で当てやすく、弾速と機体姿勢の影響を受けます。",
            "一定距離以内では照準レティクル（○）が表示され、目標に合わせて撃ちやすくなります。",
            "目標の進行方向を少し先読みして、短く撃つと調整しやすくなります。"
        },
        new[]
        {
            "機銃とミサイルの特徴",
            "非誘導の機銃は当てるのに腕が問われますが、命中コースで射撃できれば目標に回避を許しません。",
            "ミサイルは距離や角度、再装填を見て使い分けてください。"
        },
        new[]
        {
            "ミサイルの回避",
            "ミサイルに対し横を向いただけでは回避できません。",
            "R1でできる限り加速し、バレルロールで未来位置を乱すのが有効です。"
        },
        new[]
        {
            "マルチロック",
            "複数目標を同時にロックできる武装では、ロック数と対象を確認してから発射します。",
            "多数の敵を素早く処理したい場面で有効ですが、弾数管理が重要です。"
        },
        new[]
        {
            "UAV発着場",
            "UAV発着場では訓練用の目標を発進させます。周囲の範囲表示と看板を確認し、指定された空域で操作を試してください。",
            "M00では実機操作の確認を優先するため、詳細な説明はこの座学シーンで先に確認します。"
        }
    };

    ScrollRect scrollRect;
    TMP_FontAsset resolvedFont;

    void Start()
    {
        resolvedFont = ResolveNotoFont();
        BuildDocumentUi();
        Time.timeScale = 1f;
    }

    void Update()
    {
        InputManager input = InputManager.Instance;
        if (input == null || scrollRect == null)
            return;

        float scroll = -input.verticalL * StickScrollSpeed;

        if (input.up)
            scroll += ButtonScrollSpeed;
        if (input.down)
            scroll -= ButtonScrollSpeed;
        if (input.l1)
            scroll += ButtonScrollSpeed;
        if (input.r1)
            scroll -= ButtonScrollSpeed;

        if (Mathf.Abs(scroll) > 0.001f)
        {
            scrollRect.verticalNormalizedPosition = Mathf.Clamp01(
                scrollRect.verticalNormalizedPosition + scroll * Time.unscaledDeltaTime);
        }

        if (input.submit)
            SceneManager.LoadScene(NextScene);
        else if (input.cancel || input.menu)
            SceneManager.LoadScene(BackScene);
    }

    void BuildDocumentUi()
    {
        var canvasObject = new GameObject("DocumentCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var background = CreatePanel("Background", canvasObject.transform, new Color(0.02f, 0.035f, 0.045f, 0.98f));
        Stretch(background.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        TextMeshProUGUI title = CreateText("Title", canvasObject.transform, "M00 座学資料", 44f, FontStyles.Bold, TextAlignmentOptions.Left);
        Stretch(title.rectTransform, new Vector2(0.08f, 0.88f), new Vector2(0.92f, 0.96f), Vector2.zero, Vector2.zero);

        TextMeshProUGUI help = CreateText(
            "HelpText",
            canvasObject.transform,
            "左スティック / ↑↓ / L1 R1: スクロール    ○: 操作確認へ    × / メニュー: 戻る",
            24f,
            FontStyles.Normal,
            TextAlignmentOptions.Right);
        Stretch(help.rectTransform, new Vector2(0.08f, 0.04f), new Vector2(0.92f, 0.09f), Vector2.zero, Vector2.zero);

        var scrollObject = new GameObject("DocumentScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollObject.transform.SetParent(canvasObject.transform, false);
        var scrollImage = scrollObject.GetComponent<Image>();
        scrollImage.color = new Color(0.06f, 0.085f, 0.1f, 0.92f);
        Stretch(scrollObject.GetComponent<RectTransform>(), new Vector2(0.08f, 0.12f), new Vector2(0.92f, 0.86f), Vector2.zero, Vector2.zero);

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(scrollObject.transform, false);
        var viewportImage = viewport.GetComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
        viewport.GetComponent<Mask>().showMaskGraphic = false;
        Stretch(viewport.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(34f, 30f), new Vector2(-34f, -30f));

        var content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        var contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI body = CreateText("BodyText", content.transform, BuildDocumentText(), 28f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        body.textWrappingMode = TextWrappingModes.Normal;
        body.lineSpacing = 12f;
        body.paragraphSpacing = 18f;
        body.rectTransform.anchorMin = new Vector2(0f, 1f);
        body.rectTransform.anchorMax = new Vector2(1f, 1f);
        body.rectTransform.pivot = new Vector2(0.5f, 1f);
        body.rectTransform.anchoredPosition = Vector2.zero;
        body.rectTransform.sizeDelta = new Vector2(0f, 2000f);
        body.ForceMeshUpdate();

        float height = Mathf.Max(1600f, body.preferredHeight + 80f);
        contentRect.sizeDelta = new Vector2(0f, height);
        body.rectTransform.sizeDelta = new Vector2(0f, height);

        scrollRect = scrollObject.GetComponent<ScrollRect>();
        scrollRect.viewport = viewport.GetComponent<RectTransform>();
        scrollRect.content = contentRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 45f;
        scrollRect.verticalNormalizedPosition = 1f;
    }

    string BuildDocumentText()
    {
        var builder = new StringBuilder();

        for (int i = 0; i < DocumentSections.Length; i++)
        {
            string[] section = DocumentSections[i];
            if (section.Length == 0)
                continue;

            builder.Append("<size=132%><b>");
            builder.Append(section[0]);
            builder.AppendLine("</b></size>");

            for (int j = 1; j < section.Length; j++)
            {
                builder.Append("・");
                builder.AppendLine(section[j]);
            }

            if (i < DocumentSections.Length - 1)
                builder.AppendLine();
        }

        return builder.ToString();
    }

    Image CreatePanel(string objectName, Transform parent, Color color)
    {
        var panel = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        var image = panel.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    TextMeshProUGUI CreateText(string objectName, Transform parent, string value, float fontSize, FontStyles style, TextAlignmentOptions alignment)
    {
        var obj = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);

        var text = obj.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.Normal;

        if (resolvedFont != null)
            text.font = resolvedFont;

        return text;
    }

    static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    TMP_FontAsset ResolveNotoFont()
    {
#if UNITY_EDITOR
        TMP_FontAsset asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/NotoSansJP-Regular SDF.asset");
        if (asset != null)
            return asset;
#endif

        var uiTexts = FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include);
        foreach (var text in uiTexts)
        {
            if (text != null && text.font != null && text.font.name.Contains("NotoSansJP-Regular SDF"))
                return text.font;
        }

        var worldTexts = FindObjectsByType<TextMeshPro>(FindObjectsInactive.Include);
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
