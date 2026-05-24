using UnityEngine;

public class InterferenceCollider : MonoBehaviour
{
    ECMJammer owner;
    SphereCollider sphereCollider;

    public void Initialize(ECMJammer jammer, float radius)
    {
        owner = jammer;
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        if (sphereCollider == null)
            sphereCollider = GetComponent<SphereCollider>();

        if (sphereCollider == null)
            sphereCollider = gameObject.AddComponent<SphereCollider>();

        sphereCollider.isTrigger = true;
        SetRadius(radius);
    }

    public void SetRadius(float radius)
    {
        if (sphereCollider == null)
            sphereCollider = GetComponent<SphereCollider>();

        if (sphereCollider != null)
            sphereCollider.radius = Mathf.Max(0f, radius);
    }

    void OnTriggerEnter(Collider other)
    {
        owner?.SetTargetInterference(other, true);
    }

    void OnTriggerStay(Collider other)
    {
        owner?.SetTargetInterference(other, true);
    }

    void OnTriggerExit(Collider other)
    {
        owner?.RefreshTargetInterference(other);
    }
}
