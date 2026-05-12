using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SetupUI : MonoBehaviour
{
    public TextMeshProUGUI hudText;
    public TextMeshProUGUI pointText;
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

    void Start()
    {
        selectedstage = PlayerPrefs.GetInt("selectedstage", 0);
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
            SceneManager.LoadScene(scene_name[selectedstage]);
        }
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
        hudText.text = "setting\n\n";
        hudText.text += MainLine(0, "MSL") + "\n";
        hudText.text += MainLine(1, "GUN") + "\n";
        hudText.text += MainLine(2, "SPW") + "\n\n";
        hudText.text += MainLine(3, "sortie") + "\n";
        hudText.text += MainLine(4, "cancel") + "\n";

        if (pointText != null)
            pointText.text = GetMainDescription();
    }

    void UpdateWeaponText()
    {
        var list = CurrentList();
        var weapon = CurrentWeapon();

        hudText.text = GetModeTitle();

        if (weapon == null)
        {
            hudText.text += "\nNo weapon.\n";
            if (pointText != null)
                pointText.text = "候補がありません";
            return;
        }

        hudText.text += BuildPagedDetail(weapon);
        hudText.text += $"\nページ {detailPage + 1}/3 {GetPageName()} </>\n";
        hudText.text += $"ID {CurrentIndex() + 1}/{list.Count} ↑/↓";

        if (pointText != null)
            pointText.text = GetWeaponDescription();
    }

    string MainLine(int index, string label)
    {
        string head = mainIndex == index ? "> " : "  ";
        return head + label;
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
