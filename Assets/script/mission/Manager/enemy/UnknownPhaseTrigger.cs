using UnityEngine;

public class UnknownPhaseTrigger : MonoBehaviour
{
    public GameObject @base;
    public string originname;
    public bool isPhaseTrrigersParent;
    public GameObject trigger_empty;
    public float approachDistance = 2000f;

    GameObject player;
    AugumentStatus status;
    float initialHp;
    bool activated;

    void Awake()
    {
        SetupHiddenName();
        CacheTriggerState();
    }

    public void Initialize(GameObject playerObject, float triggerDistance)
    {
        player = playerObject;
        approachDistance = triggerDistance;
        status = GetComponent<AugumentStatus>();
        initialHp = status != null ? status.hp : 0f;
        SetupHiddenName();
        CacheTriggerState();
    }

    void Update()
    {
        if (activated) return;
        if (!isPhaseTrrigersParent) return;

        CacheTriggerState();

        bool approached = player != null &&
            Vector3.Distance(transform.position, player.transform.position) <= approachDistance;
        bool hit = status != null && status.hp < initialHp;

        if (approached || hit)
            Activate();
    }

    void CacheTriggerState()
    {
        if (player == null)
            player = GameObject.FindWithTag("Player");

        if (status == null)
            status = GetComponent<AugumentStatus>();

        if (status != null && initialHp <= 0f)
            initialHp = status.hp;
    }

    void SetupHiddenName()
    {
        if (@base == null)
            @base = gameObject;

        if (string.IsNullOrEmpty(originname))
            originname = @base.name;

        @base.name = "unknown";
    }

    public void Activate()
    {
        activated = true;
        if (@base != null)
            @base.name = originname;

        ClearTriggerEmpty();
        GetComponent<UAVStorageLauncher>()?.BeginLaunch();
        enabled = false;
    }

    void ClearTriggerEmpty()
    {
        if (trigger_empty == null)
            return;

        if (trigger_empty.TryGetComponent(out AugumentStatus triggerStatus))
        {
            ObjectManager.Instance?.UnregisterEnemy(trigger_empty, triggerStatus.waveID);
        }

        Destroy(trigger_empty);
        trigger_empty = null;
    }
}
