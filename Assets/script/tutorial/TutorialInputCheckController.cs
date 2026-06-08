using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialInputCheckController : MonoBehaviour
{
    [Serializable]
    public class InputCheck
    {
        public string label;
        public bool completed;
    }

    public TextMeshProUGUI checklistText;
    public TextMeshProUGUI[] checkTexts;
    public string nextSceneName = "M00";
    public bool autoLoadNextScene;
    public float autoLoadDelay = 1.5f;

    public void SetLabel(TextMeshProUGUI[] labelTexts)
    {
        if (labelTexts == null) return;
        
        checks = new InputCheck[labelTexts.Length];
        for (int i = 0; i < labelTexts.Length; i++)
        {
            checks[i] = new InputCheck();
            if (labelTexts[i] != null)
                checks[i].label = labelTexts[i].text.ToString();
        }
    }

    public InputCheck[] checks;

    bool completed;
    float completedTimer;

    void Update()
    {
        var input = InputManager.Instance;
        if (input == null)
        {
            SetText("入力管理が見つかりません。");
            return;
        }

        UpdateChecks(input);
        completed = AreAllCompleted();
        UpdateText();

        if (!completed) return;

        if (input.submit)
        {
            LoadNextScene();
            return;
        }

        completedTimer += Time.deltaTime;
        if (autoLoadNextScene && completedTimer >= autoLoadDelay)
            LoadNextScene();
    }

    public void LoadNextScene()
    {
        if (string.IsNullOrEmpty(nextSceneName)) return;
        SceneManager.LoadScene(nextSceneName);
    }
    void UpdateChecks(InputManager input)
    {
        Complete(0, input.altr2 || input.altl2 || Mathf.Abs(input.r2 - input.l2) > 0.1f); // R2/L2: 左右ヨー
        Complete(1, input.l1 || input.accel < -0.1f);                                     // L1: 減速
        Complete(2, Mathf.Abs(input.horizontalL) > 0.35f);                                 // 左スティック左右: ロール
        Complete(3, Mathf.Abs(input.verticalL) > 0.35f);                                   // 左スティック上下: ピッチ
        Complete(4, input.stickL && input.accel < -0.1f);                                  // 左スティック押し込み + L1: 機動力制限解除

        Complete(5, input.r1 || input.accel > 0.1f);                                       // R1: 加速
        Complete(6, input.targetChange);                                                   // △: 目標切替
        Complete(7, input.changeWeapon);                                                   // □: 主兵装切替
        Complete(8, input.fireMissile);                                                    // ○: 主兵装発射
        Complete(9, input.fireGun);                                                        // ×: 機銃発射
        Complete(10, Mathf.Abs(input.horizontalR) > 0.35f || Mathf.Abs(input.verticalR) > 0.35f); // 右スティック: 視点移動
    }

    void Complete(int index, bool condition)
    {
        if (!condition) return;
        if (index < 0 || index >= checks.Length) return;
        checks[index].completed = true;
    }

    bool AreAllCompleted()
    {
        foreach (var check in checks)
        {
            if (check != null && !check.completed)
                return false;
        }

        return checks.Length > 0;
    }

    void UpdateText()
    {
        var text = "操作確認\n\n";
        for (int i = 0; i < checks.Length; i++)
        {
            var check = checks[i];
            if (check == null) continue;

            string line = (check.completed ? "[OK] " : "[--] ") + check.label;
            text += line + "\n";

            if (checkTexts != null && i < checkTexts.Length && checkTexts[i] != null)
            {
                checkTexts[i].text = check.completed
                    ? "<u><color=#66ff88>" + check.label + "</color></u>"
                    : check.label;
            }
        }

        if (completed)
            text += "\n操作確認完了。決定入力で M00 へ進みます。";

        if (checklistText != null)
            checklistText.text = text;
    }

    void SetText(string text)
    {
        if (checklistText != null)
            checklistText.text = text;
    }
}
