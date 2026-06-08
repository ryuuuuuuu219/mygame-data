using TMPro;
using UnityEngine;

public class InputTextUI : MonoBehaviour
{
    public TextMeshProUGUI text;
    public Camera uiCamera;

    public RectTransform Lstick, Rstick;   // ìÆÇ≠ä€
    public float range = 12f;     // â¬ìÆîÕàÕ

    [SerializeField] InputManager i;

    Vector2 LbasePos, RbasePos; // É[Éçì_ï€ë∂óp


    void Start()
    {
        if (i == null)
            i = InputManager.Instance;

        LbasePos = Lstick.anchoredPosition;
        RbasePos = Rstick.anchoredPosition;
    }

    void Update()
    {
        if (i == null) return;

        text.text =
            "  " + (i.altl2 ? "LT" : "\u30fb") + "\t\t\t  " + (i.altr2 ? "RT" : "\u30fb") + "\n" +
            "  " + (i.l1 ? "LB" : "\u30fb") + "\t\t\t  " + (i.r1 ? "RB" : "\u30fb") + "\n" +
            "  " + (i.up ? "\u2191" : "\u30fb") + "\t\t\t  " + (i.north ? "\u25b3" : "\u30fb") + "\n" +
            (i.left ? "\u2190" : "\u30fb") + "  " + (i.right ? "\u2192" : "\u30fb") + "\t\t" + (i.west ? "\u25a1" : "\u30fb") + "  " + (i.east ? "\u25cb" : "\u30fb") + "\n" +
            "  " + (i.down ? "\u2193" : "\u30fb") + "\t\t\t  " + (i.south ? "\u00d7" : "\u30fb") + "\n" +
            "  " + (i.stickL ? "LS" : "\u30fb") + "\t\t\t  " + (i.stickR ? "RS" : "\u30fb") + "\n";

        Lstick.anchoredPosition = LbasePos + new Vector2(i.horizontalL, -i.verticalL) * range;
        Rstick.anchoredPosition = RbasePos + new Vector2(i.horizontalR, i.verticalR) * range;
    }
}
