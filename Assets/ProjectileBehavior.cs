using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody))]
public class ProjectileBehavior : MonoBehaviour
{
    public UnityEvent onLanded;            // hook this in the inspector or from code
    public string groundTag = "Barrier";    // tag for ground/collision
    public float sleepVelocityThreshold = 0.2f;
    public float checkSleepDelay = 0.25f;
    bool hasLanded = false;
    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasLanded) return;

        // If collided with ground-tagged object, immediate land
        if (!string.IsNullOrEmpty(groundTag) && collision.collider.CompareTag(groundTag))
        {
            Land();
            return;
        }

        // Otherwise start checking for restful state
        if (!hasLanded)
            Invoke(nameof(CheckIfSleeping), checkSleepDelay);
    }

    void CheckIfSleeping()
    {
        if (hasLanded) return;
        if (rb == null) return;

        if (rb.IsSleeping() || rb.linearVelocity.magnitude <= sleepVelocityThreshold)
        {
            Land();
        }
    }

    void Land()
    {
        if (hasLanded) return;
        hasLanded = true;
        Debug.Log("ProjectileBehavior: Landed.");
        onLanded?.Invoke();
    }
}
