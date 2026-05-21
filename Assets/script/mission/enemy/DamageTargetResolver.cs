using UnityEngine;

public static class DamageTargetResolver
{
    public static bool TryGetEnemyStatus(Collider collider, out AugumentStatus status)
    {
        status = null;
        if (collider == null) return false;
        if (collider.GetComponentInParent<InterferenceCollider>() != null) return false;

        status = collider.GetComponentInParent<AugumentStatus>();
        if (status != null && status.isEnemy)
            return true;

        var relay = collider.GetComponentInParent<AirBattleshipDamageRelay>();
        if (relay != null && relay.TryGetDamageTarget(out status) && status != null && status.isEnemy)
            return true;

        status = null;
        return false;
    }
}
