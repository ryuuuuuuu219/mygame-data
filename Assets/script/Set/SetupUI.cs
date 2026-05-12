using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SetupUI : MonoBehaviour
{
    public TextMeshProUGUI hudText;
    public TextMeshProUGUI pointText;
    public List<string> scene_name = new List<string>();

    int selectedstage;
    int selectedIndex;
    int pageIndex;
    List<WeaponDropData> weapons = new List<WeaponDropData>();

    enum StickLock
    {
        None,
        Horizontal,
        Vertical
    }

    StickLock stickLock = StickLock.None;
    float interval = 1f;
    float holdtime = 0.5f;

    void Start()
    {
        selectedstage = PlayerPrefs.GetInt("selectedstage", 0);
        Reload();
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

        switch (stickLock)
        {
            case StickLock.None:
                if (Mathf.Abs(h) > 0.1f)
                {
                    ChangePage(h);
                    stickLock = StickLock.Horizontal;
                }
                else if (Mathf.Abs(v) > 0.1f)
                {
                    ChangeSelection(v);
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
                    Repeat(() => ChangePage(h));
                }
                break;
            case StickLock.Vertical:
                if (Mathf.Abs(v) < 0.1f)
                {
                    UnlockStick();
                }
                else
                {
                    Repeat(() => ChangeSelection(v));
                }
                break;
        }

        if (keyInput.submit)
        {
            EquipAndLaunch();
        }
        else if (keyInput.cancel)
        {
            SceneManager.LoadScene("Briefing");
        }

        UpdateText();
    }

    void Reload()
    {
        weapons = WeaponStorage.LoadAll();
        selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, weapons.Count - 1));
        UpdateText();
    }

    void ChangeSelection(float value)
    {
        if (weapons.Count == 0) return;
        int delta = value > 0 ? 1 : -1;
        selectedIndex = (selectedIndex + delta + weapons.Count) % weapons.Count;
    }

    void ChangePage(float value)
    {
        int pageCount = 2;
        int delta = value > 0 ? 1 : -1;
        pageIndex = (pageIndex + delta + pageCount) % pageCount;
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

    void EquipAndLaunch()
    {
        if (weapons.Count == 0) return;
        WeaponStorage.Equip(weapons[selectedIndex]);
        WeaponStorage.ApplyEquippedToPlayerPrefs();

        if (selectedstage >= 0 && selectedstage < scene_name.Count)
        {
            SceneManager.LoadScene(scene_name[selectedstage]);
        }
    }

    void UpdateText()
    {
        if (hudText == null) return;

        hudText.text = "Weapon Storage\n\n";
        hudText.text += pageIndex == 0 ? "[List]  Detail\n\n" : " List  [Detail]\n\n";

        if (weapons.Count == 0)
        {
            hudText.text += "No weapons.\n";
            return;
        }

        if (pageIndex == 0)
        {
            for (int i = 0; i < weapons.Count; i++)
            {
                var data = weapons[i];
                string head = selectedIndex == i ? "> " : "  ";
                string equipped = data.equipped ? " *" : "";
                hudText.text += $"{head}{data.displayName}{equipped}\n";
            }

            hudText.text += "\nO: Equip / X: Back / <>: Detail";
        }
        else
        {
            hudText.text += WeaponStorage.BuildDetailText(weapons[selectedIndex]);
            hudText.text += "\nO: Equip / X: Back / <>: List";
        }

        if (pointText != null)
        {
            var data = weapons[selectedIndex];
            pointText.text = $"Type: {WeaponStorage.GetShortTypeName(data)}\nLevel: {data.level:F0}";
        }
    }
}
