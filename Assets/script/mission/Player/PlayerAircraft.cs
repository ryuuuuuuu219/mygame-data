using UnityEngine;

public class PlayerAircraft : AircraftController
{
    // スロットルの設定値を追加
    [Header("Throttle Presets")]
    public float accelthrottle = 5f;   // 加速スロットル倍率
    public float decelthrottle = 0f; // 減速スロットル倍率
    public float normalthrottle = 1f; // 通常スロットル倍率

    protected override void Start()
    {
        base.Start();

        var playerEntity = GameObject.Find("Player");
        if (playerEntity == null)
            playerEntity = gameObject;

        var dropChecker = playerEntity.GetComponent<PlayerDropChecker>();
        if (dropChecker == null)
            dropChecker = playerEntity.AddComponent<PlayerDropChecker>();

        dropChecker.s = status;
    }
    protected override bool GetLimiter()
    {
        var Input = InputManager.Instance;

        return !(Input.stickL && Input.accel<0); 
    }


    protected override Vector3 GetControlInput()
    {
        var Input = InputManager.Instance;

        float pitch = Input.verticalL;
        float roll = Input.horizontalL;
        float yaw = Input.r2 - Input.l2;
        if (yaw == 0)
        {
            yaw = (Input.altr2 ? 1f : 0f) + (Input.altl2 ? -1f : 0f);
        }

        yaw *= 0.2f;
        return new Vector3(pitch, roll, yaw);
    }

    protected override float GetThrottleInput()
    {
        var Input = InputManager.Instance;

        float accelAxis = Input.accel;
        if (accelAxis > 0.1f) return this.accelthrottle;
        else if (accelAxis < -0.1f) return decelthrottle;
        else return normalthrottle;
    }
}
