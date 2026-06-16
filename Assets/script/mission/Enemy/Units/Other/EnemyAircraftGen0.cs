using UnityEngine;

public class EnemyAircraftGen0 : AircraftController
{
    public Transform target; // �v���C���[�@

    bool Overshoot;
    float Overshoottimer;
    float savingdistanceToTarget;
    float distdetecttimer = 0;
    float interval = 5f;
    int pattern;


    float accelthrottle = 5f;
    float decelthrottle = 0.01f;

    Vector3 savingcordinate;

    float randomRoll=0;

    private void Update()
    {
        if (GetComponent<FCS_e>().waytarget != null)
        {
            target = GetComponent<FCS_e>().waytarget.transform;
        }

        //�͈͐���
        if (transform.position.y < 600f)
        {
            Overshoot = true;
            Overshoottimer = Random.Range(3f, 5f);
            pattern = 3;
        }
        if (transform.position.y > 10000f)
        {
            Overshoot = true;
            Overshoottimer = Random.Range(3f, 5f);
            pattern = 4;
        }
    }

    protected override Vector3 GetControlInput()
    {
        if (target == null) return Vector3.zero;

        // �^�[�Q�b�g����
        Vector3 dirToTarget = (target.position - transform.position).normalized;
        Vector3 localDir = transform.InverseTransformDirection(dirToTarget);

        float pitch = 0f;
        float yaw = 0f;
        float roll = 0f;

        if (Overshoot)
        {
            switch (pattern)
            {

                case 3:
                // �㏸�F����ᐧ���K�p
                {
                    // �㏸������D��
                    Vector3 upDir = Vector3.up;
                    Vector3 localUpDir = transform.InverseTransformDirection(upDir);

                    float rollError = Mathf.Clamp(localUpDir.x, -1f, 1f);
                    float rollStrength = Mathf.Abs(rollError);
                    float pitchStrength = 1f - rollStrength;

                    roll = rollError;
                    pitch = Mathf.Clamp(localUpDir.y, -1f, 1f) * pitchStrength;
                    yaw = rollError * 0.3f;
                }
                break;
                case 4:
                // �㏸�F����ᐧ���K�p
                {
                    // �㏸������D��
                    Vector3 downDir = Vector3.down;
                    Vector3 localdownDir = transform.InverseTransformDirection(downDir);

                    float rollError = Mathf.Clamp(localdownDir.x, -1f, 1f);
                    float rollStrength = Mathf.Abs(rollError);
                    float pitchStrength = 1f - rollStrength;

                    roll = rollError;
                    pitch = Mathf.Clamp(localdownDir.y, -1f, 1f) * pitchStrength;
                    yaw = rollError * 0.3f;
                }
                break;

                case 1:
                case 2:
                case 5:
                default:
                    // ���i �� ����Ȃ�
                    break;
            }
        }
        else
        {
        }

        return new Vector3(pitch, roll, yaw);
    }

    protected override float GetThrottleInput()
    {
        if (target == null) return 1f;

        float distance = Vector3.Distance(transform.position, target.position);


        //�O���̗h�炬
        if(distdetecttimer <= 0)
        {
            distdetecttimer = interval;
            if (Mathf.Abs(distance - savingdistanceToTarget) < 80f &&
                distance < 600f)
            {
                Overshoot = true;
                if (Overshoot)
                {
                    Overshoottimer = Random.Range(3f, 5f);
                    pattern = Random.Range(1, 6);
                    randomRoll = Random.Range(-1f, 1f);
                }
            }
            //�X�^�b�N����
            if (Vector3.Distance(savingcordinate, transform.position) < 1f)
            {
                transform.position += new Vector3(0, 50f, 0);
                rb.linearVelocity = Vector3.up * 50f;
            }

            savingcordinate = transform.position;
            savingdistanceToTarget = distance;
        }
        else
        {
            distdetecttimer -= Time.deltaTime;
        }
        if (Overshoot)
        {
            Overshoottimer -= Time.deltaTime;
            distdetecttimer = 13f;
            if (Overshoottimer <= 0) Overshoot = false;
            switch (pattern)
            {
                case 1:
                    return 0f; // ����
                case 2:
                case 3:
                    return accelthrottle; // ����
                default:
                    return decelthrottle;
            }
        }

        if (distance > 800f ||
            rb.linearVelocity.magnitude < GetComponent<AircraftController>().stallSpeed) return accelthrottle;  // �ǔ����͉���
        if (distance < 300f) return randomRoll+1.5f; // �ڋ߂��������猸��
        return 1f; // ���q
    }
}
