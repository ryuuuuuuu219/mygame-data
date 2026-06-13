using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;

public struct SelectMenuTextView
{
    public string Title;
    public string MissionList;
    public string Description;
    public string ControlGuide;
}

public struct MissionDescription
{
    public int IDnumber;
    public string scenename;
    public string descriptions;

    public MissionDescription(int idNumber, string sceneName, string description)
    {
        IDnumber = idNumber;
        scenename = sceneName;
        descriptions = description;
    }
}

public static class SelectMenuText
{
    public const string DocumentStageName = "document";
    public const string TutorialStageName = "M00";

    static readonly MissionDescription[] MissionDescriptions = new MissionDescription[]
    {
        #region ブリーフィング文章はここへ手打ちで追加・調整する。
        new MissionDescription(-1, "document", "座学です\nこのゲームの世界観、用語、基本的な操作方法を説明します。"),
        new MissionDescription(-1, "M00", "チュートリアルです\n表示される案内に従って操作方法、飛行、ロックオン、攻撃を確認してください。"),
        new MissionDescription(0, "M01", "対空陣地中央の長射程地対空ミサイルを破壊せよ\n一定高度（900）以上を飛ぶと長距離ミサイルに狙われるので低空侵入を推奨する"),
        new MissionDescription(1, "M02", "作戦空域内の未確認物体を強行偵察せよ"),
        new MissionDescription(2, "M03", "敵航空隊を撃破せよ"),
        new MissionDescription(3, "M04", "艤装中の試作空中戦艦を撃沈せよ\n（空中戦艦の上空に電子支援機（JAMMER）が存在します\nその影響範囲はレーダーに表示され、その範囲内の敵のロックオンが阻害されます）"),
        new MissionDescription(4, "M05", "新兵器、制圧型対空砲とレールガンによる対空陣地を破壊せよ"),
        new MissionDescription(5, "M06", "敵勢力を殲滅、制空権を確保せよ"),
        new MissionDescription(6, "M07", "敵実験機・「エネルギー……吸収……アリーナ」搭載機を看破し、破壊せよ"),
        new MissionDescription(7, "M08", "新兵器、対空レーザー砲によって防御されている重レーザー砲を破壊せよ"),
        new MissionDescription(8, "M09", "「防御機動UAV」の母艦、重巡航管制機「ほにゃほにゃ」を破壊せよ"),
        new MissionDescription(9, "M10", "最新鋭機・兵装多様化重戦闘機「東方弾幕風っぽいやつ」を破壊せよ"),
        new MissionDescription(10, "M11", "「東方弾幕風っぽいやつ」母艦をふくめ、空中機動艦隊を撃破せよ"),
        new MissionDescription(11, "M12", "逃走、ジオフロントに侵入する最新鋭機「東方弾幕風っぽいやつ」を追撃せよ"),
        new MissionDescription(12, "M13", "対空制圧砲弾「やべーの」の弾着観測無人機「ドン引き」を破壊、ジオフロント崩壊を阻止せよ"),
        new MissionDescription(13, "M14", "対空制圧砲弾母艦、改修型重巡航管制機「ほにゃほにゃ」を破壊せよ")
        #endregion
    };

    public static List<string> BuildSelectableStageKeys(Func<string, bool> isSceneAvailable)
    {
        return MissionDescriptions
            .Where(mission => IsSelectableStage(mission, isSceneAvailable))
            .Select(mission => mission.scenename)
            .ToList();
    }

    public static List<string> BuildSortieStageKeys(Func<string, bool> isSceneAvailable)
    {
        return MissionDescriptions
            .Where(mission => mission.IDnumber >= 0 && IsSelectableStage(mission, isSceneAvailable))
            .OrderBy(mission => mission.IDnumber)
            .Select(mission => mission.scenename)
            .ToList();
    }

    public static bool IsSelectableStage(string stageName, Func<string, bool> isSceneAvailable)
    {
        if (!TryFindMission(stageName, out MissionDescription mission))
            return false;

        return IsSelectableStage(mission, isSceneAvailable);
    }

    public static string GetStageName(IReadOnlyList<string> selectableStageKeys, int index)
    {
        if (selectableStageKeys == null || index < 0 || index >= selectableStageKeys.Count)
            return "";

        return selectableStageKeys[index];
    }

    public static string GetSelectableMissionBySortieIndex(int sortieIndex, Func<string, bool> isSceneAvailable)
    {
        if (sortieIndex < 0)
            return "";

        MissionDescription mission = MissionDescriptions.FirstOrDefault(item => item.IDnumber == sortieIndex);
        if (string.IsNullOrEmpty(mission.scenename))
            return "";

        return IsSelectableStage(mission, isSceneAvailable) ? mission.scenename : "";
    }

    public static bool TryGetSortieIndex(string stageName, out int sortieIndex)
    {
        sortieIndex = -1;

        if (!TryFindMission(stageName, out MissionDescription mission) || mission.IDnumber < 0)
            return false;

        sortieIndex = mission.IDnumber;
        return true;
    }

    public static string BuildMissionDescription(string stageName)
    {
        if (string.IsNullOrEmpty(stageName))
            return "ミッション情報を取得できません。";

        if (TryFindMission(stageName, out MissionDescription mission))
            return mission.descriptions;

        return "作戦空域内のすべての敵目標を撃破せよ。";
    }

    public static SelectMenuTextView BuildMenu(IReadOnlyList<string> selectableStageKeys, int selectedStage)
    {
        return new SelectMenuTextView
        {
            Title = "ミッション選択",
            MissionList = BuildMissionListText(selectableStageKeys, selectedStage),
            Description = "",
            ControlGuide = BuildControlGuideText()
        };
    }

    public static SelectMenuTextView BuildBriefing(string stageName)
    {
        return new SelectMenuTextView
        {
            Title = "ミッション説明",
            MissionList = "ミッション: " + stageName,
            Description = BuildMissionDescription(stageName),
            ControlGuide = BuildControlGuideText()
        };
    }

    static bool IsSelectableStage(MissionDescription mission, Func<string, bool> isSceneAvailable)
    {
        if (string.IsNullOrEmpty(mission.scenename))
            return false;

        return mission.IDnumber < 0 || (isSceneAvailable != null && isSceneAvailable(mission.scenename));
    }

    static bool TryFindMission(string stageName, out MissionDescription mission)
    {
        for (int i = 0; i < MissionDescriptions.Length; i++)
        {
            if (MissionDescriptions[i].scenename == stageName)
            {
                mission = MissionDescriptions[i];
                return true;
            }
        }

        mission = default;
        return false;
    }

    static string BuildMissionListText(IReadOnlyList<string> selectableStageKeys, int selectedStage)
    {
        if (selectableStageKeys == null)
            return "";

        var builder = new StringBuilder();
        for (int i = 0; i < selectableStageKeys.Count; i++)
            builder.AppendLine(BuildMissionLine(i, selectableStageKeys[i], selectedStage));

        return builder.ToString();
    }

    static string BuildMissionLine(int index, string stageName, int selectedStage)
    {
        string head = (selectedStage == index) ? "> " : "  ";
        return head + stageName;
    }

    static string BuildControlGuideText()
    {
        return "〇 決定\n× 戻る";
    }
}

public partial class selectmenuUI
{
    public TextMeshProUGUI hudText;          // HUDテキスト
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI missionListText;
    public TextMeshProUGUI missionDescriptionText;
    public TextMeshProUGUI controlGuideText;

    void UpdateMenuText()
    {
        ApplyText(SelectMenuText.BuildMenu(selectableStageKeys, selectedstage));
    }

    void UpdateBriefingText()
    {
        ApplyText(SelectMenuText.BuildBriefing(GetSelectedStageName()));
    }

    void ApplyText(SelectMenuTextView textView)
    {
        EnsureSplitTextObjects();
        SetMissionTextVisible(true);

        SetText(titleText, textView.Title);
        SetText(missionListText, textView.MissionList);
        SetText(missionDescriptionText, textView.Description);
        SetText(controlGuideText, textView.ControlGuide);
        ClearHudText();
    }

    void EnsureSplitTextObjects()
    {
        if (hudText == null) return;

        if (titleText == null)
            titleText = CreateSplitText("MissionTitleText", new Vector2(0f, 0f), new Vector2(0f, -80f), 24f);

        if (missionListText == null)
            missionListText = CreateSplitText("MissionListText", new Vector2(0f, -80f), new Vector2(0f, -120f), 18f);

        if (missionDescriptionText == null)
            missionDescriptionText = CreateSplitText("MissionDescriptionText", new Vector2(0f, -140f), new Vector2(0f, -80f), 18f);

        if (controlGuideText == null)
            controlGuideText = CreateSplitText("MissionControlGuideText", new Vector2(0f, -360f), new Vector2(0f, -120f), 18f);
    }

    TextMeshProUGUI CreateSplitText(string objectName, Vector2 positionOffset, Vector2 sizeOffset, float fontSize)
    {
        var text = Instantiate(hudText, hudText.transform.parent);
        text.name = objectName;
        text.text = "";
        text.fontSize = fontSize;
        text.lineSpacing = 0f;
        text.raycastTarget = false;

        var rect = text.rectTransform;
        rect.anchoredPosition += positionOffset;
        rect.sizeDelta += sizeOffset;

        return text;
    }

    void SetMissionTextVisible(bool visible)
    {
        SetTextVisible(titleText, visible);
        SetTextVisible(missionListText, visible);
        SetTextVisible(missionDescriptionText, visible);
        SetTextVisible(controlGuideText, visible);
    }

    void SetTextVisible(TextMeshProUGUI text, bool visible)
    {
        if (text != null)
            text.gameObject.SetActive(visible);
    }

    void SetText(TextMeshProUGUI text, string value)
    {
        if (text != null)
            text.text = value;
    }

    void ClearHudText()
    {
        if (hudText != null)
            hudText.text = "";
    }
}