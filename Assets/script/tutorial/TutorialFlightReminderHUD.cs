using TMPro;
using UnityEngine;

public class TutorialFlightReminderHUD : MonoBehaviour
{
    enum ReminderKind
    {
        None,
        HighGTurn,
        BarrelRoll,
        Turn,
        Yaw,
        PitchUp
    }

    public Transform player;
    public Rigidbody playerRigidbody;
    public TextMeshProUGUI reminderText;

    public Vector3 center = Vector3.zero;
    public float activeRadius = 700f;
    public float unusedSeconds = 12f;
    public float displayCooldown = 2f;

    float highGTurnTimer;
    float barrelRollTimer;
    float turnTimer;
    float yawTimer;
    float pitchUpTimer;
    float displayTimer;

    Vector3 lastForward;
    Vector3 lastUp;

    void Start()
    {
        if (player == null)
        {
            var playerObject = GameObject.Find("Player");
            if (playerObject != null)
                player = playerObject.transform;
        }

        if (playerRigidbody == null && player != null)
            playerRigidbody = player.GetComponent<Rigidbody>();

        if (player != null)
        {
            lastForward = player.forward;
            lastUp = player.up;
        }
    }

    void Update()
    {
        if (player == null || reminderText == null)
            return;

        if (IsInsideBasicFlightArea())
        {
            ClearText();
            ResetTimers();
            CapturePose();
            return;
        }

        UpdateTimers();

        displayTimer -= Time.deltaTime;
        ReminderKind reminder = PickReminder();
        if (reminder == ReminderKind.None)
        {
            ClearText();
            return;
        }

        if (displayTimer <= 0f || string.IsNullOrEmpty(reminderText.text))
        {
            reminderText.text = BuildText(reminder);
            displayTimer = Mathf.Max(0.1f, displayCooldown);
        }

        CapturePose();
    }

    bool IsInsideBasicFlightArea()
    {
        Vector2 current = new(player.position.x, player.position.z);
        Vector2 origin = new(center.x, center.z);
        return Vector2.Distance(current, origin) <= activeRadius;
    }

    void UpdateTimers()
    {
        float dt = Time.deltaTime;
        highGTurnTimer += dt;
        barrelRollTimer += dt;
        turnTimer += dt;
        yawTimer += dt;
        pitchUpTimer += dt;

        var input = InputManager.Instance;
        if (input != null)
        {
            if (input.stickL && input.accel < -0.1f && Mathf.Abs(input.horizontalL) > 0.25f)
                highGTurnTimer = 0f;
            if (Mathf.Abs(input.horizontalL) > 0.5f && Mathf.Abs(input.verticalL) > 0.2f)
                barrelRollTimer = 0f;
            if (Mathf.Abs(input.horizontalL) > 0.25f || Mathf.Abs(input.verticalL) > 0.25f)
                turnTimer = 0f;
            if (input.altr2 || input.altl2)
                yawTimer = 0f;
            if (input.verticalL < -0.25f)
                pitchUpTimer = 0f;
        }

        float yawDelta = Vector3.SignedAngle(ProjectXZ(lastForward), ProjectXZ(player.forward), Vector3.up);
        if (Mathf.Abs(yawDelta) > 8f)
            turnTimer = 0f;

        float rollDelta = Vector3.Angle(lastUp, player.up);
        if (rollDelta > 25f)
            barrelRollTimer = 0f;

        if (player.forward.y > 0.25f)
            pitchUpTimer = 0f;
    }

    ReminderKind PickReminder()
    {
        if (highGTurnTimer >= unusedSeconds) return ReminderKind.HighGTurn;
        if (barrelRollTimer >= unusedSeconds) return ReminderKind.BarrelRoll;
        if (turnTimer >= unusedSeconds) return ReminderKind.Turn;
        if (yawTimer >= unusedSeconds) return ReminderKind.Yaw;
        if (pitchUpTimer >= unusedSeconds) return ReminderKind.PitchUp;
        return ReminderKind.None;
    }

    string BuildText(ReminderKind kind)
    {
        return kind switch
        {
            ReminderKind.HighGTurn => "急旋回: 左スティック押し込み + 減速しながら旋回",
            ReminderKind.BarrelRoll => "バレルロール: ロールしながら機首を少し上げる",
            ReminderKind.Turn => "旋回: 機体を傾けてから、機首を上げる",
            ReminderKind.Yaw => "ヨー: R2 / L2 で機首を左右へ調整",
            ReminderKind.PitchUp => "ピッチアップ: 左スティック下で機首を上げる",
            _ => ""
        };
    }

    void ResetTimers()
    {
        highGTurnTimer = 0f;
        barrelRollTimer = 0f;
        turnTimer = 0f;
        yawTimer = 0f;
        pitchUpTimer = 0f;
    }

    void CapturePose()
    {
        lastForward = player.forward;
        lastUp = player.up;
    }

    void ClearText()
    {
        if (!string.IsNullOrEmpty(reminderText.text))
            reminderText.text = "";
    }

    Vector3 ProjectXZ(Vector3 value)
    {
        value.y = 0f;
        return value.sqrMagnitude > 0.0001f ? value.normalized : Vector3.forward;
    }
}
