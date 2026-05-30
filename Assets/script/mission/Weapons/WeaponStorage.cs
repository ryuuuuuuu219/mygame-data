using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public enum WeaponDropType
{
    Gun = 0,
    StandardMissile = 1,
    UGB = 2,
    nAAM = 3,
    Barrier = 10,
    Funnel = 11,
    Laser = 12,
    Railgun = 13,
    Discharge = 20,
    AntiAirGun = 21,
    KnockbackBomb = 22,
    FreezeBeam = 23
}

[Serializable]
public class WeaponDropData
{
    public int version = 1;
    public string instanceId;
    public int weaponTypeId;
    public string weaponTypeName;
    public int serialInType;
    public string displayName;
    public int level;
    public string sourceStage;
    public int sourceStageIndex;
    public float sourceScore;
    public string createdAt;
    public bool equipped;
    public bool discarded;
    public int[] upgradePoints;
}

public class WeaponDetailText
{
    public string title;
    public string labels;
    public string pointHeader;
    public string valueHeader;
    public string prevNextHeader;
    public string points;
    public string values;
    public string prevNextValues;
}

public static class WeaponStorage
{
    const string StorageFolderName = "weapon_storage";
    const string SelectedPrefix = "SelectedWeapon_";

    static readonly WeaponDropType[] InitialTypes =
    {
        WeaponDropType.Gun,
        WeaponDropType.StandardMissile,
        WeaponDropType.UGB,
        WeaponDropType.nAAM
    };

    static readonly WeaponDropType[] DropTypes =
    {
        WeaponDropType.Gun,
        WeaponDropType.StandardMissile,
        WeaponDropType.UGB,
        WeaponDropType.nAAM
    };

    public static string StoragePath => Path.Combine(Application.persistentDataPath, StorageFolderName);

    public static List<WeaponDropData> LoadAll(bool includeDiscarded = false)
    {
        EnsureStorage();
        var list = new List<WeaponDropData>();

        foreach (string path in Directory.GetFiles(StoragePath, "*.json"))
        {
            try
            {
                var data = JsonUtility.FromJson<WeaponDropData>(File.ReadAllText(path));
                if (data == null) continue;
                Normalize(data);
                if (!includeDiscarded && data.discarded) continue;
                list.Add(data);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Weapon JSON load skipped: {path}\n{ex.Message}");
            }
        }

        if (list.Count == 0)
        {
            CreateInitialWeapons();
            return LoadAll(includeDiscarded);
        }

        return list;
    }

    public static WeaponDropData GenerateDrop(float finalScore, int stageIndex, string stageName)
    {
        EnsureStorage();
        int point = Mathf.Max(100, Mathf.FloorToInt(100f + finalScore / 100f));
        var type = DropTypes[UnityEngine.Random.Range(0, DropTypes.Length)];
        var data = CreateWeapon(type, point, stageIndex, stageName, finalScore);
        Save(data);
        return data;
    }

    public static void Equip(WeaponDropData target)
    {
        if (target == null) return;

        var all = LoadAll(true);
        foreach (var data in all)
        {
            if (data.weaponTypeId == target.weaponTypeId)
            {
                data.equipped = data.instanceId == target.instanceId;
                Save(data);
            }
        }

        PlayerPrefs.SetString(SelectedPrefix + target.weaponTypeId, target.instanceId);
        ApplyToPlayerPrefs(target);
        PlayerPrefs.Save();
    }

    public static void ApplyEquippedToPlayerPrefs()
    {
        foreach (var data in LoadAll())
        {
            string selectedId = PlayerPrefs.GetString(SelectedPrefix + data.weaponTypeId, "");
            if (data.equipped || data.instanceId == selectedId)
            {
                ApplyToPlayerPrefs(data);
            }
        }
    }

    public static void Discard(WeaponDropData target)
    {
        if (target == null || target.equipped) return;
        target.discarded = true;
        Save(target);
    }

    public static string GetShortTypeName(WeaponDropData data)
    {
        return GetShortTypeName((WeaponDropType)data.weaponTypeId);
    }

    public static string BuildDetailText(WeaponDropData data, int page = 0)
    {
        var detail = BuildDetailColumns(data, page);
        return detail.title + "\n\n" + detail.labels;
    }

    public static WeaponDetailText BuildDetailColumns(WeaponDropData data, int page = 0)
    {
        var detail = new WeaponDetailText();
        if (data == null)
        {
            detail.title = "No weapon selected.";
            detail.labels = "";
            detail.pointHeader = "";
            detail.valueHeader = "";
            detail.prevNextHeader = "";
            detail.points = "";
            detail.values = "";
            detail.prevNextValues = "";
            return detail;
        }

        var table = new StatusTable();
        var keys = GetStatKeys((WeaponDropType)data.weaponTypeId);
        detail.title = $"{GetShortTypeName(data)}\tlv.{data.level}";
        detail.labels = "";
        detail.pointHeader = "[Pt]";
        detail.valueHeader = "[Value]";
        detail.prevNextHeader = "[Prev/Next]";
        detail.points = "";
        detail.values = "";
        detail.prevNextValues = "";

        for (int i = 0; i < keys.Length; i++)
        {
            string key = keys[i];
            var entry = table.SearchOfkey(key, out _);
            if (entry == null) continue;

            int point = i < data.upgradePoints.Length ? data.upgradePoints[i] : 0;
            if (page == 1 && point == 0) continue;

            float value = CalculateValue(entry.range, point);
            string prev = point > 0 ? CalculateValue(entry.range, point - 1).ToString("F1") : "-";
            string next = point < GetMaxPoint(entry.range) ? CalculateValue(entry.range, point + 1).ToString("F1") : "-";
            detail.labels += GetDisplayName(key) + "\n";
            detail.points += $"+{point}\n";
            detail.values += $"{value:F1}\n";
            detail.prevNextValues += $"{prev}/{next}\n";
        }

        return detail;
    }

    static void CreateInitialWeapons()
    {
        foreach (var type in InitialTypes)
        {
            var data = CreateWeapon(type, 0, 0, "Initial", 0f);
            data.equipped = true;
            Save(data);
            PlayerPrefs.SetString(SelectedPrefix + data.weaponTypeId, data.instanceId);
            ApplyToPlayerPrefs(data);
        }
        PlayerPrefs.Save();
    }

    static WeaponDropData CreateWeapon(WeaponDropType type, int level, int stageIndex, string stageName, float score)
    {
        int serial = NextSerial(type);
        var points = AllocatePoints(type, level);
        var data = new WeaponDropData
        {
            version = 1,
            instanceId = $"{GetShortTypeName(type)}-{serial:000000}",
            weaponTypeId = (int)type,
            weaponTypeName = type.ToString(),
            serialInType = serial,
            level = Sum(points),
            sourceStage = string.IsNullOrEmpty(stageName) ? "Unknown" : stageName,
            sourceStageIndex = stageIndex,
            sourceScore = score,
            createdAt = DateTime.Now.ToString("o"),
            equipped = false,
            discarded = false,
            upgradePoints = points
        };
        data.displayName = $"{GetShortTypeName(type)} lv.{data.level} #{serial:0000}";
        return data;
    }

    static int[] AllocatePoints(WeaponDropType type, int level)
    {
        var keys = GetStatKeys(type);
        var points = new int[keys.Length];
        if (level <= 0 || keys.Length == 0) return points;

        var table = new StatusTable();
        int guard = level * 20;
        int remaining = level;
        while (remaining > 0 && guard-- > 0)
        {
            int index = UnityEngine.Random.Range(0, keys.Length);
            var entry = table.SearchOfkey(keys[index], out _);
            if (entry == null) continue;

            int maxPoint = GetMaxPoint(entry.range);
            if (points[index] >= maxPoint) continue;

            points[index]++;
            remaining--;
        }

        return points;
    }

    static void ApplyToPlayerPrefs(WeaponDropData data)
    {
        var table = new StatusTable();
        var keys = GetStatKeys((WeaponDropType)data.weaponTypeId);

        for (int i = 0; i < keys.Length; i++)
        {
            var entry = table.SearchOfkey(keys[i], out _);
            if (entry == null) continue;

            int point = i < data.upgradePoints.Length ? data.upgradePoints[i] : 0;
            PlayerPrefs.SetFloat(keys[i], CalculateValue(entry.range, point));
        }
    }

    static float CalculateValue(modify range, int point)
    {
        float value = range.min + Mathf.Max(0, point) * range.step;
        return Mathf.Clamp(value, range.Lower, range.Upper);
    }

    static int GetMaxPoint(modify range)
    {
        if (Mathf.Approximately(range.step, 0f)) return 0;
        return Mathf.Max(0, Mathf.FloorToInt(Mathf.Abs((range.max - range.min) / range.step)));
    }

    static void Save(WeaponDropData data)
    {
        EnsureStorage();
        Normalize(data);
        string path = Path.Combine(StoragePath, data.instanceId + ".json");
        File.WriteAllText(path, JsonUtility.ToJson(data, true));
    }

    static void EnsureStorage()
    {
        if (!Directory.Exists(StoragePath))
            Directory.CreateDirectory(StoragePath);
    }

    static void Normalize(WeaponDropData data)
    {
        if (data.upgradePoints == null)
            data.upgradePoints = new int[GetStatKeys((WeaponDropType)data.weaponTypeId).Length];

        data.level = Sum(data.upgradePoints);
        if (string.IsNullOrEmpty(data.weaponTypeName))
            data.weaponTypeName = ((WeaponDropType)data.weaponTypeId).ToString();
        if (string.IsNullOrEmpty(data.displayName))
            data.displayName = $"{GetShortTypeName(data)} lv.{data.level} #{data.serialInType:0000}";
    }

    static int NextSerial(WeaponDropType type)
    {
        int max = 0;
        EnsureStorage();
        foreach (string path in Directory.GetFiles(StoragePath, "*.json"))
        {
            try
            {
                var data = JsonUtility.FromJson<WeaponDropData>(File.ReadAllText(path));
                if (data != null && data.weaponTypeId == (int)type)
                    max = Mathf.Max(max, data.serialInType);
            }
            catch
            {
                // Broken weapon files are ignored for serial allocation.
            }
        }
        return max + 1;
    }

    static int Sum(int[] values)
    {
        int sum = 0;
        if (values == null) return 0;
        foreach (int value in values) sum += value;
        return sum;
    }

    static string[] GetStatKeys(WeaponDropType type)
    {
        switch (type)
        {
            case WeaponDropType.Gun:
                return new[]
                {
                    "銃弾：発射レート",
                    "銃弾：射程",
                    "銃弾：威力",
                    "銃弾：当たり判定サイズ",
                    "銃弾：弾数",
                    "銃弾：初速"
                };
            case WeaponDropType.StandardMissile:
                return new[]
                {
                    "ミサイル：初速",
                    "ミサイル：威力",
                    "ミサイル：最高速",
                    "ミサイル：加速度",
                    "ミサイル：誘導力",
                    "ミサイル：誘導象限",
                    "ミサイル：飛翔時間",
                    "ミサイル：射程（ロック可能距離）",
                    "ミサイル：比例航法定数",
                    "ミサイル：弾数",
                    "ミサイル：装填時間",
                    "ミサイル：誘導目標の固定"
                };
            case WeaponDropType.UGB:
                return new[]
                {
                    "UGB：加害範囲",
                    "UGB：威力",
                    "UGB：炸裂範囲",
                    "UGB：弾数"
                };
            case WeaponDropType.nAAM:
                return new[]
                {
                    "長射程マルチロックミサイル：射程（ロック可能距離）",
                    "長射程マルチロックミサイル：飛翔時間",
                    "長射程マルチロックミサイル：マルチロック数",
                    "長射程マルチロックミサイル：弾数"
                };
            default:
                return Array.Empty<string>();
        }
    }

    static string GetShortTypeName(WeaponDropType type)
    {
        switch (type)
        {
            case WeaponDropType.Gun: return "GUN";
            case WeaponDropType.StandardMissile: return "MSL";
            case WeaponDropType.UGB: return "UGB";
            case WeaponDropType.nAAM: return "AAM";
            default: return type.ToString().ToUpperInvariant();
        }
    }

    static string GetDisplayName(string key)
    {
        int index = key.IndexOf('：');
        return index >= 0 && index + 1 < key.Length ? key.Substring(index + 1) : key;
    }
}
