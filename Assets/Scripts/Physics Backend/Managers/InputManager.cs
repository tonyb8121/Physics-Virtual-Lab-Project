using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using System;

/// <summary>
/// Handles input for AR placement.
/// Detects taps on detected planes.
/// </summary>
public class InputManager : MonoBehaviour
{
    private static InputManager _instance;
    public static InputManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("InputManager");
                _instance = go.AddComponent<InputManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    public event Action<Pose> OnTapDetected;

    private ARRaycastManager _raycastManager;
    private List<ARRaycastHit> _hits = new List<ARRaycastHit>();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        // If AR Session Manager exists, try to get the Raycast Manager from it
        if (_raycastManager == null)
        {
            _raycastManager = FindObjectOfType<ARRaycastManager>();
            return;
        }

        // Check Input
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                HandleTap(touch.position);
            }
        }
        // Mouse input for Editor testing
        else if (Input.GetMouseButtonDown(0))
        {
            HandleTap(Input.mousePosition);
        }
    }

    private void HandleTap(Vector2 screenPos)
    {
        // Block input if clicking UI
        if (UnityEngine.EventSystems.EventSystem.current != null && 
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (_raycastManager.Raycast(screenPos, _hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = _hits[0].pose;
            Logger.Log("Valid AR Tap Detected.");
            OnTapDetected?.Invoke(hitPose);
        }
    }
}