using UnityEngine;

public class AirBattleshipDamageRelay : MonoBehaviour
{
    public AirBattleshipBase battleshipBase;

    void Awake()
    {
        if (battleshipBase == null)
            battleshipBase = GetComponentInParent<AirBattleshipBase>();
    }

    public bool TryGetDamageTarget(out AugumentStatus status)
    {
        status = null;
        if (battleshipBase == null)
            battleshipBase = GetComponentInParent<AirBattleshipBase>();

        if (battleshipBase == null || battleshipBase.CoreBlock == null)
            return false;

        return battleshipBase.CoreBlock.TryGetComponent(out status) && status != null;
    }
}
