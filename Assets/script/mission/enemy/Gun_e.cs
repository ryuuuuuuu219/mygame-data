using System.Collections.Generic;
using UnityEngine;

public class Gun_e : MonoBehaviour
{
    public float lifeTime = 3f;         // 自爆までの秒数
    List<GameObject> allies;

    private Vector3 previousPos;
    private Vector3 currentPos;
    public Vector3 velocity;

    private void OnEnable()
    {
        previousPos = transform.position;
        currentPos = transform.position;
    }

    private void FixedUpdate()
    {
        lifeTime -= Time.deltaTime;
        if (lifeTime <= 0f)
        {
            gameObject.SetActive(false);
        }

        previousPos = transform.position;
        transform.position += velocity * Time.fixedDeltaTime;
        currentPos = transform.position;

        if (HitTerrainBetweenPreviousAndCurrent())
        {
            gameObject.SetActive(false);
            return;
        }

        // Raycastで途中の衝突をチェック
        Vector3 dir = (currentPos - previousPos).normalized;
        float dist = Vector3.Distance(previousPos, currentPos);

        allies = ObjectManager.Instance.allies;
        foreach (GameObject ally in allies)
        {
            if (ally == null) continue;
            Vector3 allyPos = ally.transform.position;
            float radius = 0.5f * (transform.localScale.x + ally.transform.localScale.x);

            // 弾道と対象の位置の最近点を求める
            Vector3 closestPoint = previousPos +
                Vector3.Project(allyPos - previousPos, dir);

            if (Vector3.Distance(closestPoint, allyPos) < radius &&
                Vector3.Dot(allyPos - previousPos, dir) > 0 &&
                Vector3.Distance(previousPos, allyPos) <= dist)
            {
                // ヒット処理
                var status = ally.GetComponent<AugumentStatus>();
                if (status != null && !status.isEnemy)
                {
                    status.damage(10f); // ダメージ量は適宜調整
                }

                gameObject.SetActive(false);
                return;
            }
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
