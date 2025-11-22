using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem; 

public class LaunchProjectile : MonoBehaviour
{
    public Transform launchPoint;
    public GameObject projectile;
    public float launchVelocity = 10f;

    [System.Obsolete]
    void Update()
    {
        // 🖱️ Launch with spacebar or left mouse button (for PC)
        bool desktopFire = Keyboard.current.spaceKey.wasPressedThisFrame ||
                           Mouse.current.leftButton.wasPressedThisFrame;

        // 📱 Launch with touch (for mobile)
        bool mobileFire = Touchscreen.current != null && 
                          Touchscreen.current.primaryTouch.press.wasPressedThisFrame;

        if (desktopFire || mobileFire)
        {
            var _projectile = Instantiate(projectile, launchPoint.position, launchPoint.rotation);
            _projectile.GetComponent<Rigidbody>().velocity = launchPoint.up * launchVelocity;
        }
    }
}
