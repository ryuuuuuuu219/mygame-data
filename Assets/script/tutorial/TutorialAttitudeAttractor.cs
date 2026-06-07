using UnityEngine;

public class TutorialAttitudeAttractor : MonoBehaviour
{
    public Transform player;
    public Transform aimPoint;
    public float range = 800f;
    public float maxPitchYawDegreesPerSecond = 25f;
    public float strength = 0.35f;
    public bool affectPitch = true;
    public bool affectYaw = true;

    void Start()
    {
        if (player == null)
        {
            var playerObject = GameObject.Find("Player");
            if (playerObject != null)
                player = playerObject.transform;
        }
    }

    void LateUpdate()
    {
        if (player == null) return;

        Vector3 targetPosition = aimPoint != null ? aimPoint.position : transform.position;
        Vector3 toTarget = targetPosition - player.position;
        float distance = toTarget.magnitude;
        if (distance <= 0.001f || distance > range) return;

        Vector3 targetDirection = toTarget.normalized;
        Vector3 localDirection = player.InverseTransformDirection(targetDirection);

        float yaw = affectYaw
            ? Mathf.Atan2(localDirection.x, Mathf.Max(0.001f, localDirection.z)) * Mathf.Rad2Deg
            : 0f;
        float pitch = affectPitch
            ? -Mathf.Atan2(localDirection.y, Mathf.Max(0.001f, localDirection.z)) * Mathf.Rad2Deg
            : 0f;

        float rangeRate = Mathf.Clamp01(1f - distance / Mathf.Max(1f, range));
        float step = maxPitchYawDegreesPerSecond * Mathf.Clamp01(strength) * rangeRate * Time.deltaTime;
        if (step <= 0f) return;

        yaw = Mathf.Clamp(yaw, -step, step);
        pitch = Mathf.Clamp(pitch, -step, step);

        Quaternion delta = Quaternion.AngleAxis(yaw, player.up) *
                           Quaternion.AngleAxis(pitch, player.right);
        player.rotation = delta * player.rotation;
    }
}
