using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;



public class SetupUI : MonoBehaviour
{
    public TextMeshProUGUI hudText;          // HUDテキスト

    public TextMeshProUGUI pointText;          // HUDテキスト
    float scorepoint = 0;
    float totalpoints = 0f;
    int subjectindex = 0;

    public List<string> scene_name = new List<string>();

    [Header("General Status")]
    public List<float> hiscore = new List<float>();
    public float hiscore_total = 0;

    public int selectedstage = 0;

    public StatusTable playerstatus;

    enum Page
    {
        Aircraft = 0,
        Gun,
        StandardMissile,
        nAAM,
        UGB
    }
    static readonly int PageCount = Enum.GetValues(typeof(Page)).Length;

    Page currentPage = Page.Aircraft;


    int returnindex()
    {
        return playerstatus.statusdic[(int)currentPage].stats.Length;
    }

    // Start is called before the first frame update
    void Start()
    {
        var status = GetComponent<AugumentStatus>();

        if (status.IsInitialized)
            InitFromStatus();
        else
            status.OnInitialized += InitFromStatus;
    }

    void InitFromStatus() 
    {
        playerstatus = GetComponent<AugumentStatus>().CurrentStatus;

        selectedstage = PlayerPrefs.GetInt("selectedstage", 0);

        if (hudText == null)
        {
            Debug.LogError("HUD Text is not assigned.");
            return;
        }
        // スコア配列を取得
        GetHiscoreArray();

        AllStatusLoad();

        // HUDテキストの更新
        UpdateText();
    }

    enum StickLock
    {
        None,
        Horizontal,
        Vertical
    }

    float interval = 1f;
    float holdtime = 0.5f;

    StickLock stickLock = StickLock.None;

    // Update is called once per frame
    void Update()
    {
        var keyInput = InputManager.Instance;

        if (keyInput != null)
        {
            float h = keyInput.horizontalL;
            float v = keyInput.verticalL;

            if (keyInput.up)//十字キー上取得
            {
                v = -1f;
            }
            else if (keyInput.down)//十字キー下取得
            {
                v = 1f;
            }
            if(keyInput.left)//十字キー左取得
            {
                h = -1f;
            }
            else if (keyInput.right)//十字キー右取得
            {
                h = 1f;
            }

            switch (stickLock)
            {
                case StickLock.None:
                    if (Mathf.Abs(h) > 0.1f)
                    {
                        ValueChange(h, subjectindex);
                        UpdateText();
                        stickLock = StickLock.Horizontal;
                    }
                    else if (Mathf.Abs(v) > 0.1f)
                    {
                        SubjectChange(v, subjectindex);
                        UpdateText();
                        stickLock = StickLock.Vertical;
                    }
                    break;

                case StickLock.Horizontal:
                    v = 0f;
                    if (Mathf.Abs(h) < 0.1f)
                    {
                        stickLock = StickLock.None;
                        interval = 1f;
                        holdtime = 0.5f;
                    }
                    else
                    {
                        interval -= Time.deltaTime;
                        if (interval < 0f)
                        {
                            interval = holdtime;
                            holdtime *= 0.9f;
                            ValueChange(h, subjectindex);
                            UpdateText();
                        }
                    }
                    break;

                case StickLock.Vertical:
                    h = 0f;
                    if (Mathf.Abs(v) < 0.1f)
                    {
                        stickLock = StickLock.None;
                        interval = 1f;
                        holdtime = 0.5f;
                    }
                    else
                    {
                        interval -= Time.deltaTime;
                        if (interval < 0f)
                        {
                            interval = holdtime;
                            holdtime *= 0.9f;
                            SubjectChange(v, subjectindex);
                            UpdateText();
                        }
                    }
                    break;
            }

            if (keyInput.submit)//〇ボタン押下時
            {
                AllStatusSave();
                SceneManager.LoadScene(scene_name[selectedstage]);
            }
            if (keyInput.cancel)//✕ボタン押下時
            {
                AllStatusSave();
                SceneManager.LoadScene("Briefing");
            }
        }

    }

    void SubjectChange(float value, int currentSubject)
    {
        bool increase = value > 0;
        int numSubjects = returnindex(); 
        int maxIndex = numSubjects; // 0〜numSubjects
        subjectindex = (subjectindex + (increase ? 1 : -1) + maxIndex + 1) % (maxIndex + 1);
    }

    void ValueChange(float value, int subject)
    {
        bool increase = value > 0;
        
        if (subject == 0)//ページ変更
        {
            AllStatusSave();
            if (increase)
            {
                currentPage = (Page)(((int)currentPage + 1) % PageCount);
            }
            else
            {
                currentPage = (Page)(((int)currentPage - 1 + PageCount) % PageCount);
            }

        }
        else//ステータス変更
        {
            StatEntry entry = playerstatus.statusdic[(int)currentPage].stats[subject - 1];
            string statname = entry.key;
            modify mod = entry.range;

            ref float currentvalue = ref playerstatus.GetVar(statname);

            float step = Mathf.Abs(mod.step);
            float delta = increase ? step : -step;

            // 「UI的に逆効果」のステータスなら反転
            if (mod.step < 0)
                delta = -delta;

            // 次の値
            float nextValue = currentvalue + delta;

            // 範囲チェック
            if (nextValue < mod.Lower || nextValue > mod.Upper)
                return;

            // ポイント不足チェック
            if (increase && scorepoint <= 0)
                return;

            // 反映
            currentvalue = nextValue;

            // ポイント処理（UI基準）
            if (increase)
            {
                scorepoint--;
                totalpoints += 1f;
            }
            else
            {
                scorepoint++;
                totalpoints -= 1f;
            }

        }
    }

    float loopedStatusLoad(string key)
    {
        float val;
        StatEntry entry = playerstatus.SearchOfkey(key, out val);
        modify mod = entry.range;

        if(val < mod.min || val > mod.max)
        {
            val = mod.min;
            playerstatus.GetVar(key) = val;
        }
        float point = (val - mod.min) / mod.step;
        return point;
    }

    void AllStatusLoad()
    {
        totalpoints = 0f;

        // ステータスの読み込み
        foreach (var page in playerstatus.statusdic)
        {
            foreach (var stat in page.stats)
            {
                float points = loopedStatusLoad(stat.key);
                totalpoints += points;
            }
        }



        scorepoint -= totalpoints;

        // ポイントがマイナスにならないように調整 デバッグ中は無効化
        //scorepoint = Mathf.Max(0, scorepoint - totalpoints);
    }

    void AllStatusSave()
    {
        // ステータスの保存
        foreach (var page in playerstatus.statusdic)
        {
            foreach (var stat in page.stats)
            {
                PlayerPrefs.SetFloat(stat.key, playerstatus.GetVar(stat.key));
            }
        }



        PlayerPrefs.Save();
    }

    string Line(int index, string label, string value)
    {
        string head = (subjectindex == index) ? "> " : "  ";
        return head + label + ": " + value + "\n";
    }
    void UpdateText()
    {
        hudText.text = Line(0, "ページ切り替え", "");

        switch (currentPage)
        {
            case Page.Aircraft:
                hudText.text += "■ 機体ステータス ■\n";
                break;
            case Page.Gun:
                hudText.text += "■ 機銃ステータス ■\n";
                break;
            case Page.StandardMissile:
                hudText.text += "■ 標準ミサイルステータス ■\n";
                break;
            case Page.nAAM:
                hudText.text += "■ 武器システムステータス ■\n";
                break;
            case Page.UGB:
                hudText.text += "■ UGBステータス ■\n";
                break;
            default: break;
        }
        for (int i = 0; i < returnindex(); i++)
        {
            StatEntry entry = playerstatus.statusdic[(int)currentPage].stats[i];
            string key = entry.key;
            float value = playerstatus.GetVar(entry.key);
            hudText.text += Line(i + 1, key, value.ToString("F1"));
        }

        pointText.text = $"獲得ポイント: {scorepoint:F0} pt\n"+
            $"使用ポイント: {totalpoints:F0} pt";
    }


    void GetHiscoreArray()
    {
        hiscore.Clear();
        hiscore_total = 0;
        foreach (var name in scene_name)
        {
            float score = PlayerPrefs.GetFloat(name + "_hiscore", 0);
            hiscore.Add(score);
            hiscore_total += score;
        }

        scorepoint = Mathf.Floor(hiscore_total * 0.1f);

        // デバッグ用
        scorepoint += 10000f;
    }
}
