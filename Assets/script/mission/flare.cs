using UnityEngine;
public class flare : MonoBehaviour
{
    [Header("Timing")]
    public float activateDelay = 0.3f;  // 点火までの遅延
    public float lifeTime = 5f;         // 寿命（秒）
    public float decayRate = 0.8f;      // 熱減衰速度（heat/s）

    [Header("Thermal Properties")]
    public float maxHeat = 1.0f;        // 最大熱量
    public float currentHeat = 0f;      // 現在熱量


    private bool isActivated = false;

    public Vector3 velocity;

    private void Awake()
    {
        // 初期化処理
        ObjectManager.Instance.RegisterFlare(gameObject);
    }


    private void OnEnable()
    {
        // 状態初期化
        isActivated = false;
        currentHeat = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        velocity *= 0.95f;
        velocity.y -= 0.1f; // 重力の影響を追加
        transform.position += velocity * Time.deltaTime;

        // --- 点火処理 ---
        if (!isActivated)
        {
            activateDelay -= Time.deltaTime;
            if (activateDelay <= 0f)
            {
                isActivated = true;
                currentHeat = maxHeat; // 点火直後に最大熱
            }
        }
        else
        {
            // --- 減衰処理 ---
            currentHeat -= decayRate * Time.deltaTime;
            currentHeat = Mathf.Max(currentHeat, 0f);
        }

        // --- 寿命管理 ---
        lifeTime -= Time.deltaTime;
        if (lifeTime <= 0f)
        {
            ObjectManager.Instance?.UnregisterFlare(gameObject);
            gameObject.SetActive(false);
            Destroy(gameObject, 1f);
        }
    }
}
