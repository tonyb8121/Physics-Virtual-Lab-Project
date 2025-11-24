using UnityEngine;

public class Pendulum3D : MonoBehaviour
{
    [Header("Pendulum Settings")]
    public float gravity = 9.81f;
    public float damping = 0.9999f;

    [Header("References")]
    public Transform stringCylinder;   // Assign Cylinder here
    public Transform bob;              // Assign Sphere here

    [Header("Oscillation Tracking")]
    public int oscillationCount { get; private set; } = 0;
    public float timer { get; private set; } = 0f;
    public bool isRunning { get; private set; } = false;      // physics running
    public bool isRunningUI { get; private set; } = false;    // UI timer & oscillations

    private float angle;
    private float angularVelocity;
    private float angularAcceleration;

    private float configuredAngle;
    private float configuredLength;

    private Transform pivot;
    private float lastAngle = 0f; // to detect equilibrium crossing

    void Start()
    {
        pivot = transform.parent;      // Pendulum object is a child of the pivot
        Configure(20f, 2f);            // Default values
        lastAngle = angle;
    }

    void FixedUpdate()
    {
        if (isRunning)
        {
            // Physics for angular motion
            angularAcceleration = (-gravity / configuredLength) * Mathf.Sin(angle);
            angularVelocity += angularAcceleration * Time.fixedDeltaTime;
            angularVelocity *= damping;
            angle += angularVelocity * Time.fixedDeltaTime;

            UpdatePendulum();
        }

        if (isRunningUI)
        {
            // Increment timer
            timer += Time.fixedDeltaTime;

            // Count oscillations when crossing equilibrium in positive direction
            if (angle > 0 && lastAngle <= 0)
            {
                oscillationCount++;
            }

            lastAngle = angle;
        }
    }

    void UpdatePendulum()
    {
        // Rotate entire pendulum (string + bob)
        transform.localRotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg);

        // ---------------- STRING ----------------
        if (stringCylinder != null)
        {
            // Height is half the length because Unity cylinders scale from center
            stringCylinder.localScale = new Vector3(
                stringCylinder.localScale.x,
                configuredLength / 2f,
                stringCylinder.localScale.z
            );

            // Move so top touches pivot
            stringCylinder.localPosition = new Vector3(
                0f,
                -configuredLength / 2f,
                0f
            );
        }

        // ---------------- BOB ----------------
        if (bob != null)
        {
            // Compute real radius based on scaling
            float bobRadius = bob.localScale.y * -0.46f;

            // Position bob at end of string, adjusted by radius
            bob.localPosition = new Vector3(
                0f,
                -configuredLength - bobRadius + 1.5f,
                0f
            );
        }
    }

    // ---------------- PUBLIC API ----------------
    public void Configure(float angleDeg, float length)
    {
        configuredAngle = angleDeg;
        configuredLength = Mathf.Max(0.1f, length);

        angle = configuredAngle * Mathf.Deg2Rad;
        angularVelocity = 0f;
        angularAcceleration = 0f;

        UpdatePendulum();
        lastAngle = angle;
    }

    public void Play()
    {
        isRunning = true;
        isRunningUI = true;
    }

    public void Stop()
    {
        isRunning = false;
        isRunningUI = false;
        angularVelocity = 0f;
        // Optional: keep current angle to display timer/oscillations
    }

public void Reset()
{
    isRunning = false;
    isRunningUI = false;

    timer = 0f;
    oscillationCount = 0;

    angularVelocity = 0f;

    // Reset pendulum to initial set angle & length
    angle = configuredAngle * Mathf.Deg2Rad;
    lastAngle = angle;

    UpdatePendulum();
}

}
