using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class selectmenuUI : MonoBehaviour
{
    public TextMeshProUGUI hudText;          // HUDテキスト

    public int selectedstage;

    // Start is called before the first frame update
    void Start()
    {
        Time.timeScale = 1f;
        if (SceneManager.GetActiveScene().name == "Briefing")
        {
            selectedstage = PlayerPrefs.GetInt("selectedstage", 0);
        }

        selectedstage = ClampStageIndex(selectedstage);
        PlayerPrefs.SetInt("selectedstage", selectedstage);
    }

    bool maruflag = false;
    bool batsuflag = false;

    float interval = 0f;
    float holdtime = 0.5f;

    // Update is called once per frame
    void Update()
    {
        var keyInput = InputManager.Instance;
        if(keyInput == null)
        {
            return;
        }

        if (keyInput != null)
        {
            if(keyInput.cancel)//✕ボタン押下時
            {
                if (!batsuflag)
                {
                    batsuflag = true;
                    GeneratedAudioManager.Play(GeneratedAudioCue.UiCancel);
                    if (SceneManager.GetActiveScene().name == "Menu")
                    {
                        SceneManager.LoadScene("Title");
                    }
                    else if (SceneManager.GetActiveScene().name == "Briefing")
                    {
                        SceneManager.LoadScene("Menu");
                    }
                }
            }
            else
            {
                batsuflag = false;
            }

            if (keyInput.submit)//〇ボタン押下時
            {
                if (!maruflag)
                {
                    maruflag = true;
                    GeneratedAudioManager.Play(GeneratedAudioCue.UiSubmit);
                    if (SceneManager.GetActiveScene().name == "Briefing")
                    {
                        PlayerPrefs.SetInt("selectedstage", selectedstage);

                        PlayerPrefs.Save();

                        SceneManager.LoadScene("SetUp");
                    }
                    else if (SceneManager.GetActiveScene().name == "Menu")
                    {
                        PlayerPrefs.SetInt("selectedstage", selectedstage);

                        PlayerPrefs.Save();

                        SceneManager.LoadScene("Briefing");
                    }
                    else if (SceneManager.GetActiveScene().name == "Title")
                    {
                        SceneManager.LoadScene("Menu");
                    }
                }
            }
            else
            {
                maruflag = false;
            }
        }
        float v = keyInput.verticalL;//Lスティック上下取得→十字キーを優先
        if(keyInput.up)//十字キー上取得
        {
            v = -1f;
        }
        else if (keyInput.down)//十字キー下取得
        {
            v = 1f;
        }
        if (SceneManager.GetActiveScene().name == "Menu")
        {
            hudText.text = "ミッション選択\n\n";

            for (int i = 0; i < missionCount; i++)
            {
                hudText.text += Line(i, GetStagename(i)) + "\n";
            }

            hudText.text +=
                "〇 決定\n" +
                "× 戻る";
            if (Mathf.Abs(v) > 0.1f)
            {
                interval -= Time.deltaTime;
                if (interval < 0f)
                {
                    interval = holdtime;
                    StageChange(v);
                }
            }
            else
            {
                interval = 0f;
                holdtime = 0.5f;
            }
        }
        else if (SceneManager.GetActiveScene().name == "Briefing")
        {
            string stageName = GetSelectedStageName();

            hudText.text = "ミッション説明\n\n" +
                "ミッション: " + stageName + "\n\n" +
                BuildMissionDescription(stageName) + "\n\n" +
                "〇 決定\n" +
                "× 戻る";
        }
    }

    int missionCount => missionText.Count;
    readonly Dictionary<string, string> missionText = new Dictionary<string, string>()
    {
        #region ブリーフィング文章はここへ手打ちで追加・調整する。
        {"Document","座学です\nこのゲームの世界観、用語、基本的な操作方法を説明します。"},
        {"M00","チュートリアルです\n表示される案内に従って操作方法、飛行、ロックオン、攻撃を確認してください。"},
        {"M01","対空陣地中央の長射程地対空ミサイルを破壊せよ\n一定高度（900）以上を飛ぶと長距離ミサイルに狙われるので低空侵入を推奨する" },
        {"M02","作戦空域内の未確認物体を強行偵察せよ" },
        {"M03","敵航空隊を撃破せよ" },
        {"M04","艤装中の試作空中戦艦を撃沈せよ\n（空中戦艦の上空に電子支援機（JAMMER）が存在します\nその影響範囲はレーダーに表示され、その範囲内の敵のロックオンが阻害されます）" },
        {"M05","新兵器、制圧型対空砲とレールガンによる対空陣地を破壊せよ" },
        {"M06","敵勢力を殲滅、制空権を確保せよ" },
        {"M07","敵実験機・「エネルギー……吸収……アリーナ」搭載機を看破し、破壊せよ" },
        {"M08","新兵器、対空レーザー砲によって防御されている重レーザー砲を破壊せよ" },
        {"M09","「防御機動UAV」の母艦、重巡航管制機「ほにゃほにゃ」を破壊せよ" },
        {"M10","最新鋭機・兵装多様化重戦闘機「東方弾幕風っぽいやつ」を破壊せよ" },
        {"M11","「東方弾幕風っぽいやつ」母艦をふくめ、空中機動艦隊を撃破せよ" },
        {"M12","逃走、ジオフロントに侵入する最新鋭機「東方弾幕風っぽいやつ」を追撃せよ" },
        {"M13","対空制圧砲弾「やべーの」の弾着観測無人機「ドン引き」を破壊、ジオフロント崩壊を阻止せよ" },
        {"M14","対空制圧砲弾母艦、改修型重巡航管制機「ほにゃほにゃ」を破壊せよ" }

        #endregion

    };

    public List<string> stageNames()
    {
        return missionText.Keys.ToList();
    }

    /*
    復号用
    「エネルギー……吸収……アリーナ」「BHS」（バレット・ヘル・システム）
    「防御機動UAV」　そのまま
    「やべーの」　「彼岸花」
    「ドン引き」　「向日葵」
    「ほにゃほにゃ」「庭園」
    「東方弾幕風っぽいやつ」「花束」
    
    */

    string GetSelectedStageName()
    {
        if (selectedstage < 0 || selectedstage >= missionCount)
            return "";

        return missionText.Keys.ElementAt(selectedstage);
    }

    string GetStagename(int index)
    {
        if (index < 0 || index >= missionCount)
            return "";
        string r = missionText.Keys.ElementAt(index);
        return r;
    }

    string BuildMissionDescription(string stageName)
    {
        if (string.IsNullOrEmpty(stageName))
            return "ミッション情報を取得できません。";

        if (missionText.TryGetValue(stageName, out string authoredText))
            return authoredText;

        return "作戦空域内のすべての敵目標を撃破せよ。";
    }

    string Line(int index, string value)
    {
        string head = (selectedstage == index) ? "> " : "  ";
        return head + GetStagename(index);
    }


    void StageChange(float value)
    {
        if (missionCount == 0)
            return;

        bool increase = value > 0;
        int numSubjects = missionCount - 1;
        if (increase)
        {
            selectedstage++;
            if (selectedstage > numSubjects) selectedstage = 0;
        }
        else
        {
            selectedstage--;
            if (selectedstage < 0) selectedstage = numSubjects;
        }
        GeneratedAudioManager.Play(GeneratedAudioCue.UiMove);
    }

    bool IsSceneAvailable(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return false;

        return SceneUtility.GetBuildIndexByScenePath("Assets/Scenes/" + sceneName + ".unity") >= 0;
    }

    int ClampStageIndex(int index)
    {
        if (missionCount <= 0)
            return 0;

        return Mathf.Clamp(index, 0, missionCount - 1);
    }
}

   
