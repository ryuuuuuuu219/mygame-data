using UnityEngine;

[AddComponentMenu("Air Combat/Air Combat Behavior Visual Graph Viewer")]
public class AirCombatBehaviorVisualGraphViewer : MonoBehaviour
{
    [SerializeField] AirCombatBehaviorServer server;

    public AirCombatBehaviorServer Server => server;

    void Reset()
    {
        server = GetComponent<AirCombatBehaviorServer>();
    }
}
