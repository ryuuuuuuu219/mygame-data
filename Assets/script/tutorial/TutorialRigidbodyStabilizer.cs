using UnityEngine;

public class TutorialRigidbodyStabilizer : MonoBehaviour
{
    public bool makeKinematic;
    public bool freezeRotation;
    public bool zeroAngularVelocity = true;
    public float mass = 0f;

    Rigidbody rb;

    public static void Configure(Rigidbody rb, bool makeKinematic, bool freezeRotation)
    {
        if (rb == null) return;

        rb.useGravity = false;
        rb.mass = 0f;
        rb.isKinematic = makeKinematic;
        rb.detectCollisions = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        rb.constraints = freezeRotation ? RigidbodyConstraints.FreezeRotation : RigidbodyConstraints.None;
        rb.angularVelocity = Vector3.zero;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        Apply();
    }

    void Start()
    {
        Apply();
    }

    void FixedUpdate()
    {
        Apply();
    }

    void LateUpdate()
    {
        Apply();
    }

    void Apply()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();
        if (rb == null) return;

        Configure(rb, makeKinematic, freezeRotation);
        rb.mass = mass;
        if (zeroAngularVelocity)
            rb.angularVelocity = Vector3.zero;
    }
}
