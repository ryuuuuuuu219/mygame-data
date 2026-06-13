using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class selectmenuUI : MonoBehaviour
{
    const string SelectedMissionKeyPref = "selectedMissionKey";
    const string SelectedSortieIndexPref = "selectedSortieIndex";

    public int selectedstage;
    List<string> selectableStageKeys;

    // Start is called before the first frame update
    void Start()
    {
        Time.timeScale = 1f;
        selectableStageKeys = BuildSelectableStageKeys();

        if (SceneManager.GetActiveScene().name == "Briefing")
        {
            selectedstage = ClampStageIndex(GetSavedSelectionIndex());
        }
        else
        {
            selectedstage = ClampStageIndex(selectedstage);
        }

        PlayerPrefs.SetInt("selectedstage", selectedstage);
        EnsureSplitTextObjects();
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
                        string stageName = GetSelectedStageName();
                        SaveSelectedStage(stageName);

                        if (SelectMenuText.TryGetSortieIndex(stageName, out int sortieIndex))
                        {
                            PlayerPrefs.SetInt(SelectedSortieIndexPref, sortieIndex);
                            PlayerPrefs.SetInt("selectedstage", sortieIndex);
                            PlayerPrefs.Save();
                            SceneManager.LoadScene("SetUp");
                        }
                        else
                        {
                            Debug.LogError("[selectmenuUI] Unsupported stage: " + stageName);
                        }
                    }
                    else if (SceneManager.GetActiveScene().name == "Menu")
                    {
                        string stageName = GetSelectedStageName();
                        SaveSelectedStage(stageName);

                        if (stageName == SelectMenuText.DocumentStageName)
                        {
                            SceneManager.LoadScene("document");
                        }
                        else if (stageName == SelectMenuText.TutorialStageName)
                        {
                            SceneManager.LoadScene("preM00");
                        }
                        else
                        {
                            SaveSelectedStage(GetSelectedStageName());
                            SceneManager.LoadScene("Briefing");
                        }
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
            UpdateMenuText();
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
            UpdateBriefingText();
        }
    }

    int missionCount => selectableStageKeys != null ? selectableStageKeys.Count : 0;

    public List<string> stageNames()
    {
        return BuildSelectableStageKeys();
    }

    List<string> BuildSelectableStageKeys()
    {
        return SelectMenuText.BuildSelectableStageKeys(IsSceneAvailable);
    }

    int GetSavedSelectionIndex()
    {
        string savedStageName = PlayerPrefs.GetString(SelectedMissionKeyPref, "");
        if (!string.IsNullOrEmpty(savedStageName))
        {
            int index = selectableStageKeys.IndexOf(savedStageName);
            if (index >= 0)
                return index;
        }

        int sortieIndex = PlayerPrefs.GetInt(SelectedSortieIndexPref, PlayerPrefs.GetInt("selectedstage", 0));
        if (sortieIndex >= 0)
        {
            string sortieStage = SelectMenuText.GetSelectableMissionBySortieIndex(sortieIndex, IsSceneAvailable);
            if (!string.IsNullOrEmpty(sortieStage))
            {
                int index = selectableStageKeys.IndexOf(sortieStage);
                if (index >= 0)
                    return index;
            }
        }

        return 0;
    }

    void SaveSelectedStage(string stageName)
    {
        PlayerPrefs.SetString(SelectedMissionKeyPref, stageName ?? "");

        if (SelectMenuText.TryGetSortieIndex(stageName, out int sortieIndex))
            PlayerPrefs.SetInt(SelectedSortieIndexPref, sortieIndex);

        PlayerPrefs.Save();
    }

    string GetSelectedStageName()
    {
        return SelectMenuText.GetStageName(selectableStageKeys, selectedstage);
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