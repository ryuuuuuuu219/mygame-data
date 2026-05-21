using System.Collections.Generic;
using UnityEngine;

public static class ECMManager
{
    public struct JammerEffectEntry
    {
        public GameObject jammer;
        public List<GameObject> effected;

        public JammerEffectEntry(GameObject jammer)
        {
            this.jammer = jammer;
            effected = new List<GameObject>();
        }
    }

    public static readonly List<JammerEffectEntry> JammerEffects = new();

    public static void RegisterJammer(GameObject jammer)
    {
        if (jammer == null) return;
        if (FindEntryIndex(jammer) >= 0) return;

        JammerEffects.Add(new JammerEffectEntry(jammer));
    }

    public static void UnregisterJammer(GameObject jammer)
    {
        if (jammer == null) return;

        int index = FindEntryIndex(jammer);
        if (index < 0) return;

        var affectedByRemovedJammer = new List<GameObject>(JammerEffects[index].effected);
        JammerEffects.RemoveAt(index);

        for (int i = 0; i < affectedByRemovedJammer.Count; i++)
        {
            RefreshTarget(affectedByRemovedJammer[i]);
        }
    }

    public static void SetEffect(GameObject jammer, GameObject target, bool value)
    {
        if (jammer == null || target == null) return;

        int index = FindOrCreateEntry(jammer);
        var entry = JammerEffects[index];
        entry.effected.RemoveAll(item => item == null);

        if (value)
        {
            if (!entry.effected.Contains(target))
                entry.effected.Add(target);

            SetTargetEcm(target, true);
        }
        else
        {
            entry.effected.Remove(target);
            RefreshTarget(target);
        }

        JammerEffects[index] = entry;
    }

    public static bool IsEffected(GameObject target)
    {
        if (target == null) return false;

        for (int i = 0; i < JammerEffects.Count; i++)
        {
            var entry = JammerEffects[i];
            if (entry.jammer == null || !entry.jammer.activeInHierarchy) continue;
            if (entry.effected != null && entry.effected.Contains(target))
                return true;
        }

        return false;
    }

    static void RefreshTarget(GameObject target)
    {
        if (target == null) return;

        bool isJammer = target.TryGetComponent(out ECMJammer jammer) && jammer.isActiveAndEnabled;
        SetTargetEcm(target, isJammer || IsEffected(target));
    }

    static void SetTargetEcm(GameObject target, bool value)
    {
        if (target == null) return;
        if (!target.TryGetComponent(out AugumentStatus status)) return;

        status.ECM = value;
    }

    static int FindOrCreateEntry(GameObject jammer)
    {
        int index = FindEntryIndex(jammer);
        if (index >= 0) return index;

        JammerEffects.Add(new JammerEffectEntry(jammer));
        return JammerEffects.Count - 1;
    }

    static int FindEntryIndex(GameObject jammer)
    {
        for (int i = JammerEffects.Count - 1; i >= 0; i--)
        {
            if (JammerEffects[i].jammer == null)
            {
                JammerEffects.RemoveAt(i);
                continue;
            }

            if (JammerEffects[i].jammer == jammer)
                return i;
        }

        return -1;
    }
}
