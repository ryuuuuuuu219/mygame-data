using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyGunLeadBiasAddon : MonoBehaviour
{
    public float biasDegreeSpeed = 1f;
    public float biasDegree = 5f;
    public float rerollAngleThreshold = 45f;
    public float centerSnapThreshold = 30f;

    readonly List<BiasState> biasStates = new();
    readonly List<LineRenderer> debugAimLines = new();
    readonly List<LineRenderer> debugBiasTargetLines = new();

    struct BiasState
    {
        public Vector2 angle;
        public Vector2 targetAngle;
        public int lastUpdateFrame;
    }

    public void EnsureBarrelCount(int count)
    {
        count = Mathf.Max(1, count);

        while (biasStates.Count < count)
            biasStates.Add(NewBiasState(Vector2.zero));

        if (biasStates.Count > count)
            biasStates.RemoveRange(count, biasStates.Count - count);
    }

    public void UpdateBiasAngles(int count)
    {
        EnsureBarrelCount(count);

        for (int i = 0; i < biasStates.Count; i++)
            UpdateBiasAngle(i);
    }

    void UpdateBiasAngle(int barrelIndex)
    {
        BiasState state = biasStates[barrelIndex];
        float step = biasDegreeSpeed * Time.deltaTime;
        bool advancedThisFrame = state.lastUpdateFrame != Time.frameCount && step > 0f;

        if (advancedThisFrame)
        {
            state.angle = Vector2.MoveTowards(state.angle, state.targetAngle, step);
            state.lastUpdateFrame = Time.frameCount;
            RefreshTargetIfReached(ref state);
        }

        biasStates[barrelIndex] = state;
    }

    void RefreshTargetIfReached(ref BiasState state)
    {
        if (Vector2.Distance(state.angle, state.targetAngle) > 0.1f)
            return;

        state.targetAngle = PickTargetAngle(state.angle);
    }

    public Vector2 GetBiasAngle(int barrelIndex)
    {
        return barrelIndex >= 0 && barrelIndex < biasStates.Count
            ? biasStates[barrelIndex].angle
            : Vector2.zero;
    }

    public Vector2 GetBiasTargetAngle(int barrelIndex)
    {
        return barrelIndex >= 0 && barrelIndex < biasStates.Count
            ? biasStates[barrelIndex].targetAngle
            : Vector2.zero;
    }

    public void UpdateDebugLines(
        Transform gunMuzzle,
        float debugAimLineLength,
        int barrelIndex,
        bool showDebugAimLine,
        Vector3 biasDirection,
        Vector3 targetDirection)
    {
        if (!showDebugAimLine || gunMuzzle == null) return;

        EnsureDebugAimLine(barrelIndex);
        EnsureDebugBiasTargetLine(barrelIndex);

        Vector3 start = gunMuzzle.position;

        LineRenderer aimLine = debugAimLines[barrelIndex];
        aimLine.SetPosition(0, start);
        aimLine.SetPosition(1, start + biasDirection.normalized * debugAimLineLength);

        LineRenderer targetLine = debugBiasTargetLines[barrelIndex];
        targetLine.SetPosition(0, start);
        targetLine.SetPosition(1, start + targetDirection.normalized * debugAimLineLength);
    }

    BiasState NewBiasState(Vector2 previousAngle)
    {
        return new BiasState
        {
            angle = PickTargetAngle(previousAngle),
            targetAngle = PickTargetAngle(previousAngle),
            lastUpdateFrame = -1,
        };
    }

    Vector2 PickTargetAngle(Vector2 previousAngle)
    {
        if (biasDegree <= 0f)
            return Vector2.zero;

        if (previousAngle.sqrMagnitude <= 0.001f)
            return AngleToVector(Random.Range(0f, 360f));

        float angleA = Mathf.Atan2(previousAngle.y, previousAngle.x) * Mathf.Rad2Deg;
        float angleOffset = Random.Range(rerollAngleThreshold, 360f - rerollAngleThreshold);
        float angleB = angleA + angleOffset;

        if (Mathf.Abs(angleOffset - 180f) <= centerSnapThreshold)
            return Vector2.zero;

        return AngleToVector(angleB);
    }

    Vector2 AngleToVector(float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * biasDegree;
    }

    void EnsureDebugAimLine(int barrelIndex)
    {
        while (debugAimLines.Count <= barrelIndex)
            debugAimLines.Add(null);

        if (debugAimLines[barrelIndex] != null) return;

        debugAimLines[barrelIndex] = CreateDebugLine($"DebugAimLine_{barrelIndex + 1:00}", Color.red);
    }

    void EnsureDebugBiasTargetLine(int barrelIndex)
    {
        while (debugBiasTargetLines.Count <= barrelIndex)
            debugBiasTargetLines.Add(null);

        if (debugBiasTargetLines[barrelIndex] != null) return;

        debugBiasTargetLines[barrelIndex] = CreateDebugLine($"DebugBiasTargetLine_{barrelIndex + 1:00}", Color.yellow);
    }

    LineRenderer CreateDebugLine(string lineName, Color color)
    {
        GameObject lineObject = new GameObject(lineName);
        lineObject.transform.SetParent(transform, false);
        LineRenderer line = lineObject.AddComponent<LineRenderer>();

        line.positionCount = 2;
        line.useWorldSpace = true;
        line.startWidth = 0.5f;
        line.endWidth = 0.5f;
        line.startColor = color;
        line.endColor = color;
        line.material = new Material(Shader.Find("Sprites/Default"));
        return line;
    }
}
