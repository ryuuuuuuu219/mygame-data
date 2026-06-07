using TMPro;
using UnityEngine;

public class TutorialBasicFlightArea : MonoBehaviour
{
    public Transform player;
    public TextMeshProUGUI areaText;
    public Vector3 center = Vector3.zero;
    public float radius = 700f;

    public bool IsInside { get; private set; }

    void Start()
    {
        if (player == null)
        {
            var playerObject = GameObject.Find("Player");
            if (playerObject != null)
                player = playerObject.transform;
        }
    }

    void Update()
    {
        if (player == null) return;

        Vector2 current = new(player.position.x, player.position.z);
        Vector2 origin = new(center.x, center.z);
        IsInside = Vector2.Distance(current, origin) <= radius;

        if (areaText == null) return;
        areaText.text = IsInside
            ? "基本飛行エリア: 操作を確認してください。"
            : "";
    }
}
