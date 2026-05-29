using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class Missile : MonoBehaviour
{
    [Header("��{�p�����[�^")]
    public Transform target;
    public float power = 50f;
    public float acceleration = 20f;
    public float maxspeed;
    public float lifeTime = 10f;
    public float turnRate = 90f; // �ő���񑬓x (deg/sec)
    public float breakAngle = 90f; // �U������p�x (deg)
    public float ProportionalConstant = 3f; // ���q�@�萔
    public float turnRateDecay = 1f; // �U���͌����� (deg/sec/sec)
    public float totalDeltaTheta = 90f; // �ݐϗU������p�x��� (deg)
    public Vector3 launchDirectionOverride;
    public float guidanceStartDelay;
    public bool guidanceStartSwitch;
    public bool usePurePursuitAssistForLaunchOverride = true;
    public float initialTurnRate = 95f;
    public float initialTurnBreakAngle = 15f;
    public float effectRadius = 0f;

    [Header("������")]
    private Vector3 previousPos;
    private Vector3 currentPos;
    private Vector3 newDir;
    public Vector3 velocity;
    public float speed;

    private List<GameObject> allies;
    private Vector3 lastDirToTarget;
    private float currentTurnRate;
    private float usedDeltaTheta;
    private bool useInitialPurePursuit;
    private const int GroundProbeIntervalFrames = 5;
    private int groundProbeFrame;
    private bool hasPlannedGroundHit;
    private Vector3 plannedGroundHitPoint;

    public bool isheatseeker = true;

    // ������
    public void missileInit(Vector3 startPos, Vector3 startVelocity, float lifetime = 10f)
    {
        ObjectManager.Instance?.RegisterMissile_e(gameObject);
        lifeTime = lifetime;
        target = null;

        velocity = startVelocity;
        if (launchDirectionOverride.sqrMagnitude > 0.001f)
            velocity = launchDirectionOverride.normalized * startVelocity.magnitude;

        transform.position = startPos;
        transform.rotation = Quaternion.LookRotation(velocity.normalized);

        previousPos = currentPos = transform.position;
        speed = startVelocity.magnitude;

        newDir = velocity.normalized;
        lastDirToTarget = Vector3.zero;
        currentTurnRate = turnRate;
        usedDeltaTheta = 0f;
        useInitialPurePursuit = ShouldUsePurePursuitAssist();
        groundProbeFrame = 0;
        hasPlannedGroundHit = false;
        gameObject.SetActive(true);
    }

    public void StatusSetting(float Power, float accel, float maxspe, float turn, float breakAng, float pConst)
    {
        StatusSetting(Power, accel, maxspe, turn, breakAng, pConst, turnRateDecay, totalDeltaTheta);
    }

    public void StatusSetting(float Power, float accel, float maxspe, float turn, float breakAng, float pConst, float turnDecay, float totalTheta)
    {
        power = Power;

        acceleration = accel;
        maxspeed = maxspe;

        turnRate = turn;
        breakAngle = breakAng;
        ProportionalConstant = pConst;
        turnRateDecay = turnDecay;
        totalDeltaTheta = totalTheta;
        currentTurnRate = turnRate;
        usedDeltaTheta = 0f;
        useInitialPurePursuit = ShouldUsePurePursuitAssist();
    }

    void FixedUpdate()
    {
        // --- ���� ---
        lifeTime -= Time.fixedDeltaTime;
        if (lifeTime <= 0f)
        {
            rangeover();
            return;
        }

        if (newDir == Vector3.zero)
            newDir = velocity.normalized;

        bool manualGuidanceStart = float.IsInfinity(guidanceStartDelay);
        bool skipGuidance = manualGuidanceStart && guidanceStartSwitch;
        if (!skipGuidance && !manualGuidanceStart && guidanceStartDelay > 0f)
        {
            guidanceStartDelay -= Time.fixedDeltaTime;
            skipGuidance = guidanceStartDelay > 0f;
        }

        // --- �U������ ---
        if (!skipGuidance && target != null)
        {
            Vector3 dirToTarget = (target.position - transform.position).normalized;
            Vector3 guidanceDir = newDir.sqrMagnitude > 0.001f ? newDir.normalized : velocity.normalized;
            float angleDiff = Vector3.Angle(guidanceDir, dirToTarget);

            // �K��p�x�ȏ�ŗU�����
            if (!useInitialPurePursuit && angleDiff > breakAngle)
            {
                target = null;
                ProbeGroundImpact(true);
                return;
            }

            if (useInitialPurePursuit)
            {
                ApplyPurePursuitAssist(dirToTarget, initialTurnRate);
                if (Vector3.Angle(newDir, dirToTarget) <= initialTurnBreakAngle)
                {
                    useInitialPurePursuit = false;
                    lastDirToTarget = dirToTarget;
                }
            }
            else
            {
                // ���q�@�ߎ�
                if (lastDirToTarget != Vector3.zero)
                {
                    Vector3 LOSrate = Vector3.Cross(lastDirToTarget, dirToTarget);
                    Vector3 rotAxis = LOSrate.sqrMagnitude > 0.000001f
                        ? LOSrate.normalized
                        : Vector3.Cross(velocity.normalized, dirToTarget).normalized;
                    float rotMag = LOSrate.magnitude * ProportionalConstant * Mathf.Rad2Deg / Time.fixedDeltaTime;

                    float remainingTheta = Mathf.Max(0f, totalDeltaTheta - usedDeltaTheta);
                    currentTurnRate = Mathf.Max(0f, currentTurnRate - turnRateDecay * Time.fixedDeltaTime);

                    // ���񑬓x����Ɨݐϐ���p�x���
                    rotMag = Mathf.Min(rotMag, currentTurnRate);
                    float frameDeltaTheta = Mathf.Min(rotMag * Time.fixedDeltaTime, remainingTheta);

                    // �i�s�����X�V
                    if (frameDeltaTheta > 0f && rotAxis.sqrMagnitude > 0.000001f)
                    {
                        newDir = Quaternion.AngleAxis(frameDeltaTheta, rotAxis) * velocity.normalized;
                        usedDeltaTheta += frameDeltaTheta;
                    }
                }
            }

            lastDirToTarget = dirToTarget;
        }

        // --- ���i ---
        speed += acceleration * Time.fixedDeltaTime;
        speed = Mathf.Clamp(speed, 0f, maxspeed);
        velocity = newDir.normalized * speed;

        previousPos = transform.position;
        transform.position += velocity * Time.fixedDeltaTime;
        currentPos = transform.position;

        // --- ��]�X�V ---
        if (velocity.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(velocity.normalized);

        // --- �Փ˔��� ---
        RaycastHitCheck();
        if (!gameObject.activeSelf) return;

        if (ResolvePlannedGroundImpact())
        {
            rangeover();
            return;
        }

        detectflare(isheatseeker);
    }

    private bool ResolvePlannedGroundImpact()
    {
        ProbeGroundImpact(false);

        if (!hasPlannedGroundHit)
            return false;

        Vector3 move = currentPos - previousPos;
        if (move.sqrMagnitude <= 0.000001f)
            return false;

        Vector3 fromPrevious = plannedGroundHitPoint - previousPos;
        Vector3 fromCurrent = plannedGroundHitPoint - currentPos;
        if (Vector3.Dot(fromPrevious, fromCurrent) > 0f)
            return false;

        ImpactEffectFactory.Spawn(plannedGroundHitPoint, effectRadius);
        transform.position = plannedGroundHitPoint;
        hasPlannedGroundHit = false;
        return true;
    }

    private void ProbeGroundImpact(bool force)
    {
        Vector3 probeVelocity = velocity.sqrMagnitude > 0.001f
            ? velocity
            : newDir.normalized * Mathf.Max(speed, 0f);

        if (probeVelocity.y > 0f)
        {
            hasPlannedGroundHit = false;
            groundProbeFrame = 0;
            return;
        }

        if (!force)
        {
            groundProbeFrame--;
            if (groundProbeFrame > 0)
                return;
        }

        groundProbeFrame = GroundProbeIntervalFrames;
        hasPlannedGroundHit = false;

        if (probeVelocity.sqrMagnitude <= 0.000001f)
            return;

        float probeDistance = Mathf.Max(1000f, probeVelocity.magnitude * Mathf.Max(lifeTime, Time.fixedDeltaTime));
        if (Physics.Raycast(
            transform.position,
            probeVelocity.normalized,
            out RaycastHit hit,
            probeDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore) &&
            ObjectGroundBounds.IsGroundCollider(hit.collider))
        {
            plannedGroundHitPoint = hit.point;
            hasPlannedGroundHit = true;
        }
    }

    private bool ShouldUsePurePursuitAssist()
    {
        return usePurePursuitAssistForLaunchOverride && launchDirectionOverride.sqrMagnitude > 0.001f;
    }

    private void ApplyPurePursuitAssist(Vector3 dirToTarget, float purePursuitTurnRate)
    {
        Vector3 currentDir = newDir.sqrMagnitude > 0.001f ? newDir.normalized : velocity.normalized;
        if (currentDir.sqrMagnitude <= 0.001f) return;

        float frameDeltaTheta = Mathf.Max(0f, purePursuitTurnRate * Time.fixedDeltaTime);
        if (frameDeltaTheta <= 0f) return;

        Vector3 assistedDir = Vector3.RotateTowards(
            currentDir,
            dirToTarget,
            frameDeltaTheta * Mathf.Deg2Rad,
            0f
        );

        float actualDeltaTheta = Vector3.Angle(currentDir, assistedDir);
        if (actualDeltaTheta <= 0.0001f) return;

        newDir = assistedDir.normalized;
    }

    // ��������
    void RaycastHitCheck()
    {
        Vector3 dir = (currentPos - previousPos).normalized;
        float dist = Vector3.Distance(previousPos, currentPos);
        if (dist <= 0.0001f) return;

        RaycastHit[] hits = Physics.RaycastAll(
            previousPos,
            dir,
            dist,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            if (ObjectGroundBounds.IsGroundCollider(hit.collider))
            {
                ImpactEffectFactory.Spawn(hit.point, effectRadius);
                rangeover();
                return;
            }
        }
        allies = ObjectManager.Instance.allies;
        foreach (GameObject ally in allies)
        {
            if (ally == null) continue;

            Vector3 allyPos = ally.transform.position;
            float radius = 0.5f * (transform.localScale.x + ally.transform.localScale.x);

            Vector3 closestPoint = previousPos + Vector3.Project(allyPos - previousPos, dir);

            if (Vector3.Distance(closestPoint, allyPos) < radius &&
                Vector3.Dot(allyPos - previousPos, dir) > 0 &&
                Vector3.Distance(previousPos, allyPos) <= dist)
            {
                var status = ally.GetComponent<AugumentStatus>();
                if (status != null)
                {
                    status.damage(power); // �_���[�W�ʂ͓K�X����
                    ImpactEffectFactory.Spawn(closestPoint, effectRadius);
                    rangeover();
                }
                return;
            }
        }
    }

    // ���ŏ���
    void rangeover()
    {
        target = null;
        ObjectManager.Instance?.UnregisterMissile_e(gameObject);
        gameObject.SetActive(false);
    }

    void OnDisable()
    {
        ObjectManager.Instance?.UnregisterMissile_e(gameObject);
    }

    [Header("�V�[�J�[�ݒ�")]
    public float seekerFoV = 30f;          // �V�[�J�[�̎���p
    public float flareHeatModifier = 1f;   // �t���A�M�␳�{��
    public Vector3 seekerDir = Vector3.forward;
    public List<DetectedObject> detectedObjects;
    void detectflare(bool enable = false)
    {
        if (!enable) return;

        seekerDir = target != null ? (target.position - transform.position).normalized : velocity.normalized;
        detectedObjects = new();

        List<GameObject> flares = ObjectManager.Instance.allies;
        foreach (GameObject flare in flares)
        {
            if (flare == null) continue;

            // --- (1) �����Ɗp�x�̎��O���� ---
            Vector3 dirToFlare = (flare.transform.position - transform.position).normalized;
            float angleToFlare = Vector3.Angle(seekerDir, dirToFlare);
            if (angleToFlare > seekerFoV) continue; // ����O�X�L�b�v

            float distance = Vector3.Distance(transform.position, flare.transform.position);

            // --- (2) flare_e�i�����ȃt���A�j���Ƀ`�F�b�N ---
            flare f = flare.GetComponent<flare>();
            if (f != null && f.currentHeat > 0f)
            {
                float heatEffectiveness = f.currentHeat * flareHeatModifier / (distance * distance);
                detectedObjects.Add(new DetectedObject(flare, heatEffectiveness));
            }

            // --- (3) �X�N���v�g�S�T���ithrottle�T���j ---
            foreach (var component in flare.GetComponents<MonoBehaviour>())
            {
                if (component == null) continue;

                Type type = component.GetType();
                FieldInfo throttleField = type.GetField("throttle",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (throttleField == null) continue;

                object value = throttleField.GetValue(component);
                if (value is float currentHeat && currentHeat > 0f)
                {
                    float heatEffectiveness = currentHeat / (distance * distance);
                    detectedObjects.Add(new DetectedObject(flare, heatEffectiveness));
                }
            }
        }

        // --- (4) �ł�����M������b�N ---
        if (detectedObjects.Count > 0)
        {
            detectedObjects.Sort((a, b) => b.strength.CompareTo(a.strength));
            target = detectedObjects[0].obj.transform;
        }
    }

}

