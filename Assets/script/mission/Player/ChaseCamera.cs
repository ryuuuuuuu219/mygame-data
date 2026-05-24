using UnityEngine;

public class ChaseCameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    public Camera chaseCamera;
    public Vector3 offset = new Vector3(0, 1.5f, -3f);
    public float followSpeed = 5f;
    public float rotationSpeed = 5f;

    [Header("Right Stick Settings")]
    public float sensitivity = 45f;   // 右スティックの視点移動角度（度/最大入力）
    public float returnSpeed = 1f;    // 入力がないときに機体方向に戻る速度

    private float yawOffset;   // 相対Yaw
    private float pitchOffset; // 相対Pitch

    private void Update()
    {
        if (chaseCamera == null) return;


        // --- 右スティック入力を取得 ---
        var input = InputManager.Instance;
        float inputX = input.horizontalR; // 右スティックX
        float inputY = input.verticalR;   // 右スティックY


        // 入力に応じて相対オフセットを更新
        yawOffset = Mathf.Lerp(yawOffset, inputX * sensitivity, rotationSpeed * Time.deltaTime);
        pitchOffset = Mathf.Lerp(pitchOffset, -inputY * sensitivity, rotationSpeed * Time.deltaTime);


        // --- 基準回転（機体の姿勢基準） ---
        Quaternion baseRotation = Quaternion.LookRotation(transform.forward, transform.up);

        // --- オフセット回転（右スティック入力による視点ずらし） ---
        Quaternion offsetRotation = Quaternion.Euler(pitchOffset, yawOffset, 0f);

        // --- 合成してカメラに適用 ---
        Quaternion desiredRotation = baseRotation * offsetRotation;
        chaseCamera.transform.rotation = desiredRotation;

        // --- 位置を補間 ---
        Vector3 desiredPosition = transform.TransformPoint(offset);
        chaseCamera.transform.position = Vector3.Lerp(
            chaseCamera.transform.position,
            desiredPosition,
            followSpeed * Time.deltaTime
        );
    }
}
