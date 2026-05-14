using UnityEngine;

public class UAVStorage : MonoBehaviour
{
    public float launchDelay = 0.5f;

    M02DesignController controller;
    float timer;
    bool launched;

    public void Initialize(M02DesignController owner)
    {
        controller = owner;
        timer = launchDelay;
    }

    void Update()
    {
        if (launched) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        launched = true;
        controller.LaunchFighters(transform.position, transform.rotation);
    }
}
