using UnityEngine;

public class EnemyNameConverterToUI : MonoBehaviour
{
    [System.Serializable]
    public struct NameConversion
    {
        public string beforeName;
        public string afterName;
    }

    public NameConversion[] conversions = new NameConversion[]
    {
        new NameConversion { beforeName = "Ace", afterName = "ACE" },
        new NameConversion { beforeName = "ACE_M03", afterName = "ACE" },
        new NameConversion { beforeName = "AceM03Special", afterName = "ACE-2" },
        new NameConversion { beforeName = "AA_GUN", afterName = "AA" },
        new NameConversion { beforeName = "HR_AAGUN", afterName = "AA-2" },
        new NameConversion { beforeName = "SAM", afterName = "SAM" },
        new NameConversion { beforeName = "LASM", afterName = "LASM" },
        new NameConversion { beforeName = "Railgun", afterName = "Railgun" },
        new NameConversion { beforeName = "JAMMER", afterName = "JAMMER" },
        new NameConversion { beforeName = "AIR_BATTLESHIP", afterName = "BATTLESHIP" },
        new NameConversion { beforeName = "ALLY_ACE", afterName = "ALLY" },
        new NameConversion { beforeName = "TRIGGER_EMPTY", afterName = "TRIGGER" },
        new NameConversion { beforeName = "UAV_STORAGE", afterName = "UAV STORAGE" },
        new NameConversion { beforeName = "fighter", afterName = "FIGHTER" },
        new NameConversion { beforeName = "acem05", afterName = "ACE" },
        new NameConversion { beforeName = "ace_m03", afterName = "ACE" },
        new NameConversion { beforeName = "fighter_m03_special", afterName = "ACE-2" }
    };

    public string converter(GameObject obj)
    {
        if (obj == null)
        {
            return "";
        }

        string sourceName = NormalizeName(obj.name);
        foreach (NameConversion conversion in conversions)
        {
            if (string.IsNullOrEmpty(conversion.beforeName))
            {
                continue;
            }

            if (sourceName == NormalizeName(conversion.beforeName))
            {
                return string.IsNullOrEmpty(conversion.afterName)
                    ? sourceName
                    : conversion.afterName;
            }
        }

        return sourceName;
    }

    private string NormalizeName(string sourceName)
    {
        if (string.IsNullOrEmpty(sourceName))
        {
            return "";
        }

        return sourceName.Replace("(Clone)", "").Trim();
    }
}
