using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class selectmenuUI : MonoBehaviour
{
    public TextMeshProUGUI hudText;          // HUDテキスト
    public List<string> stage_name = new List<string>();

    public int selectedstage;

    // Start is called before the first frame update
    void Start()
    {
        Time.timeScale = 1f;
        RemoveUnavailableStages();
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

            for (int i = 0; i < stage_name.Count; i++)
            {
                hudText.text += Line(i, "Stage", stage_name[i]) + "\n";
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

    readonly Dictionary<string, string> missionText = new Dictionary<string, string>()
    {
        // レギオン: ブリーフィング文章はここへ手打ちで追加・調整する。
        {"M01","対空陣地中央の長射程地対空ミサイルを破壊せよ\n一定高度（900）以上を飛ぶと目標から長距離ミサイルが飛んでくるので低空侵入を推奨する" },
        {"M02","作戦空域内の未確認物体を強行偵察せよ" },
        {"M03","通常地形の作戦空域で敵航空隊を撃破せよ" },
        {"M04","空中戦艦を撃沈せよ" }
    };

    string GetSelectedStageName()
    {
        if (selectedstage < 0 || selectedstage >= stage_name.Count)
            return "";

        return stage_name[selectedstage];
    }

    string BuildMissionDescription(string stageName)
    {
        if (string.IsNullOrEmpty(stageName))
            return "ミッション情報を取得できません。";

        if (missionText.TryGetValue(stageName, out string authoredText))
            return authoredText;

        return "作戦空域内のすべての敵目標を撃破せよ。";
    }

    string Line(int index, string label, string value)
    {
        string head = (selectedstage == index) ? "> " : "  ";
        return head + stage_name[index];
    }


    void StageChange(float value)
    {
        if (stage_name.Count == 0)
            return;

        bool increase = value > 0;
        int numSubjects = stage_name.Count - 1;
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

    void RemoveUnavailableStages()
    {
        for (int i = stage_name.Count - 1; i >= 0; i--)
        {
            if (!IsSceneAvailable(stage_name[i]))
            {
                Debug.LogWarning("[selectmenuUI] Removed unavailable stage from list: " + stage_name[i]);
                stage_name.RemoveAt(i);
            }
        }
    }

    bool IsSceneAvailable(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return false;

        return SceneUtility.GetBuildIndexByScenePath("Assets/Scenes/" + sceneName + ".unity") >= 0;
    }

    int ClampStageIndex(int index)
    {
        if (stage_name.Count <= 0)
            return 0;

        return Mathf.Clamp(index, 0, stage_name.Count - 1);
    }
}

   
