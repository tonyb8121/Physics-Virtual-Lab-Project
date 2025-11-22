using UnityEngine;

public class Pendulum3D : MonoBehaviour
{
    [Header("Pendulum Settings")]
    public float gravity = 9.81f;
    public float damping = 0.995f;

    [Header("References")]
    public Transform stringCylinder;  // 👈 assign your Cylinder here in Inspector

    private float angle;
    private float angularVelocity = 0f;
    private float angularAcceleration = 0f;

    private Transform pivot;
    private bool isRunning = false;

    private float configuredAngle;
    private float configuredLength;

    void Start()
    {
        pivot = transform.parent;
        Configure(0f, 2f); // default
    }

    void FixedUpdate()
    {
        if (!isRunning) return;

        angularAcceleration = (-gravity / configuredLength) * Mathf.Sin(angle);
        angularVelocity += angularAcceleration * Time.deltaTime;
        angularVelocity *= damping;
        angle += angularVelocity * Time.deltaTime;

        UpdateBobPosition();
    }

    void UpdateBobPosition()
    {
        // Bob local position relative to pivot
        transform.localPosition = new Vector3(configuredLength * Mathf.Sin(angle), -configuredLength * Mathf.Cos(angle), 0f);

        // Update cylinder scale + position
        if (stringCylinder != null)
        {
            stringCylinder.localScale = new Vector3(
                stringCylinder.localScale.x,
                configuredLength / 2f,   // Y-scale = half the length (because Unity’s primitive cylinders are 2 units tall by default)
                stringCylinder.localScale.z
            );

            stringCylinder.localPosition = new Vector3(0f, -configuredLength / 2f, 0f);
        }
    }

    // ---------------- PUBLIC API ----------------

    public void Configure(float angleDeg, float len)
    {
        configuredAngle = angleDeg;
        configuredLength = Mathf.Max(0.1f, len);

        angle = configuredAngle * Mathf.Deg2Rad;
        angularVelocity = 0f;
        angularAcceleration = 0f;

        UpdateBobPosition();
    }

    public void Play()
    {
        isRunning = true;
    }

    public void Stop()
    {
        isRunning = false;
        angularVelocity = 0f;
        angularAcceleration = 0f;

        Configure(configuredAngle, configuredLength);
    }
}
