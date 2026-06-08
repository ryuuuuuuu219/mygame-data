using UnityEngine;

public class TutorialTextYawToPlayer : MonoBehaviour
{
    public Transform player;

    void LateUpdate()
    {
        if (player == null)
            return;

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;

        if (toPlayer.sqrMagnitude <= 0.001f)
            return;

        transform.rotation = Quaternion.LookRotation(toPlayer.normalized, Vector3.up) *
                             Quaternion.Euler(0f, 180f, 0f);
    }
}
