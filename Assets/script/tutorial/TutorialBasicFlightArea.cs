using TMPro;
using UnityEngine;

public class TutorialBasicFlightArea : MonoBehaviour
{
    public Transform player;
    public TextMeshProUGUI areaText;
    public Vector3 center = Vector3.zero;
    public float radius = 700f;
    public bool showAreaMarker = true;
    public Vector2 markerXZ = Vector2.zero;
    public float markerY = 10000f;
    public float markerHeight = 20000f;
    public Color markerColor = new(1f, 0.08f, 0.04f, 0.18f);
    public string markerText;
    public float markerTextSize = 10f;
    public Color markerTextColor = Color.cyan;
    public TMP_FontAsset markerTextFont;
    public string areaMessage = "基本飛行エリア: ロール、ピッチ、ヨー、加減速を確認してください。";

    public bool IsInside { get; private set; }
    GameObject marker;
    GameObject markerTextObject;

    void Start()
    {
        if (player == null)
        {
            var playerObject = GameObject.Find("Player");
            if (playerObject != null)
                player = playerObject.transform;
        }

        if (showAreaMarker)
            CreateAreaMarker();
    }

    void Update()
    {
        if (player == null) return;

        Vector2 current = new(player.position.x, player.position.z);
        Vector2 origin = new(center.x, center.z);
        IsInside = Vector2.Distance(current, origin) <= radius;

        if (areaText != null)
            areaText.text = IsInside ? areaMessage : "";

        if (markerTextObject != null && player != null)
        {
            Vector3 textPosition = markerTextObject.transform.position;
            textPosition.y = player.position.y;
            markerTextObject.transform.position = textPosition;
        }
    }

    void CreateAreaMarker()
    {
        if (marker != null) return;

        if (markerXZ == Vector2.zero)
            markerXZ = new Vector2(center.x, center.z);

        marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = "TutorialBasicFlightAreaMarker";
        marker.transform.SetParent(transform, false);
        marker.transform.position = new Vector3(markerXZ.x, markerY, markerXZ.y);
        marker.transform.localScale = new Vector3(radius * 2f, markerHeight * 0.5f, radius * 2f);

        Collider collider = marker.GetComponent<Collider>();
        if (collider != null)
            collider.enabled = false;

        MeshRenderer renderer = marker.GetComponent<MeshRenderer>();
        if (renderer == null) return;

        Shader shader = Shader.Find("Transparent/Diffuse");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = new(shader);
        material.color = markerColor;
        renderer.material = material;

        CreateCenterText();
    }

    void CreateCenterText()
    {
        if (string.IsNullOrEmpty(markerText) || markerTextObject != null)
            return;

        markerTextObject = new GameObject("TutorialAreaMarkerText");
        markerTextObject.transform.SetParent(transform, false);
        markerTextObject.transform.position = new Vector3(
            markerXZ.x,
            player != null ? player.position.y : center.y,
            markerXZ.y);
        markerTextObject.transform.rotation = Quaternion.LookRotation(Vector3.back, Vector3.up);

        var yawToPlayer = markerTextObject.AddComponent<TutorialTextYawToPlayer>();
        yawToPlayer.player = player;

        TextMeshPro text = markerTextObject.AddComponent<TextMeshPro>();
        if (markerTextFont != null)
            text.font = markerTextFont;
        text.text = markerText;
        text.fontSize = markerTextSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = markerTextColor;
    }
}
