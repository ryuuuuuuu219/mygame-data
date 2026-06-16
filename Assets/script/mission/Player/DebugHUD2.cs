using UnityEngine;
using TMPro;

public class DebugHUD2 : MonoBehaviour
{
    [Header("HUD Colors")]
    public Color normalHudColor = Color.green;
    public Color missileAlertHudColor = Color.red;

    public AlartSystem alartSystem;
    public AugumentStatus status;
    public TextMeshProUGUI hpText;
    TextMeshProUGUI scoreTimeText;


    public GameObject alarttextUI;
    public GameObject hitUI;
    public GameObject altTimerUI;
    [SerializeField] SpawnTableManager spawnTableManager;

    public bool hit;
    public bool destroyed;
    float timer = 1f;

    public WeaponSystem weaponSystem;

    void Start()
    {
        CreateScoreTimeText();
    }

    private void LateUpdate()
    {
        bool missilelocked = IsMissileAlertActive();
        ApplyHudTextColor(missilelocked ? missileAlertHudColor : normalHudColor);
        UpdateScoreTimeText();
        ApplyHudTextColor(missilelocked ? missileAlertHudColor : normalHudColor);

        if (spawnTableManager!=null && spawnTableManager.isStageClear)
        {
            altTimerUI.GetComponent<TextMeshProUGUI>().text = "Stage Clear!" +
                "\nReturning to Base in " + spawnTableManager.toResultTimer.ToString("F1") + "s";

        }
        if (status != null)
        {
            int? mode = weaponSystem != null ? (int)weaponSystem.mode : null;
            string wcooldown= "Gun:" + weaponSystem.currentBullets.ToString("F0") + " / " + weaponSystem.maxBullets.ToString("F0") + "\n";

            switch (mode)
            {
                case 0:
                    wcooldown += weaponSystem != null ? "MSL: " + weaponSystem.currentMissiles.ToString("F0") + " / " + weaponSystem.maxMissiles.ToString("F0") + "\n" +
                        "\n" + weaponSystem.missileTimerA.ToString("F1")+"-"+ weaponSystem.missileTimerB.ToString("F1") 
                        : "N/A";
                    break;
                case 1:
                    wcooldown += "nAAM: " + weaponSystem.currentnAAM.ToString("F0") + " / " + weaponSystem.maxnAAM.ToString("F0") + "\n";
                    for (int i = 0; i < weaponSystem.multiTimers.Count; i++)
                    {
                        wcooldown += weaponSystem.multiTimers[i].ToString("F1")
                            + ((i % 2 == 0) ? "-" : "\n");
                    }


                    break;
                case 2:
                    wcooldown += "UGB: " + weaponSystem.currentUGB.ToString("F0") + " / " + weaponSystem.maxUGB.ToString("F0") + "\n" + 
                        weaponSystem.ugbTimer.ToString("F1");
                    break;
                default:
                    wcooldown += "N/A";
                    break;
            }
            float hp = status.hp;

            hpText.text = "HP:" + hp.ToString("F0")
                + "\nMode:" + mode+
                "\n"+wcooldown;

            bool islocked = IsLockAlertActive();
            if (missilelocked)
            {
                alarttextUI.GetComponent<TextMeshProUGUI>().text = "missile alert";
            }
            else if(islocked)
            {
                alarttextUI.GetComponent<TextMeshProUGUI>().text = "Warning";
            }
            else
            {
                alarttextUI.GetComponent<TextMeshProUGUI>().text = "";
            }
        }
        hit=ObjectManager.Instance.hitUIflag;
        destroyed = ObjectManager.Instance.destroyedUIflag;
        if (hit)
        {
            hitUI.GetComponent<TextMeshProUGUI>().text = "hit";
            timer -= Time.deltaTime;
            if (timer < 0f)
            {
                hit = false;
                timer = 1f;
            }
        }
        if (destroyed)
        {
            hitUI.GetComponent<TextMeshProUGUI>().text = "destroyed";
            timer -= Time.deltaTime;
            if (timer < 0f)
            {
                destroyed = false;
                timer = 1f;
            }
        }
        if(!hit && !destroyed)
        {
            hitUI.GetComponent<TextMeshProUGUI>().text = "";
        }
    }

    void CreateScoreTimeText()
    {
        if (scoreTimeText != null) return;

        Canvas parentCanvas = hpText != null ? hpText.GetComponentInParent<Canvas>() : FindFirstObjectByType<Canvas>();
        if (parentCanvas == null) return;

        var obj = new GameObject("ScoreTimeText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parentCanvas.transform, false);

        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(20f, -20f);
        rect.sizeDelta = new Vector2(360f, 90f);

        scoreTimeText = obj.GetComponent<TextMeshProUGUI>();
        scoreTimeText.fontSize = 24f;
        scoreTimeText.alignment = TextAlignmentOptions.TopLeft;
        scoreTimeText.raycastTarget = false;
        scoreTimeText.color = normalHudColor;
        scoreTimeText.text = "";
    }

    void UpdateScoreTimeText()
    {
        if (scoreTimeText == null)
            CreateScoreTimeText();
        if (scoreTimeText == null) return;

        float score = ObjectManager.Instance != null ? ObjectManager.Instance.score : 0f;
        float elapsedTime = spawnTableManager != null ? spawnTableManager.MissionElapsedTime : Time.timeSinceLevelLoad;
        int stageIndex = PlayerPrefs.GetInt("selectedstage", 0) + 1;
        float missionClearBonus = 2000f + 500f * stageIndex;
        float prePenaltyScore = missionClearBonus + score;
        float timePenalty = Mathf.Clamp(elapsedTime - 180f, 0f, prePenaltyScore);

        scoreTimeText.text =
            "Score: " + score.ToString("F0") +
            "\nTime: " + elapsedTime.ToString("F1") + "s" +
            "\nPre-Penalty Score: " + prePenaltyScore.ToString("F0") +
            "\nTime Penalty: -" + timePenalty.ToString("F0");
    }

    bool IsMissileAlertActive()
    {
        if (alartSystem == null || alartSystem.MissileArray == null)
        {
            return false;
        }

        foreach (bool b in alartSystem.MissileArray)
        {
            if (b) return true;
        }

        return false;
    }

    bool IsLockAlertActive()
    {
        if (alartSystem == null || alartSystem.LockingArray == null)
        {
            return false;
        }

        foreach (bool b in alartSystem.LockingArray)
        {
            if (b) return true;
        }

        return false;
    }

    void ApplyHudTextColor(Color color)
    {
        if (hpText != null) hpText.color = color;
        if (scoreTimeText != null) scoreTimeText.color = color;
        SetTextColor(alarttextUI, color);
        SetTextColor(hitUI, color);
        SetTextColor(altTimerUI, color);
    }

    void SetTextColor(GameObject obj, Color color)
    {
        if (obj == null) return;

        TextMeshProUGUI text = obj.GetComponent<TextMeshProUGUI>();
        if (text != null) text.color = color;
    }
}
