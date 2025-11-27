using UnityEngine;

public static class ProjectileCalculator
{
    // angle in degrees, speed scalar (m/s), gravity is positive magnitude (e.g. 9.81)
    // returns time of flight (s), max height (m), range (m)
    public static float TimeOfFlight(float angleDeg, float speed, float gravity)
    {
        float angleRad = angleDeg * Mathf.Deg2Rad;
        float vy = speed * Mathf.Sin(angleRad);
        return (2f * vy) / gravity;
    }

    public static float MaxHeight(float angleDeg, float speed, float gravity)
    {
        float angleRad = angleDeg * Mathf.Deg2Rad;
        float vy = speed * Mathf.Sin(angleRad);
        return (vy * vy) / (2f * gravity);
    }

    public static float Range(float angleDeg, float speed, float gravity)
    {
        float angleRad = angleDeg * Mathf.Deg2Rad;
        float vx = speed * Mathf.Cos(angleRad);
        float vy = speed * Mathf.Sin(angleRad);
        float T = (2f * vy) / gravity;
        return vx * T;
    }

    public static float ImpactSpeed(float angleDeg, float speed, float gravity)
    {
        float angleRad = angleDeg * Mathf.Deg2Rad;
        float vx = speed * Mathf.Cos(angleRad);
        float vy = speed * Mathf.Sin(angleRad);
        return Mathf.Sqrt(vx * vx + vy * vy);
    }
}
