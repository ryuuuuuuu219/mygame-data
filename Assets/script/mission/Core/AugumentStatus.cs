using System;
using UnityEngine;

public class AugumentStatus : MonoBehaviour
{
    Vector3 previousPosition;
    public Vector3 Velocity; // 対地速度

    public bool issortie = false;// 出撃済みフラグ spawnTableManagerで設定
    public bool isVisible = true;// レーダーに映るかどうか

    public bool isPlayer;
    public bool missionObjective;
    public bool isEnemy;
    public bool ECM;

    public int waveID = -1;

    public float currentHeat = 1f;
    public float destructionEffectRadius = 12f;
    [Tooltip("撃破時に加算するスコア。0以上ならこの値を使い、負数なら従来通りmaxhpを使う。")]
    public float scoreReward = -1f;

    public float hp,maxhp = 100;
    public float lifeTime = 10f;

    [Header("Status Overrides")]
    public bool preferInspectorStatus = true;
    
    public StatusTable CurrentStatus;
    public bool TryGetHP(out float current, out float max)
    {
        current = hp;
        max = maxhp;

        return max > 0f;
    }

    public float GetScoreReward()
    {
        return scoreReward >= 0f ? scoreReward : maxhp;
    }

    public void SetScoreReward(float reward)
    {
        scoreReward = Mathf.Max(0f, reward);
    }

    public void storeHP()
    {
        if (hp <= 0f)
        {
            hp = CurrentStatus.Get("Aircraft", "HP").value;
            maxhp = hp;
        }
    }

    public void damage(float dmg)
    {
        if (!IsInitialized) return;
        if (hp <= 0f) return;
        if (!issortie) return;
        if (!isVisible) return;

        hp -= dmg;
        if (hp <= 0)
        {
            Die();
        }
    }

    public void altGetVar(string key, out float value)
    {
        value = 0f;
        if (CurrentStatus == null)
        {
            Debug.LogError($"{name}: CurrentStatusが設定されていません！");
            return;
        }
        foreach (var page in CurrentStatus.statusdic)
        {
            foreach (var stat in page.stats)
            {
                if (stat.key == key)
                {
                    value = stat.value;
                    return;
                }
            }
        }
        Debug.LogError(
    $"{name}: キー'{key}'が見つかりません！\n呼び出し元:\n{Environment.StackTrace}",
    this
            );
    }
    public bool IsInitialized { get; private set; }
    public event Action OnInitialized;

    // Start is called before the first frame update
    void Start()
    {
        StatusTable inspectorStatus = CurrentStatus;
        CurrentStatus = new StatusTable();

        if (isPlayer)
        {
            WeaponStorage.ApplyEquippedToPlayerPrefs();
            CurrentStatus = Clone_player();
        }

        if (preferInspectorStatus)
        {
            ApplyInspectorOverrides(CurrentStatus, inspectorStatus);
        }

        if (CurrentStatus != null)
        {
            CurrentStatus = Clone(CurrentStatus);
            IsInitialized = true;
            OnInitialized?.Invoke();
        }

        storeHP();
        previousPosition = transform.position;
    }
    public StatusTable Clone(StatusTable origin)
    {
        var clone = new StatusTable();

        clone.statusdic = new StatPage[origin.statusdic.Length];

        for (int i = 0; i < origin.statusdic.Length; i++)
        {
            clone.statusdic[i] = new StatPage
            {
                pageName = origin.statusdic[i].pageName,
                stats = new StatEntry[origin.statusdic[i].stats.Length]
            };

            for (int j = 0; j < origin.statusdic[i].stats.Length; j++)
            {
                var src = origin.statusdic[i].stats[j];
                if(src.value != 0)//インスペクターで値が設定されている場合
                {
                    clone.statusdic[i].stats[j] = new StatEntry
                    {
                        key = src.key,
                        range = src.range,
                        value = src.value
                    };
                }
                else
                {
                    clone.statusdic[i].stats[j] = new StatEntry
                    {
                        key = src.key,
                        range = src.range,
                        value = src.range.min
                    };
                }
            }
        }
        return clone;
    }

    void ApplyInspectorOverrides(StatusTable target, StatusTable inspector)
    {
        if (target == null || inspector?.statusdic == null) return;

        foreach (var page in inspector.statusdic)
        {
            if (page?.stats == null) continue;

            foreach (var stat in page.stats)
            {
                if (stat == null || string.IsNullOrEmpty(stat.key)) continue;
                if (Mathf.Approximately(stat.value, 0f)) continue;

                var targetStat = TryGetStat(target, page.pageName, stat.key);
                if (targetStat == null) continue;

                targetStat.range = stat.range;
                targetStat.value = stat.value;
            }
        }
    }

    StatEntry TryGetStat(StatusTable table, string pageName, string key)
    {
        if (table?.statusdic == null) return null;

        foreach (var page in table.statusdic)
        {
            if (page == null || page.pageName != pageName || page.stats == null) continue;

            foreach (var stat in page.stats)
            {
                if (stat != null && stat.key == key) return stat;
            }
        }

        return null;
    }

    public StatusTable Clone_player()
    {
        var clone = new StatusTable();

        clone.statusdic = new StatPage[CurrentStatus.statusdic.Length];

        for (int i = 0; i < CurrentStatus.statusdic.Length; i++)
        {
            clone.statusdic[i] = new StatPage
            {
                pageName = CurrentStatus.statusdic[i].pageName,
                stats = new StatEntry[CurrentStatus.statusdic[i].stats.Length]
            };

            for (int j = 0; j < CurrentStatus.statusdic[i].stats.Length; j++)
            {
                var src = CurrentStatus.statusdic[i].stats[j];
                clone.statusdic[i].stats[j] = new StatEntry
                {
                    key = src.key,
                    range = src.range,
                    value = PlayerPrefs.GetFloat(src.key, src.range.min)
                };
            }
        }
        return clone;
    }

    public event Action OnDestroyed;


    void Die()
    {
        // 爆発エフェクトを再生するなどの処理をここに追加可能
        if (!isPlayer && isEnemy)
        {
            var OM = ObjectManager.Instance;
            float awardedScore = GetScoreReward();
            OM.destroyedUIflag = true;
            OM.score += awardedScore;
            Debug.Log($"{name} destroyed. Score awarded: {awardedScore}");
            ImpactEffectFactory.Spawn(transform.position, destructionEffectRadius);
            GeneratedAudioManager.Play(GeneratedAudioCue.Destroyed, transform.position, 0.85f);

            OnDestroyed?.Invoke();
            OM.UnregisterEnemy(gameObject, waveID);
            Destroy(gameObject);
        }
        else if (isPlayer && !isEnemy)
        {

        }
        else
        {
            Debug.LogError("Undefined!");
        }
    }

    private void Update()
    {
        if (!issortie)return;
        if (lifeTime > 0)
        {
            lifeTime -= Time.deltaTime;
            if (lifeTime <= 0) Die();
        }

        velocityUpdate();
    }

    void velocityUpdate()
    {
        Velocity = transform.position - previousPosition;
        previousPosition = transform.position;
    }
}

[System.Serializable]
public struct modify
{
    public float min, max, step;

    public float Lower => Mathf.Min(min, max);
    public float Upper => Mathf.Max(min, max);
}


[Serializable]
public class StatEntry
{
    public string key;
    public modify range;
    public float value;
}

[Serializable]
public class StatPage
{
    public string pageName;
    public StatEntry[] stats;
}

[Serializable]
public class StatusTable
{
    public StatPage[] statusdic = new StatPage[]
    {
        // ■1ページ目（機体）
        new StatPage
        {
            pageName = "Aircraft",
            stats = new StatEntry[]
            {
                new StatEntry{ key = "HP", range = new modify{ min=100f, max=5000f, step=10f }},
                new StatEntry{ key = "機動性(ピッチ)", range = new modify{ min=4f, max=20f, step=0.1f }},
                new StatEntry{ key = "機動性(ロール)", range = new modify{ min=4f, max=20f, step=0.1f }},
                new StatEntry{ key = "機動性(ヨー)", range = new modify{ min=4f, max=20f, step=0.1f }},
                new StatEntry{ key = "加速度", range = new modify{ min=100f, max=200f, step=5f } },
                new StatEntry{ key = "最高速度", range = new modify{ min=300f, max=800f, step=5f } }
            }
        },
        // ■2ページ目（機銃）
        new StatPage
        {
            pageName = "Gun",
            stats = new StatEntry[]
            {
                new StatEntry{ key = "銃弾：発射レート", range = new modify{ min=4f, max=60f, step=0.1f } },
                new StatEntry{ key = "銃弾：射程", range = new modify{ min=450f, max=1000f, step=10f } },
                new StatEntry{ key = "銃弾：威力", range = new modify{ min=3f, max=40f, step=0.05f } },
                new StatEntry{ key = "銃弾：当たり判定サイズ", range = new modify{ min=1f, max=8f, step=0.05f } },
                new StatEntry{ key = "銃弾：弾数", range = new modify{ min=120f, max=2000f, step=5f } },
                new StatEntry{ key = "銃弾：初速", range = new modify{ min=100f, max=1200f, step=5f } }
            }
        },
        // ■3ページ目（標準ミサイル）
        new StatPage
        {
            pageName = "StandardMissile",
            stats = new StatEntry[]
            {
                new StatEntry{ key = "ミサイル：初速", range = new modify{ min=200f, max=600f, step=5f } },
                new StatEntry{ key = "ミサイル：威力", range = new modify{ min=10f, max=200f, step=5f } },
                new StatEntry{ key = "ミサイル：最高速", range = new modify{ min=300f, max=1000f, step=5f } },
                new StatEntry{ key = "ミサイル：加速度", range = new modify{ min=0f, max=100f, step=1f } },
                new StatEntry{ key = "ミサイル：誘導力", range = new modify{ min=45f, max=360f, step=1f } },
                new StatEntry{ key = "ミサイル：誘導象限", range = new modify{ min=45f, max=180f, step=1f } },
                new StatEntry{ key = "ミサイル：飛翔時間", range = new modify{ min=6f, max=30f, step=0.5f } },
                new StatEntry{ key = "ミサイル：射程（ロック可能距離）", range = new modify{ min=700f, max=2000f, step=10f } },
                new StatEntry{ key = "ミサイル：比例航法定数", range = new modify{ min=1f, max=10f, step=0.1f } },
                new StatEntry{ key = "ミサイル：弾数", range = new modify{ min=20f, max=180f, step=0.25f } },
                new StatEntry{ key = "ミサイル：装填時間", range = new modify{ min=5f, max=0.5f, step=-0.1f } },
                new StatEntry{ key = "ミサイル：誘導目標の固定", range = new modify{ min=0.01f, max=1f, step=0.01f } }
            }
        },
        // ■4ページ
        new StatPage
        {
            pageName = "nAAM",
            stats = new StatEntry[]
            {
                new StatEntry{ key = "長射程マルチロックミサイル：射程（ロック可能距離）", range = new modify{ min=1500f, max=5000f, step=5f } },
                new StatEntry{ key = "長射程マルチロックミサイル：飛翔時間", range = new modify{ min=5f, max=30f, step=0.5f } },
                new StatEntry{ key = "長射程マルチロックミサイル：マルチロック数", range = new modify{ min=1f, max=10f, step=0.2f } },
                new StatEntry{ key = "長射程マルチロックミサイル：弾数", range = new modify{ min=8f, max=180f, step=0.25f } },
            }
        },
        // ■5ページ
        new StatPage
        {
            pageName = "UGB",
            stats = new StatEntry[]
            {
                new StatEntry{ key = "UGB：加害範囲", range = new modify{ min=10f, max=500f, step=1f } },
                new StatEntry{ key = "UGB：威力", range = new modify{ min=100f, max=3000f, step=10f } },
                new StatEntry{ key = "UGB：炸裂範囲", range = new modify{ min=10f, max=500f, step=1f } },
                new StatEntry{ key = "UGB：弾数", range = new modify{ min=4f, max=50f, step=0.25f } },
            }
        }
    };

    public StatEntry SearchOfkey(string key, out float vari)
    {
        foreach (var page in statusdic)
        {
            for (int i = 0; i < page.stats.Length; i++)
            {
                if (page.stats[i].key == key)
                {
                    vari = GetVar(key);
                    return page.stats[i];
                }
            }
        }
        vari = 0f;
        return null;
    }

    public ref float GetVar(string key)
    {
        foreach (var page in statusdic)
        {
            foreach (var stat in page.stats)
            {
                if (stat.key == key)
                {
                    return ref stat.value;
                }
            }
        }
        throw new Exception("Key not found: " + key);
    }

    public StatEntry Get(string page, string key)
    {
        foreach (var p in statusdic)
        {
            if (p.pageName != page) continue;

            foreach (var s in p.stats)
            {
                if (s.key == key) return s;
            }
        }
        throw new Exception($"Stat not found: {page}.{key}");
    }
}
