using UnityEngine;

[DisallowMultipleComponent]
public sealed class AirCombatRandomRepositioner : MonoBehaviour
{
    [Header("Execution")]
    [SerializeField] bool repositionOnStart;
    [SerializeField] bool enableKeyboardShortcut = true;
    [SerializeField] KeyCode repositionKey = KeyCode.R;

    [Header("Position")]
    [SerializeField] Transform areaCenter;
    [SerializeField] Vector3 fallbackCenter = new(0f, 2500f, 0f);
    [SerializeField] Vector3 areaSize = new(4000f, 2000f, 4000f);
    [SerializeField, Min(0f)] float minimumSeparation = 800f;
    [SerializeField, Min(1)] int placementAttempts = 32;

    [Header("Rotation")]
    [SerializeField] bool randomizePitch = true;
    [SerializeField] Vector2 pitchRange = new(-30f, 30f);
    [SerializeField] bool randomizeRoll = true;
    [SerializeField] Vector2 rollRange = new(-180f, 180f);

    [Header("Physics")]
    [SerializeField] bool clearVelocity = true;

    [Header("Debug")]
    [SerializeField] Vector3 lastPlayerPosition;
    [SerializeField] Vector3 lastEnemyPosition;
    [SerializeField] Vector3 lastPlayerEulerAngles;
    [SerializeField] Vector3 lastEnemyEulerAngles;

    AirCombatBehaviorAnalyzer analyzer;

    void Awake()
    {
        analyzer = GetComponent<AirCombatBehaviorAnalyzer>();
    }

    void Start()
    {
        if (repositionOnStart)
            Reposition();
    }

    void Update()
    {
        if (enableKeyboardShortcut && Input.GetKeyDown(repositionKey))
            Reposition();
    }

    [ContextMenu("Reposition Player And Enemy")]
    public void Reposition()
    {
        if (analyzer == null)
            analyzer = GetComponent<AirCombatBehaviorAnalyzer>();

        if (analyzer == null || analyzer.playerObject == null || analyzer.enemyObject == null)
        {
            Debug.LogWarning($"{nameof(AirCombatRandomRepositioner)}: Analyzer targets are not assigned.", this);
            return;
        }

        Transform player = analyzer.playerObject.transform;
        Transform enemy = analyzer.enemyObject.transform;
        Vector3 center = areaCenter != null ? areaCenter.position : fallbackCenter;

        Vector3 playerPosition = RandomPosition(center);
        Vector3 enemyPosition = RandomPosition(center);
        float requiredSeparationSqr = minimumSeparation * minimumSeparation;
        for (int i = 0; i < placementAttempts && (enemyPosition - playerPosition).sqrMagnitude < requiredSeparationSqr; i++)
            enemyPosition = RandomPosition(center);

        Quaternion playerRotation = RandomRotation();
        Quaternion enemyRotation = RandomRotation();
        SetPose(player, playerPosition, playerRotation);
        SetPose(enemy, enemyPosition, enemyRotation);

        lastPlayerPosition = playerPosition;
        lastEnemyPosition = enemyPosition;
        lastPlayerEulerAngles = playerRotation.eulerAngles;
        lastEnemyEulerAngles = enemyRotation.eulerAngles;
    }

    Vector3 RandomPosition(Vector3 center)
    {
        Vector3 half = Vector3.Max(Vector3.zero, areaSize) * 0.5f;
        return center + new Vector3(
            Random.Range(-half.x, half.x),
            Random.Range(-half.y, half.y),
            Random.Range(-half.z, half.z));
    }

    Quaternion RandomRotation()
    {
        float pitch = randomizePitch ? Random.Range(pitchRange.x, pitchRange.y) : 0f;
        float yaw = Random.Range(-180f, 180f);
        float roll = randomizeRoll ? Random.Range(rollRange.x, rollRange.y) : 0f;
        return Quaternion.Euler(pitch, yaw, roll);
    }

    void SetPose(Transform target, Vector3 position, Quaternion rotation)
    {
        Rigidbody body = target.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.position = position;
            body.rotation = rotation;
            if (clearVelocity)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }
        else
        {
            target.SetPositionAndRotation(position, rotation);
        }
    }

    void OnValidate()
    {
        areaSize = Vector3.Max(Vector3.zero, areaSize);
        minimumSeparation = Mathf.Max(0f, minimumSeparation);
        placementAttempts = Mathf.Max(1, placementAttempts);
        if (pitchRange.x > pitchRange.y) (pitchRange.x, pitchRange.y) = (pitchRange.y, pitchRange.x);
        if (rollRange.x > rollRange.y) (rollRange.x, rollRange.y) = (rollRange.y, rollRange.x);
    }

    void OnDrawGizmosSelected()
    {
        Vector3 center = areaCenter != null ? areaCenter.position : fallbackCenter;
        Gizmos.color = new Color(0f, 1f, 1f, 0.35f);
        Gizmos.DrawWireCube(center, areaSize);
    }
}
