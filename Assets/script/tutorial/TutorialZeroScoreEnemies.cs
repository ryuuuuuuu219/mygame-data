using UnityEngine;

public class TutorialZeroScoreEnemies : MonoBehaviour
{
    public bool applyOnStart = true;
    public bool keepApplying = true;
    public float interval = 0.25f;

    float timer;

    void Start()
    {
        if (applyOnStart)
            Apply();
    }

    void Update()
    {
        if (!keepApplying) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        timer = Mathf.Max(0.05f, interval);
        Apply();
    }

    void Apply()
    {
        if (ObjectManager.Instance != null)
        {
            foreach (var enemy in ObjectManager.Instance.Enemies)
                SetZeroReward(enemy);
        }

        var statuses = FindObjectsByType<AugumentStatus>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        foreach (var status in statuses)
        {
            if (status != null && status.isEnemy)
                status.SetScoreReward(0f);
        }
    }

    void SetZeroReward(GameObject enemy)
    {
        if (enemy == null) return;
        if (enemy.TryGetComponent(out AugumentStatus status))
            status.SetScoreReward(0f);
    }
}
