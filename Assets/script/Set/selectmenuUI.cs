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
        if (SceneManager.GetActiveScene().name == "Briefing")
        {
            selectedstage = PlayerPrefs.GetInt("selectedstage", 0);
        }
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
            v = 1f;
        }
        else if (keyInput.down)//十字キー下取得
        {
            v = -1f;
        }
        if (SceneManager.GetActiveScene().name == "Menu")
        {
            hudText.text = "Select Mission\n\n";

            for (int i = 0; i < stage_name.Count; i++)
            {
                hudText.text += Line(i, "Stage", stage_name[i]) + "\n";
            }

            hudText.text +=
                "Press O to Confirm\n" +
                "Press X to Cancel";
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
            hudText.text = "Mission Briefing\n\n" +
                "Mission: " + stage_name[selectedstage] + "\n\n" +
                text[stage_name[selectedstage]] + "\n\n" +
                "Press O to Confirm\n" +
                "Press X to Cancel";
        }
    }

    Dictionary<string, string> text= new Dictionary<string, string>()
    {
        {"M01","Eliminate all enemy targets in the area." },
        {"M02","Eliminate all enemy targets in the area." }
    };

    string Line(int index, string label, string value)
    {
        string head = (selectedstage == index) ? "> " : "  ";
        return head + stage_name[index];
    }


    void StageChange(float value)
    {
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
    }
}

   