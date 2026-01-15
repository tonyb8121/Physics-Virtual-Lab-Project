using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Static physics calculator using standard kinematic equations.
/// Used for verifying simulations against theoretical values.
/// </summary>
public static class PhysicsEngineManager
{
    public const float GRAVITY = 9.81f; // Standard Earth gravity

    #region Projectile Motion
    
    // Range R = (v^2 * sin(2θ)) / g
    public static float CalculateProjectileRange(float velocity, float angleDeg)
    {
        float angleRad = angleDeg * Mathf.Deg2Rad;
        return (Mathf.Pow(velocity, 2) * Mathf.Sin(2 * angleRad)) / GRAVITY;
    }

    // Max Height H = (v * sin(θ))^2 / (2g)
    public static float CalculateMaxHeight(float velocity, float angleDeg)
    {
        float angleRad = angleDeg * Mathf.Deg2Rad;
        float v_y = velocity * Mathf.Sin(angleRad);
        return Mathf.Pow(v_y, 2) / (2 * GRAVITY);
    }

    // Flight Time T = (2 * v * sin(θ)) / g
    public static float CalculateFlightTime(float velocity, float angleDeg)
    {
        float angleRad = angleDeg * Mathf.Deg2Rad;
        return (2 * velocity * Mathf.Sin(angleRad)) / GRAVITY;
    }

    /// <summary>
    /// Generates points for a trajectory line renderer.
    /// </summary>
    public static Vector3[] CalculateTrajectoryPoints(Vector3 startPos, float velocity, float angleDeg, int steps = 50)
    {
        float totalTime = CalculateFlightTime(velocity, angleDeg);
        if (totalTime <= 0) return new Vector3[] { startPos };

        Vector3[] points = new Vector3[steps];
        float angleRad = angleDeg * Mathf.Deg2Rad;
        
        // Initial velocity components
        float v_x = velocity * Mathf.Cos(angleRad);
        float v_y = velocity * Mathf.Sin(angleRad);

        float timeStep = totalTime / (steps - 1);

        for (int i = 0; i < steps; i++)
        {
            float t = i * timeStep;
            
            // x = x0 + vx * t
            // y = y0 + vy * t - 0.5 * g * t^2
            
            float x = v_x * t;
            float y = (v_y * t) - (0.5f * GRAVITY * t * t);

            // In AR, Y is up, Z is forward. We map 2D physics to 3D space.
            // Assuming launch direction is forward (Z).
            Vector3 point = startPos + new Vector3(0, y, x);
            points[i] = point;
        }

        return points;
    }
    #endregion

    #region Pendulum
    
    // Period T = 2π * sqrt(L/g)
    public static float CalculatePendulumPeriod(float length)
    {
        if (length <= 0) return 0;
        return 2 * Mathf.PI * Mathf.Sqrt(length / GRAVITY);
    }
    
    // Potential Energy PE = m * g * h
    public static float CalculatePotentialEnergy(float mass, float heightFromBottom)
    {
        return mass * GRAVITY * heightFromBottom;
    }

    // Kinetic Energy KE = 0.5 * m * v^2
    public static float CalculateKineticEnergy(float mass, float velocity)
    {
        return 0.5f * mass * velocity * velocity;
    }
    #endregion
}