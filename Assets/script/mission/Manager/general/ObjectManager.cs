using UnityEngine;
using System.Collections.Generic;

public class ObjectManager : MonoBehaviour
{
    public static ObjectManager Instance;   // シングルトン的にアクセスできるようにする

    public GameObject tester_enemy;
    public List<(int, GameObject)> enemies = new ();
    public List<GameObject> missiles_e = new ();

    public List<GameObject> allies = new ();
    public List<GameObject> missiles_a = new ();

    public List<GameObject> flares = new();

    public float score = 0f;

    public IReadOnlyList<GameObject> Enemies
    {
        get
        {
            _enemyCache.Clear();
            foreach (var (_, enemy) in enemies)
            {
                if (enemy != null)
                    _enemyCache.Add(enemy);
            }
            return _enemyCache;
        }
    }

    void SetTester()
    {
        if (tester_enemy == null) return;
        RegisterEnemy(tester_enemy, 0);
    }

    private readonly List<GameObject> _enemyCache = new();


    void Awake()
    {
        Instance = this;
        RegisterAlly(gameObject); // 自分自身を味方リストに登録
        SetTester();
    }

    public SpawnTableManager spawnTableManager;

    void Update()
    {
        HandleUIFlags();
    }

    #region === UI Flags ===

    public bool hitUIflag = false;
    public bool destroyedUIflag = false;
    float hitUITimer = 0f;
    float destroyedUITimer = 0f;

    void HandleUIFlags()
    {
        if (hitUIflag)
        {
            hitUITimer += Time.deltaTime;
            if (hitUITimer > 0.5f)
            {
                hitUIflag = false;
                hitUITimer = 0f;
            }
        }
        if (destroyedUIflag)
        {
            destroyedUITimer += Time.deltaTime;
            if (destroyedUITimer > 0.5f)
            {
                destroyedUIflag = false;
                destroyedUITimer = 0f;
            }
        }
    }

    #endregion

    #region === 登録・解除 ===

    public void RegisterEnemy(GameObject e, int waveID)
    {
        if (!enemies.Contains((waveID, e))) enemies.Add((waveID, e));
    }

    public void UnregisterEnemy(GameObject e, int waveID)
    {
        bool removed = enemies.Remove((waveID, e));
        if (!removed) return;

        bool isTarget = e.TryGetComponent(out AugumentStatus aug) && aug.missionObjective;
        if (aug != null)
            aug.waveID = waveID;

        if (spawnTableManager == null) return;
        spawnTableManager.NotifyEnemyDestroyed(waveID, isTarget);
    }

    public void RegisterAlly(GameObject a)
    {
        if (!allies.Contains(a)) allies.Add(a);
    }

    public void UnregisterAlly(GameObject a)
    {
        allies.Remove(a);
    }
    public void RegisterMissile_e(GameObject m)
    {
        if (!missiles_e.Contains(m)) missiles_e.Add(m);
    }
    public void UnregisterMissile_e(GameObject m)
    {
        missiles_e.Remove(m);
    }
    public void RegisterMissile_a(GameObject m)
    {
        if (!missiles_a.Contains(m)) missiles_a.Add(m);
    }
    public void UnregisterMissile_a(GameObject m)
    {
        missiles_a.Remove(m);
    }

    public void RegisterFlare(GameObject f)
    {
        if (!flares.Contains(f)) flares.Add(f);
    }


    public void UnregisterFlare(GameObject f)
    {
        flares.Remove(f);
    }


    #endregion
}
