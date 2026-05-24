using UnityEngine;

public class EnemyAceAirBS : AircraftController
{
    public Transform target; // プレイヤー機

    float accelthrottle = 5f;

    float randomRoll = 0;

    FCS_e weapon;

    public float basealtitude = 500f;
    private void Update()
    {
        if (weapon == null)
        {
            weapon = GetComponent<FCS_e>();
            if (weapon == null) return;
        }
        if (target != weapon.waytarget)
        {
            if (weapon.target != null)
            {
                target = weapon.target.transform;
            }
            else if (weapon.waytarget != null)
            {
                target = weapon.waytarget.transform;
            }
        }

        // 高度維持
        float altitudeError = basealtitude - transform.position.y;
        transform.position += Vector3.up * altitudeError * 0.01f * Time.deltaTime;

    }

    protected override Vector3 GetControlInput()
    {
        if (target == null) return Vector3.zero;

        Vector3 localDir = target.position - transform.position;
        Torque = localDir;

        float pitch = 0f;
        float roll = 0f;

        localDir = transform.InverseTransformDirection(localDir);



        return new Vector3(pitch, roll, Mathf.Clamp(localDir.x, -10f, 10f)*50f);
    }

    protected override float GetThrottleInput()
    {
        if (target == null) return 1f;

        float distance = Vector3.Distance(transform.position, target.position);


        if (distance > 800f ||
            rb.linearVelocity.magnitude < GetComponent<AircraftController>().stallSpeed) return accelthrottle;  // 追尾時は加速
        if (distance < 300f) return randomRoll + 1.5f; // 接近しすぎたら減速
        return 1f; // 巡航
    }


}