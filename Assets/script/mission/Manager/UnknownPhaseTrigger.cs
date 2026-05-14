using UnityEngine;

public class UnknownPhaseTrigger : MonoBehaviour
{
    UAVStorageMissionController controller;
    GameObject player;
    AugumentStatus status;
    float approachDistance;
    float initialHp;
    bool activated;

    public void Initialize(UAVStorageMissionController owner, GameObject playerObject, float triggerDistance)
    {
        controller = owner;
        player = playerObject;
        approachDistance = triggerDistance;
        status = GetComponent<AugumentStatus>();
        initialHp = status != null ? status.hp : 0f;
    }

    void Update()
    {
        if (activated) return;

        bool approached = player != null &&
            Vector3.Distance(transform.position, player.transform.position) <= approachDistance;
        bool hit = status != null && status.hp < initialHp;

        if (approached || hit)
            Activate();
    }

    void Activate()
    {
        activated = true;
        controller.ActivateStorage(gameObject);
        enabled = false;
    }
}
