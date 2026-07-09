using UnityEngine;

/// <summary>
/// 再現可能な解析試験に使う、EnemyAircraftAIGen4ベースの標的用AI。
/// 乱数を使わず、同じ初期条件から同じ判断と機動を行う。
/// </summary>
public sealed class EnemyAircraftAnalysisTargetAI : EnemyAircraftAIGen4
{
    [Header("Analysis Target")]
    [SerializeField, Min(0.01f)]
    float commandDirectionSmoothing = 8f;

    protected override bool DeterministicAnalysisMode => true;
    protected override float CommandDirectionSmoothing => commandDirectionSmoothing;
}
