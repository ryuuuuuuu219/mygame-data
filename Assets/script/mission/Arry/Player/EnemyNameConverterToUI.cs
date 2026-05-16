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
        new NameConversion { beforeName = "AceM03Special", afterName = "ACE SPECIAL" },
        new NameConversion { beforeName = "AA_GUN", afterName = "AA GUN" },
        new NameConversion { beforeName = "SAM", afterName = "SAM" },
        new NameConversion { beforeName = "LASM", afterName = "LASM" }
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
