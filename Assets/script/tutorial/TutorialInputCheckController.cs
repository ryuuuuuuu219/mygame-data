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

    public InputCheck[] checks =
    {
        new InputCheck { label = "左スティック左右: ロール" },
        new InputCheck { label = "左スティック上下: ピッチ" },
        new InputCheck { label = "右スティック: 視点移動" },
        new InputCheck { label = "R1: 加速" },
        new InputCheck { label = "L1: 減速" },
        new InputCheck { label = "R2/L2: ヨー" },
        new InputCheck { label = "左スティック押し込み + 減速: 機動制限解除" },
        new InputCheck { label = "△: 目標切替" },
        new InputCheck { label = "○: ミサイル / 選択兵装発射" },
        new InputCheck { label = "×: 機銃" },
        new InputCheck { label = "□: 兵装切替" },
    };

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
        Complete(0, Mathf.Abs(input.horizontalL) > 0.35f);
        Complete(1, Mathf.Abs(input.verticalL) > 0.35f);
        Complete(2, Mathf.Abs(input.horizontalR) > 0.35f || Mathf.Abs(input.verticalR) > 0.35f);
        Complete(3, input.r1 || input.accel > 0.1f);
        Complete(4, input.l1 || input.accel < -0.1f);
        Complete(5, input.altr2 || input.altl2 || Mathf.Abs(input.r2 - input.l2) > 0.1f);
        Complete(6, input.stickL && input.accel < -0.1f);
        Complete(7, input.targetChange);
        Complete(8, input.fireMissile);
        Complete(9, input.fireGun);
        Complete(10, input.changeWeapon);
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
