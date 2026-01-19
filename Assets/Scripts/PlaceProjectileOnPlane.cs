using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.EventSystems;

public class PlaceProjectileOnPlane : MonoBehaviour
{
    [Header("References")]
    public GameObject projectilePrefab;       // Combined prefab (Cannon + Cliff)
    public ARRaycastManager raycastManager;

    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private GameObject spawnedObject;
    private bool isSelected = false;

    // Manipulation
    private float initialDistance;
    private Vector3 initialScale;
    private Vector2 lastTouchPos0;
    private Vector2 lastTouchPos1;

    private Camera cam;

    void Start()
    {
        cam = Camera.main;
    }

    void Update()
    {
        HandleSelection();
        HandlePlacement();
        HandleManipulation();
    }

    // Helper method to check if touch/mouse is over UI
    bool IsPointerOverUI(Vector2 screenPosition)
    {
        // Check if EventSystem exists
        if (EventSystem.current == null)
            return false;

        // Create pointer event data
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = screenPosition;

        // Raycast to check for UI hits
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        // Return true if we hit any UI elements
        return results.Count > 0;
    }

    void HandleSelection()
    {
        if (spawnedObject == null) return;

        // Phone - check if touch is over UI first
        if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            // Ignore if touching UI
            if (IsPointerOverUI(Input.GetTouch(0).position))
            {
                isSelected = false;
                return;
            }

            Ray r = cam.ScreenPointToRay(Input.GetTouch(0).position);
            if (Physics.Raycast(r, out RaycastHit hit))
            {
                if (hit.transform.gameObject == spawnedObject || 
                    hit.transform.IsChildOf(spawnedObject.transform))
                {
                    isSelected = true;
                    return;
                }
            }
            isSelected = false;
        }

        // Mouse - check if over UI first
        if (Input.GetMouseButtonDown(0))
        {
            // Ignore if over UI
            if (IsPointerOverUI(Input.mousePosition))
            {
                isSelected = false;
                return;
            }

            Ray r = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(r, out RaycastHit hit))
            {
                if (hit.transform.gameObject == spawnedObject ||
                    hit.transform.IsChildOf(spawnedObject.transform))
                {
                    isSelected = true;
                    return;
                }
            }
            isSelected = false;
        }
    }

    void HandlePlacement()
    {
        if (isSelected) return;

        // Phone tap - ignore if over UI
        if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            if (!IsPointerOverUI(Input.GetTouch(0).position))
                TryPlaceObject(Input.GetTouch(0).position);
        }

        // Mouse click - ignore if over UI
        if (Input.GetMouseButtonDown(0))
        {
            if (!IsPointerOverUI(Input.mousePosition))
                TryPlaceObject(Input.mousePosition);
        }
    }

    void TryPlaceObject(Vector2 screenPos)
    {
        if (raycastManager.Raycast(screenPos, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = hits[0].pose;

            if (spawnedObject == null)
            {
                // Spawn the combined prefab
                spawnedObject = Instantiate(projectilePrefab, hitPose.position, hitPose.rotation);

                // Find the UI Binder
                ProjectileUIBinder binder = FindFirstObjectByType<ProjectileUIBinder>();
                
                if (binder != null)
                {
                    // Connect Cannon/Launcher
                    ProjectileLauncher launcher = spawnedObject.GetComponentInChildren<ProjectileLauncher>();
                    if (launcher != null)
                    {
                        binder.BindLauncherAtRuntime(launcher);
                        Debug.Log("✓ Cannon connected to UI!");
                    }
                    else
                    {
                        Debug.LogWarning("⚠️ ProjectileLauncher not found in prefab!");
                    }

                    // Connect Cliff Controller
                    CliffController cliff = spawnedObject.GetComponentInChildren<CliffController>();
                    if (cliff != null)
                    {
                        binder.BindCliffController(cliff);
                        Debug.Log("✓ Cliff connected to UI!");
                    }
                    else
                    {
                        Debug.LogWarning("⚠️ CliffController not found in prefab!");
                    }
                }
                else
                {
                    Debug.LogError("⚠️ ProjectileUIBinder not found in scene!");
                }
            }
            else if (!isSelected)
            {
                spawnedObject.transform.SetPositionAndRotation(hitPose.position, hitPose.rotation);
            }
        }
    }

    void HandleManipulation()
    {
        if (spawnedObject == null || !isSelected) return;

        // PHONE MULTITOUCH
        if (Input.touchCount == 1)
        {
            Touch t = Input.GetTouch(0);
            
            // Ignore if over UI
            if (IsPointerOverUI(t.position))
                return;
            
            if (t.phase == TouchPhase.Moved)
                MoveObject(t.deltaPosition * 0.0015f);
        }
        else if (Input.touchCount == 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            // Ignore if either touch is over UI
            if (IsPointerOverUI(t0.position) || IsPointerOverUI(t1.position))
                return;

            if (t0.phase == TouchPhase.Began || t1.phase == TouchPhase.Began)
            {
                initialDistance = Vector2.Distance(t0.position, t1.position);
                initialScale = spawnedObject.transform.localScale;
            }
            else
            {
                float currDist = Vector2.Distance(t0.position, t1.position);
                float scaleFactor = currDist / initialDistance;
                spawnedObject.transform.localScale = initialScale * scaleFactor;

                float rotationDelta =
                    Vector2.SignedAngle(t1.position - lastTouchPos1, t1.position - t0.position);
                spawnedObject.transform.Rotate(Vector3.up, rotationDelta);
            }

            lastTouchPos0 = t0.position;
            lastTouchPos1 = t1.position;
        }

        // MOUSE / PC CONTROLS
        // Ignore mouse manipulation if over UI
        if (IsPointerOverUI(Input.mousePosition))
            return;

        if (Input.GetMouseButton(0))
        {
            Vector3 move = new Vector3(Input.GetAxis("Mouse X"), 0, Input.GetAxis("Mouse Y"));
            MoveObject(move * 0.05f);
        }

        if (Input.GetMouseButton(1))
        {
            float rotX = Input.GetAxis("Mouse X") * 5f;
            spawnedObject.transform.Rotate(Vector3.up, rotX);
        }

        if (Input.mouseScrollDelta.y != 0)
        {
            Vector3 direction = cam.transform.forward;
            spawnedObject.transform.position += direction * Input.mouseScrollDelta.y * 0.1f;
        }

        if (Input.GetKey(KeyCode.LeftShift) && Input.GetMouseButton(0))
        {
            float delta = Input.GetAxis("Mouse Y") * 0.01f;
            spawnedObject.transform.localScale += Vector3.one * delta;
        }
    }

    void MoveObject(Vector2 delta)
    {
        Vector3 move = cam.transform.right * delta.x + cam.transform.up * delta.y;
        spawnedObject.transform.position += move;
    }
}