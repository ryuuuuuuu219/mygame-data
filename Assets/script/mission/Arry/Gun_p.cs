using System.Collections.Generic;
using UnityEngine;

public class Gun_p : MonoBehaviour
{
    [Header("弾の寿命設定")]
    public float lifeTime = 3f;         // 弾の生存時間（秒）

    [Header("内部パラメータ")]
    public Vector3 velocity;            // 弾の移動速度
    private Vector3 previousPos;
    private Vector3 currentPos;

    public float power = 10f;
    public float size = 1f;

    private List<GameObject> enemys;

    private void OnEnable()
    {
        previousPos = transform.position;
        currentPos = transform.position;
    }

    public void Init(float Power, float Size)
    {
        this.power = Power;
        this.size = Size;

        transform.GetComponent<BoxCollider>().size = new Vector3(size, size, size);
    }

    private void FixedUpdate()
    {
        lifeTime -= Time.deltaTime;
        if (lifeTime <= 0f)
        {
            gameObject.SetActive(false);
            return;
        }

        previousPos = transform.position;
        transform.position += velocity * Time.fixedDeltaTime;
        currentPos = transform.position;

        if (HitTerrainBetweenPreviousAndCurrent())
        {
            gameObject.SetActive(false);
            return;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other is TerrainCollider)
        {
            gameObject.SetActive(false);
            return;
        }

        var status = other.GetComponent<AugumentStatus>();
        if (status != null && status.isEnemy)
        {
            status.damage(power);
            ObjectManager.Instance.hitUIflag = true;
        gameObject.SetActive(false);
        }
    }

    private bool HitTerrainBetweenPreviousAndCurrent()
    {
        Vector3 delta = currentPos - previousPos;
        float distance = delta.magnitude;
        if (distance <= 0.0001f) return false;

        return Physics.Raycast(
            previousPos,
            delta / distance,
            out RaycastHit hit,
            distance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore) &&
            hit.collider is TerrainCollider;
    }

}
