using UnityEngine;

public class TutorialAttitudeAttractor : MonoBehaviour
{
    public Transform player;
    public Transform aimPoint;
    public float range = 800f;
    public float maxPitchYawDegreesPerSecond = 25f;
    public float strength = 0.9f;
    public float torqueStrength = 40f;
    public float maxTorque = 30f;
    public bool affectPitch = true;
    public bool affectYaw = true;
    public bool createAimTargetOnStart = true;
    public Vector3 aimTargetLocalOffset = new(0f, 20f, 120f);
    public Vector3 aimTargetScale = new(1f, 1f, 1f);
    public float aimTargetHp = 100f;
    public float targetFollowSharpness = 12f;
    public float minLeadDistance = 3f;
    public float maxLeadDistance = 450f;
    public bool attractPlayerToSelf = true;
    public bool fallbackToTransformRotation;

    Rigidbody playerRb;
    AircraftController playerAircraft;
    AugumentStatus playerStatus;
    Rigidbody ownerRb;

    void Start()
    {
        if (player == null)
        {
            var playerObject = GameObject.Find("Player");
            if (playerObject != null)
                player = playerObject.transform;
        }

        if (player != null)
        {
            playerRb = player.GetComponent<Rigidbody>();
            playerAircraft = player.GetComponent<AircraftController>();
            playerStatus = player.GetComponent<AugumentStatus>();
        }

        ownerRb = GetComponent<Rigidbody>();

        if (createAimTargetOnStart && aimPoint == null)
            aimPoint = CreateAimTarget().transform;
    }

    void FixedUpdate()
    {
        if (player == null) return;

        UpdateAimTargetPosition();

        Vector3 targetPosition = attractPlayerToSelf || aimPoint == null ? transform.position : aimPoint.position;
        Vector3 toTarget = targetPosition - player.position;
        float distance = toTarget.magnitude;
        if (distance <= 0.001f || distance > range) return;

        Vector3 targetDirection = toTarget.normalized;
        Vector3 localDirection = player.InverseTransformDirection(targetDirection);

        float yawError = affectYaw
            ? Mathf.Atan2(localDirection.x, Mathf.Max(0.001f, localDirection.z))
            : 0f;
        float pitchError = affectPitch
            ? -Mathf.Atan2(localDirection.y, Mathf.Max(0.001f, localDirection.z))
            : 0f;

        if (playerAircraft != null)
        {
            ApplyControlAssist(pitchError, yawError);
            return;
        }

        if (playerRb != null)
        {
            ApplyPhysicsAssist(pitchError, yawError);
            return;
        }

        if (fallbackToTransformRotation)
            ApplyTransformAssist(pitchError, yawError);
    }

    void ApplyControlAssist(float pitchError, float yawError)
    {
        Vector3 assistInput = new(
            CalculateControlAssist(-pitchError),
            0f,
            CalculateControlAssist(yawError)
        );

        if (assistInput.sqrMagnitude <= 0.0001f) return;

        playerAircraft.AddControlAssist(assistInput);
    }

    float CalculateControlAssist(float angleError)
    {
        const float fixedAssist = 0.45f;
        float threshold = 2f * Mathf.Deg2Rad;

        if (Mathf.Abs(angleError) <= threshold)
            return angleError;

        return Mathf.Sign(angleError) * fixedAssist;
    }

    void ApplyPhysicsAssist(float pitchError, float yawError)
    {
        float strengthScale = Mathf.Clamp01(strength);
        Vector3 torque =
            player.up * Mathf.Clamp(yawError * torqueStrength * strengthScale, -maxTorque, maxTorque) +
            player.right * Mathf.Clamp(pitchError * torqueStrength * strengthScale, -maxTorque, maxTorque);

        if (torque.sqrMagnitude <= 0.0001f) return;

        playerRb.AddTorque(torque, ForceMode.Acceleration);
    }

    void ApplyTransformAssist(float pitchError, float yawError)
    {
        float step = maxPitchYawDegreesPerSecond * Mathf.Clamp01(strength) * Time.fixedDeltaTime;
        if (step <= 0f) return;

        float yaw = Mathf.Clamp(yawError * Mathf.Rad2Deg, -step, step);
        float pitch = Mathf.Clamp(pitchError * Mathf.Rad2Deg, -step, step);

        Quaternion delta = Quaternion.AngleAxis(yaw, player.up) *
                           Quaternion.AngleAxis(pitch, player.right);
        player.rotation = delta * player.rotation;
    }

    void UpdateAimTargetPosition()
    {
        if (aimPoint == null || player == null) return;

        Vector3 desiredPosition = CalculateLeadTargetPosition();
        float lerp = 1f - Mathf.Exp(-Mathf.Max(0.01f, targetFollowSharpness) * Time.fixedDeltaTime);
        aimPoint.position = Vector3.Lerp(aimPoint.position, desiredPosition, lerp);
        aimPoint.rotation = transform.rotation;
    }

    Vector3 CalculateLeadTargetPosition()
    {
        float bulletSpeed = ResolvePlayerGunBulletSpeed();
        float distance = Vector3.Distance(player.position, transform.position);
        float leadTime = distance / Mathf.Max(1f, bulletSpeed);
        Vector3 ownerVelocity = ownerRb != null ? ownerRb.linearVelocity : Vector3.zero;
        Vector3 leadOffset = Vector3.ClampMagnitude(ownerVelocity * leadTime, maxLeadDistance);

        if (leadOffset.magnitude < minLeadDistance)
        {
            Vector3 fallbackDirection = ownerVelocity.sqrMagnitude > 0.01f
                ? ownerVelocity.normalized
                : transform.forward;
            leadOffset = fallbackDirection * minLeadDistance;
        }

        return transform.position - leadOffset;
    }

    float ResolvePlayerGunBulletSpeed()
    {
        if (playerStatus != null)
        {
            playerStatus.altGetVar("\u9283\u5f3e\uff1a\u521d\u901f", out float statusBulletSpeed);
            if (statusBulletSpeed > 0f)
                return statusBulletSpeed;
        }

        WeaponSystem weaponSystem = player.GetComponent<WeaponSystem>();
        if (weaponSystem != null && weaponSystem.bulletSpeed > 0f)
            return weaponSystem.bulletSpeed;

        FCS_p fcs = player.GetComponent<FCS_p>();
        if (fcs != null && fcs.bulletSpeed > 0f)
            return fcs.bulletSpeed;

        return 200f;
    }

    GameObject CreateAimTarget()
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "TutorialLeadAimTarget";
        cube.transform.SetParent(null, true);
        cube.transform.position = CalculateInitialAimTargetPosition();
        cube.transform.rotation = transform.rotation;
        cube.transform.localScale = aimTargetScale;

        Rigidbody targetRb = cube.AddComponent<Rigidbody>();
        targetRb.useGravity = false;
        targetRb.isKinematic = true;
        targetRb.detectCollisions = true;

        ConfigureAimTargetStatus(cube);

        return cube;
    }

    Vector3 CalculateInitialAimTargetPosition()
    {
        if (player == null)
            return transform.TransformPoint(aimTargetLocalOffset);

        return CalculateLeadTargetPosition();
    }

    void ConfigureAimTargetStatus(GameObject cube)
    {
        AugumentStatus status = cube.GetComponent<AugumentStatus>();
        if (status == null)
            status = cube.AddComponent<AugumentStatus>();

        status.isEnemy = true;
        status.isPlayer = false;
        status.issortie = true;
        status.isVisible = true;
        status.missionObjective = true;
        status.waveID = -1;
        status.hp = Mathf.Max(1f, aimTargetHp);
        status.maxhp = status.hp;
        status.lifeTime = -1f;
        status.SetScoreReward(0f);

        ObjectManager.Instance?.RegisterEnemy(cube, status.waveID);
    }
}
