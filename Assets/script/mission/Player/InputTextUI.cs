using TMPro;
using UnityEngine;

public class InputTextUI : MonoBehaviour
{
    public TextMeshProUGUI text;
    public Camera uiCamera;

    public RectTransform Lstick, Rstick;   // 動く丸
    public float range = 12f;     // 可動範囲

    [SerializeField] InputManager i;

    Vector2 LbasePos, RbasePos; // ゼロ点保存用


    void Start()
    {
        LbasePos = Lstick.anchoredPosition;
        RbasePos = Rstick.anchoredPosition;
    }

    void Update()
    {
        if (i == null) return;

        text.text =
            "　" + (i.altl2 ? "LT" : "・") + "\t\t\t　" + (i.altr2 ? "RT" : "・") + "\n" +
            "　" + (i.l1 ? "LB" : "・") + "\t\t\t　" + (i.r1 ? "RB" : "・") + "\n" +
            "　" + (i.up ? "↑" : "・") + "\t\t\t　" + (i.north ? "▲" : "・") + "\n" +
            (i.left ? "←" : "・") + "　" + (i.right ? "→" : "・") + "\t\t" + (i.west ? "■" : "・") + "　" +(i.east ? "●" : "・") + "\n" +
            "　" + (i.down ? "↓" : "・") + "\t\t\t　" + (i.south ? "×" : "・") + "\n" +
            "　" + (i.stickL ? "LS" : "・") + "\t\t\t　" + (i.stickR ? "RS" : "・") + "\n";

        Lstick.anchoredPosition = LbasePos + new Vector2(i.horizontalL, -i.verticalL) * range;
        Rstick.anchoredPosition = RbasePos + new Vector2(i.horizontalR, i.verticalR) * range;
    }
}
