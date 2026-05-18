using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SetupUI : MonoBehaviour
{
    public TextMeshProUGUI hudText;
    public TextMeshProUGUI pointText;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemPointHeaderText;
    public TextMeshProUGUI itemValueHeaderText;
    public TextMeshProUGUI itemPrevNextHeaderText;
    public TextMeshProUGUI itemPointText;
    public TextMeshProUGUI itemValueText;
    public TextMeshProUGUI itemPrevNextText;
    public List<string> scene_name = new List<string>();

    const string StdIndexKey = "WeaponSelectIndex_stdm";
    const string GunIndexKey = "WeaponSelectIndex_gun";
    const string SpwIndexKey = "WeaponSelectIndex_spw";

    enum ScreenMode
    {
        Main,
        StandardMissile,
        Gun,
        SpecialWeapon
    }

    enum StickLock
    {
        None,
        Horizontal,
        Vertical
    }

    ScreenMode screenMode = ScreenMode.Main;
    StickLock stickLock = StickLock.None;

    int selectedstage;
    int mainIndex;
    int stdmIndex;
    int gunIndex;
    int spwIndex;
    int detailPage;

    float interval = 1f;
    float holdtime = 0.5f;

    readonly List<WeaponDropData> stdmWeapons = new List<WeaponDropData>();
    readonly List<WeaponDropData> gunWeapons = new List<WeaponDropData>();
    readonly List<WeaponDropData> spwWeapons = new List<WeaponDropData>();
    readonly StringBuilder textBuilder = new StringBuilder(512);

    void Start()
    {
        if (hudText != null)
        {
            hudText.fontSize = 18f;
            hudText.lineSpacing = 0f;
        }
        if (pointText != null)
        {
            pointText.fontSize = 18f;
            pointText.lineSpacing = 0f;
        }
        EnsureSplitTextObjects();

        RemoveUnavailableScenes();
        selectedstage = PlayerPrefs.GetInt("selectedstage", 0);
        selectedstage = ClampIndex(selectedstage, scene_name.Count);
        PlayerPrefs.SetInt("selectedstage", selectedstage);
        stdmIndex = PlayerPrefs.GetInt(StdIndexKey, 0);
        gunIndex = PlayerPrefs.GetInt(GunIndexKey, 0);
        spwIndex = PlayerPrefs.GetInt(SpwIndexKey, 0);
        ReloadWeapons();
        UpdateText();
    }

    void Update()
    {
        var keyInput = InputManager.Instance;
        if (keyInput == null) return;

        float h = keyInput.horizontalL;
        float v = keyInput.verticalL;

        if (keyInput.up) v = -1f;
        else if (keyInput.down) v = 1f;

        if (keyInput.left) h = -1f;
        else if (keyInput.right) h = 1f;

        HandleMove(h, v);

        if (keyInput.submit)
        {
            Submit();
        }
        else if (keyInput.cancel)
        {
            Back();
        }

        UpdateText();
    }

    void ReloadWeapons()
    {
        stdmWeapons.Clear();
        gunWeapons.Clear();
        spwWeapons.Clear();

        foreach (var weapon in WeaponStorage.LoadAll())
        {
            switch ((WeaponDropType)weapon.weaponTypeId)
            {
                case WeaponDropType.StandardMissile:
                    stdmWeapons.Add(weapon);
                    break;
                case WeaponDropType.Gun:
                    gunWeapons.Add(weapon);
                    break;
                case WeaponDropType.UGB:
                case WeaponDropType.nAAM:
                    spwWeapons.Add(weapon);
                    break;
            }
        }

        stdmIndex = ClampIndex(stdmIndex, stdmWeapons.Count);
        gunIndex = ClampIndex(gunIndex, gunWeapons.Count);
        spwIndex = ClampIndex(spwIndex, spwWeapons.Count);
    }

    void HandleMove(float h, float v)
    {
        switch (stickLock)
        {
            case StickLock.None:
                if (Mathf.Abs(h) > 0.1f)
                {
                    ChangeHorizontal(h);
                    stickLock = StickLock.Horizontal;
                }
                else if (Mathf.Abs(v) > 0.1f)
                {
                    ChangeVertical(v);
                    stickLock = StickLock.Vertical;
                }
                break;
            case StickLock.Horizontal:
                if (Mathf.Abs(h) < 0.1f)
                {
                    UnlockStick();
                }
                else
                {
                    Repeat(() => ChangeHorizontal(h));
                }
                break;
            case StickLock.Vertical:
                if (Mathf.Abs(v) < 0.1f)
                {
                    UnlockStick();
                }
                else
                {
                    Repeat(() => ChangeVertical(v));
                }
                break;
        }
    }

    void ChangeVertical(float value)
    {
        int delta = value > 0f ? 1 : -1;

        if (screenMode == ScreenMode.Main)
        {
            mainIndex = Wrap(mainIndex + delta, 5);
            return;
        }

        var list = CurrentList();
        if (list.Count == 0) return;

        SetCurrentIndex(Wrap(CurrentIndex() + delta, list.Count));
        SaveIndexes();
    }

    void ChangeHorizontal(float value)
    {
        if (screenMode == ScreenMode.Main) return;

        int delta = value > 0f ? 1 : -1;
        detailPage = Wrap(detailPage + delta, 3);
    }

    void Submit()
    {
        if (screenMode == ScreenMode.Main)
        {
            switch (mainIndex)
            {
                case 0:
                    Enter(ScreenMode.StandardMissile);
                    break;
                case 1:
                    Enter(ScreenMode.Gun);
                    break;
                case 2:
                    Enter(ScreenMode.SpecialWeapon);
                    break;
                case 3:
                    Sortie();
                    break;
                case 4:
                    SceneManager.LoadScene("Briefing");
                    break;
            }
            return;
        }

        var weapon = CurrentWeapon();
        if (weapon == null) return;

        WeaponStorage.Equip(weapon);
        ReloadWeapons();
        SaveIndexes();
    }

    void Back()
    {
        if (screenMode == ScreenMode.Main)
        {
            SceneManager.LoadScene("Briefing");
            return;
        }

        screenMode = ScreenMode.Main;
        detailPage = 0;
    }

    void Enter(ScreenMode mode)
    {
        screenMode = mode;
        detailPage = 0;
    }

    void Sortie()
    {
        WeaponStorage.ApplyEquippedToPlayerPrefs();

        if (selectedstage >= 0 && selectedstage < scene_name.Count)
        {
            string sceneName = scene_name[selectedstage];
            if (IsSceneAvailable(sceneName))
                SceneManager.LoadScene(sceneName);
            else
                Debug.LogError("[SetupUI] Scene is not available in build settings: " + sceneName);
        }
    }

    void RemoveUnavailableScenes()
    {
        for (int i = scene_name.Count - 1; i >= 0; i--)
        {
            if (!IsSceneAvailable(scene_name[i]))
            {
                Debug.LogWarning("[SetupUI] Removed unavailable scene from sortie list: " + scene_name[i]);
                scene_name.RemoveAt(i);
            }
        }
    }

    bool IsSceneAvailable(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return false;

        return SceneUtility.GetBuildIndexByScenePath("Assets/Scenes/" + sceneName + ".unity") >= 0;
    }

    void Repeat(System.Action action)
    {
        interval -= Time.deltaTime;
        if (interval >= 0f) return;

        interval = holdtime;
        holdtime *= 0.9f;
        action();
    }

    void UnlockStick()
    {
        stickLock = StickLock.None;
        interval = 1f;
        holdtime = 0.5f;
    }

    void SaveIndexes()
    {
        PlayerPrefs.SetInt(StdIndexKey, stdmIndex);
        PlayerPrefs.SetInt(GunIndexKey, gunIndex);
        PlayerPrefs.SetInt(SpwIndexKey, spwIndex);
        PlayerPrefs.Save();
    }

    void UpdateText()
    {
        if (hudText == null) return;

        if (screenMode == ScreenMode.Main)
        {
            UpdateMainText();
        }
        else
        {
            UpdateWeaponText();
        }
    }

    void UpdateMainText()
    {
        SetSplitTextVisible(false);

        textBuilder.Clear();
        textBuilder.Append("setting\n\n");
        AppendMainLine(0, "MSL");
        AppendMainLine(1, "GUN");
        AppendMainLine(2, "SPW");
        textBuilder.Append('\n');
        AppendMainLine(3, "sortie");
        AppendMainLine(4, "cancel");
        hudText.text = textBuilder.ToString();

        if (pointText != null)
            pointText.text = GetMainDescription();
    }

    void UpdateWeaponText()
    {
        SetSplitTextVisible(true);

        var list = CurrentList();
        var weapon = CurrentWeapon();

        if (weapon == null)
        {
            textBuilder.Clear();
            textBuilder.Append(GetModeTitle());
            textBuilder.Append("\nNo weapon.\n");
            hudText.text = textBuilder.ToString();
            SetSplitText("", "", "", "", "", "", "");
            if (pointText != null)
                pointText.text = "候補がありません";
            return;
        }

        var detail = WeaponStorage.BuildDetailColumns(weapon, detailPage);
        textBuilder.Clear();
        textBuilder.Append(detail.title);
        textBuilder.Append(GetEquippedLabel(weapon));
        textBuilder.Append("\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n");
        textBuilder.Append("ページ ");
        textBuilder.Append(detailPage + 1);
        textBuilder.Append("/3 ");
        textBuilder.Append(GetPageName());
        textBuilder.Append(" </>\nID ");
        textBuilder.Append(CurrentIndex() + 1);
        textBuilder.Append('/');
        textBuilder.Append(list.Count);
        textBuilder.Append(" ↑/↓");
        hudText.text = textBuilder.ToString();
        SetSplitText(
            "\n\n\n" + detail.labels,
            "\n\n" + detail.pointHeader,
            "\n\n" + detail.valueHeader,
            "\n\n" + detail.prevNextHeader,
            "\n\n\n" + detail.points,
            "\n\n\n" + detail.values,
            "\n\n\n" + detail.prevNextValues);

        if (pointText != null)
            pointText.text = GetWeaponDescription();
    }

    void AppendMainLine(int index, string label)
    {
        textBuilder.Append(mainIndex == index ? "> " : "  ");
        textBuilder.Append('\t');
        textBuilder.Append(label);
        textBuilder.Append('\n');
    }

    string GetMainDescription()
    {
        switch (mainIndex)
        {
            case 0: return "標準ミサイルを変更します";
            case 1: return "機銃を変更します";
            case 2: return "特殊兵装を変更します";
            case 3: return "出撃します";
            case 4: return "ブリーフィングへ戻ります";
            default: return "";
        }
    }

    string GetWeaponDescription()
    {
        switch (screenMode)
        {
            case ScreenMode.StandardMissile: return "標準ミサイルを選択します";
            case ScreenMode.Gun: return "機銃を選択します";
            case ScreenMode.SpecialWeapon: return "特殊兵装を選択します";
            default: return "";
        }
    }

    string GetEquippedLabel(WeaponDropData weapon)
    {
        if (weapon == null || !weapon.equipped) return "";

        if (screenMode != ScreenMode.SpecialWeapon)
            return " [装備中]";

        switch ((WeaponDropType)weapon.weaponTypeId)
        {
            case WeaponDropType.UGB:
                return " [装備中1]";
            case WeaponDropType.nAAM:
                return " [装備中2]";
            default:
                return " [装備中]";
        }
    }

    string GetModeTitle()
    {
        switch (screenMode)
        {
            case ScreenMode.StandardMissile: return "MSL";
            case ScreenMode.Gun: return "GUN";
            case ScreenMode.SpecialWeapon: return "SPW";
            default: return "";
        }
    }

    string GetPageName()
    {
        switch (detailPage)
        {
            case 0: return "[メインパラメータ]";
            case 1: return "[サブパラメータ 差分のみ]";
            case 2: return "[サブパラメータ すべて]";
            default: return "";
        }
    }

    string BuildPagedDetail(WeaponDropData weapon)
    {
        return WeaponStorage.BuildDetailText(weapon, detailPage);
    }

    void EnsureSplitTextObjects()
    {
        if (hudText == null) return;

        if (itemNameText == null)
            itemNameText = CreateSplitText("ItemNameText", 0f, -220f);

        if (itemPointHeaderText == null)
            itemPointHeaderText = CreateSplitText("ItemPointHeaderText", 565f, 80f, true);

        if (itemValueHeaderText == null)
            itemValueHeaderText = CreateSplitText("ItemValueHeaderText", 660f, 100f, true);

        if (itemPrevNextHeaderText == null)
            itemPrevNextHeaderText = CreateSplitText("ItemPrevNextHeaderText", 780f, 180f, true);

        if (itemPointText == null)
            itemPointText = CreateSplitText("ItemPointText", 565f, 80f, true);

        if (itemValueText == null)
            itemValueText = CreateSplitText("ItemValueText", 660f, 100f, true);

        if (itemPrevNextText == null)
            itemPrevNextText = CreateSplitText("ItemPrevNextText", 780f, 180f, true);

        SetSplitTextVisible(false);
    }

    TextMeshProUGUI CreateSplitText(string objectName, float x, float widthDelta, bool useAbsoluteX = false)
    {
        var text = Instantiate(hudText, hudText.transform.parent);
        text.name = objectName;
        text.text = "";
        text.fontSize = 18f;
        text.lineSpacing = 0f;
        text.raycastTarget = false;

        var rect = text.rectTransform;
        rect.anchoredPosition = useAbsoluteX
            ? new Vector2(x, rect.anchoredPosition.y)
            : rect.anchoredPosition + new Vector2(x, 0f);
        rect.sizeDelta += new Vector2(widthDelta, 0f);

        return text;
    }

    void SetSplitText(
        string itemNames,
        string pointHeader,
        string valueHeader,
        string prevNextHeader,
        string points,
        string values,
        string prevNextValues)
    {
        if (itemNameText != null)
            itemNameText.text = itemNames;
        if (itemPointHeaderText != null)
            itemPointHeaderText.text = pointHeader;
        if (itemValueHeaderText != null)
            itemValueHeaderText.text = valueHeader;
        if (itemPrevNextHeaderText != null)
            itemPrevNextHeaderText.text = prevNextHeader;
        if (itemPointText != null)
            itemPointText.text = points;
        if (itemValueText != null)
            itemValueText.text = values;
        if (itemPrevNextText != null)
            itemPrevNextText.text = prevNextValues;
    }

    void SetSplitTextVisible(bool visible)
    {
        if (itemNameText != null)
            itemNameText.gameObject.SetActive(visible);
        if (itemPointHeaderText != null)
            itemPointHeaderText.gameObject.SetActive(visible);
        if (itemValueHeaderText != null)
            itemValueHeaderText.gameObject.SetActive(visible);
        if (itemPrevNextHeaderText != null)
            itemPrevNextHeaderText.gameObject.SetActive(visible);
        if (itemPointText != null)
            itemPointText.gameObject.SetActive(visible);
        if (itemValueText != null)
            itemValueText.gameObject.SetActive(visible);
        if (itemPrevNextText != null)
            itemPrevNextText.gameObject.SetActive(visible);
    }

    List<WeaponDropData> CurrentList()
    {
        switch (screenMode)
        {
            case ScreenMode.StandardMissile: return stdmWeapons;
            case ScreenMode.Gun: return gunWeapons;
            case ScreenMode.SpecialWeapon: return spwWeapons;
            default: return stdmWeapons;
        }
    }

    WeaponDropData CurrentWeapon()
    {
        var list = CurrentList();
        if (list.Count == 0) return null;
        return list[CurrentIndex()];
    }

    int CurrentIndex()
    {
        switch (screenMode)
        {
            case ScreenMode.StandardMissile: return ClampIndex(stdmIndex, stdmWeapons.Count);
            case ScreenMode.Gun: return ClampIndex(gunIndex, gunWeapons.Count);
            case ScreenMode.SpecialWeapon: return ClampIndex(spwIndex, spwWeapons.Count);
            default: return 0;
        }
    }

    void SetCurrentIndex(int value)
    {
        switch (screenMode)
        {
            case ScreenMode.StandardMissile:
                stdmIndex = value;
                break;
            case ScreenMode.Gun:
                gunIndex = value;
                break;
            case ScreenMode.SpecialWeapon:
                spwIndex = value;
                break;
        }
    }

    int ClampIndex(int index, int count)
    {
        if (count <= 0) return 0;
        return Mathf.Clamp(index, 0, count - 1);
    }

    int Wrap(int value, int count)
    {
        if (count <= 0) return 0;
        return (value % count + count) % count;
    }
}
