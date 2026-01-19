using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.EventSystems;

public class PlaceOnPlane : MonoBehaviour
{
    [Header("References")]
    public GameObject objectToPlace;
    public ARRaycastManager raycastManager;
    public ARPlaneManager planeManager;

    [Header("AR Plane Settings")]
    public bool stopDetectionAfterFirstPlane = true;
    public bool disablePlanesAfterPlacement = true;
    
    [Header("Placement Preview")]
    public bool showPreview = true;
    public Color previewColor = new Color(1f, 1f, 1f, 0.5f);
    public float previewScale = 1f;

    [Header("Spinning Placement Indicator")]
    public bool showSpinningIndicator = true;
    public float indicatorSize = 0.3f;
    public float spinSpeed = 90f;
    public Color indicatorColor = new Color(0f, 1f, 1f, 0.8f);
    public float pulseSpeed = 2f;
    public float pulseAmount = 0.2f;

    [Header("Scale Limits")]
    public float minScale = 0.05f;
    public float maxScale = 0.2f;

    [Header("Lock/Anchor Settings")]
    public KeyCode lockKey = KeyCode.L;
    public bool showLockStatus = true;

    [Header("Tutorial Settings")]
    public bool enableTutorial = true;
    public Color tutorialColor = new Color(1f, 1f, 1f, 0.9f);

    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private GameObject spawnedObject;
    private GameObject previewObject;
    private GameObject spinningIndicator;
    private bool isSelected = false;
    private bool isLocked = false;

    // Manipulation
    private float initialDistance;
    private Vector3 initialScale;
    private Vector2 lastTouchPos0;
    private Vector2 lastTouchPos1;

    // For triple-tap detection
    private float lastTapTime = 0f;
    private int tapCount = 0;
    private const float tripleTapTimeWindow = 0.5f;

    // UI Notification
    private bool showLockNotification = false;
    private float notificationTimer = 0f;
    private const float notificationDuration = 15f;
    private float notificationAlpha = 1f;

    // Tutorial System
    private enum TutorialState { ScanSurfaces, TapToPlace, Complete }
    private TutorialState tutorialState = TutorialState.ScanSurfaces;
    private bool tutorialComplete = false;
    private float tutorialAnimTime = 0f;
    private bool planesDetected = false;
    private bool firstPlaneDetected = false;

    private Camera cam;

    void Start()
    {
        cam = Camera.main;
        CreatePreviewObject();
        CreateSpinningIndicator();
        
        // Subscribe to plane events using NEW AR Foundation 6.0 API
        if (planeManager != null && stopDetectionAfterFirstPlane)
        {
            planeManager.trackablesChanged.AddListener(OnPlanesChanged);
        }
    }

    void Update()
    {
        HandleLockToggle();
        UpdateLockNotification();
        UpdateTutorial();
        
        if (!isLocked)
        {
            HandleSelection();
            UpdatePreview();
            UpdateSpinningIndicator();
            HandlePlacement();
            HandleManipulation();
        }
        else
        {
            if (previewObject != null)
                previewObject.SetActive(false);
            if (spinningIndicator != null)
                spinningIndicator.SetActive(false);
        }
    }

    // ----------------------------
    // AR PLANE MANAGEMENT (UPDATED FOR AR FOUNDATION 6.0)
    // ----------------------------
    void OnPlanesChanged(ARTrackablesChangedEventArgs<ARPlane> args)
    {
        if (stopDetectionAfterFirstPlane && !firstPlaneDetected && args.added.Count > 0)
        {
            firstPlaneDetected = true;
            planeManager.enabled = false;
            Debug.Log("First plane detected - stopping plane detection");
        }
    }

    void DisableAllPlanes()
    {
        if (planeManager == null) return;

        foreach (var plane in planeManager.trackables)
        {
            plane.gameObject.SetActive(false);
        }
        
        Debug.Log("All AR planes disabled");
    }

    void AddCollidersToPlanes()
    {
        if (planeManager == null) return;

        foreach (var plane in planeManager.trackables)
        {
            MeshCollider meshCollider = plane.GetComponent<MeshCollider>();
            if (meshCollider == null)
            {
                meshCollider = plane.gameObject.AddComponent<MeshCollider>();
            }
            
            MeshFilter meshFilter = plane.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.mesh != null)
            {
                meshCollider.sharedMesh = meshFilter.mesh;
            }
        }
        
        Debug.Log("Colliders added to AR planes");
    }

    // ----------------------------
    // CREATE SPINNING INDICATOR
    // ----------------------------
    void CreateSpinningIndicator()
    {
        if (!showSpinningIndicator) return;

        spinningIndicator = new GameObject("SpinningPlacementIndicator");
        
        GameObject outerRing = CreateRing(spinningIndicator.transform, indicatorSize, 0.02f, 32);
        
        GameObject crosshair = new GameObject("Crosshair");
        crosshair.transform.SetParent(spinningIndicator.transform);
        crosshair.transform.localPosition = Vector3.zero;
        
        CreateLine(crosshair.transform, new Vector3(-indicatorSize/2, 0, 0), new Vector3(indicatorSize/2, 0, 0));
        CreateLine(crosshair.transform, new Vector3(0, 0, -indicatorSize/2), new Vector3(0, 0, indicatorSize/2));
        
        float cornerDist = indicatorSize * 0.35f;
        float cornerSize = indicatorSize * 0.1f;
        CreateCornerMarker(spinningIndicator.transform, new Vector3(cornerDist, 0, cornerDist), cornerSize);
        CreateCornerMarker(spinningIndicator.transform, new Vector3(-cornerDist, 0, cornerDist), cornerSize);
        CreateCornerMarker(spinningIndicator.transform, new Vector3(cornerDist, 0, -cornerDist), cornerSize);
        CreateCornerMarker(spinningIndicator.transform, new Vector3(-cornerDist, 0, -cornerDist), cornerSize);
        
        spinningIndicator.SetActive(false);
    }

    GameObject CreateRing(Transform parent, float radius, float thickness, int segments)
    {
        GameObject ring = new GameObject("Ring");
        ring.transform.SetParent(parent);
        ring.transform.localPosition = Vector3.zero;
        
        LineRenderer lr = ring.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = indicatorColor;
        lr.endColor = indicatorColor;
        lr.startWidth = thickness;
        lr.endWidth = thickness;
        lr.positionCount = segments + 1;
        lr.useWorldSpace = false;
        
        for (int i = 0; i <= segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            lr.SetPosition(i, new Vector3(x, 0, z));
        }
        
        return ring;
    }

    void CreateLine(Transform parent, Vector3 start, Vector3 end)
    {
        GameObject line = new GameObject("Line");
        line.transform.SetParent(parent);
        line.transform.localPosition = Vector3.zero;
        
        LineRenderer lr = line.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = indicatorColor;
        lr.endColor = indicatorColor;
        lr.startWidth = 0.015f;
        lr.endWidth = 0.015f;
        lr.positionCount = 2;
        lr.useWorldSpace = false;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
    }

    void CreateCornerMarker(Transform parent, Vector3 position, float size)
    {
        GameObject marker = new GameObject("CornerMarker");
        marker.transform.SetParent(parent);
        marker.transform.localPosition = position;
        
        LineRenderer lr = marker.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = indicatorColor;
        lr.endColor = indicatorColor;
        lr.startWidth = 0.02f;
        lr.endWidth = 0.02f;
        lr.positionCount = 5;
        lr.useWorldSpace = false;
        
        lr.SetPosition(0, new Vector3(-size, 0, 0));
        lr.SetPosition(1, new Vector3(0, 0, 0));
        lr.SetPosition(2, new Vector3(0, 0, -size));
        lr.SetPosition(3, new Vector3(0, 0, 0));
        lr.SetPosition(4, new Vector3(0, 0, 0));
    }

    // ----------------------------
    // UPDATE SPINNING INDICATOR
    // ----------------------------
    void UpdateSpinningIndicator()
    {
        if (!showSpinningIndicator || spinningIndicator == null || spawnedObject != null)
        {
            if (spinningIndicator != null)
                spinningIndicator.SetActive(false);
            return;
        }

        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Vector2 targetPosition = screenCenter;
        
        if (Input.touchCount > 0)
        {
            targetPosition = Input.GetTouch(0).position;
        }
        else if (Application.isEditor || Application.platform == RuntimePlatform.WindowsPlayer)
        {
            targetPosition = Input.mousePosition;
        }

        if (raycastManager.Raycast(targetPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = hits[0].pose;
            
            spinningIndicator.SetActive(true);
            spinningIndicator.transform.position = hitPose.position;
            spinningIndicator.transform.rotation = hitPose.rotation;
            
            Transform crosshair = spinningIndicator.transform.Find("Crosshair");
            if (crosshair != null)
            {
                crosshair.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.Self);
            }
            
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            spinningIndicator.transform.localScale = Vector3.one * pulse;
            
            foreach (Transform child in spinningIndicator.transform)
            {
                if (child.name == "CornerMarker")
                {
                    child.Rotate(Vector3.up, -spinSpeed * 0.5f * Time.deltaTime, Space.Self);
                }
            }
        }
        else
        {
            spinningIndicator.SetActive(false);
        }
    }

    // ----------------------------
    // CREATE PREVIEW OBJECT
    // ----------------------------
    void CreatePreviewObject()
    {
        if (objectToPlace == null || !showPreview) return;

        previewObject = Instantiate(objectToPlace);
        previewObject.name = "Preview_" + objectToPlace.name;
        
        MakeTransparent(previewObject, previewColor);
        DisablePhysics(previewObject);
        
        previewObject.SetActive(false);
    }

    void MakeTransparent(GameObject obj, Color color)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer rend in renderers)
        {
            Material[] mats = rend.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                Material mat = new Material(mats[i]);
                
                mat.SetFloat("_Mode", 3);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
                
                mat.color = color;
                
                mats[i] = mat;
            }
            rend.materials = mats;
        }
    }

    void DisablePhysics(GameObject obj)
    {
        Collider[] colliders = obj.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }

        Rigidbody[] rigidbodies = obj.GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody rb in rigidbodies)
        {
            rb.isKinematic = true;
        }

        MonoBehaviour[] scripts = obj.GetComponentsInChildren<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts)
        {
            if (script.GetType().Name.Contains("Pendulum") || 
                script.GetType().Name.Contains("Projectile"))
            {
                script.enabled = false;
            }
        }
    }

    // ----------------------------
    // UPDATE PREVIEW POSITION
    // ----------------------------
    void UpdatePreview()
    {
        if (!showPreview || previewObject == null || spawnedObject != null) 
        {
            if (previewObject != null)
                previewObject.SetActive(false);
            return;
        }

        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Vector2 previewPosition = screenCenter;
        
        if (Input.touchCount > 0)
        {
            previewPosition = Input.GetTouch(0).position;
        }
        else if (Application.isEditor || Application.platform == RuntimePlatform.WindowsPlayer)
        {
            previewPosition = Input.mousePosition;
        }

        if (raycastManager.Raycast(previewPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = hits[0].pose;
            
            previewObject.SetActive(true);
            previewObject.transform.SetPositionAndRotation(hitPose.position, hitPose.rotation);
            previewObject.transform.localScale = objectToPlace.transform.localScale * previewScale;
        }
        else
        {
            previewObject.SetActive(false);
        }
    }

    // ----------------------------
    // TUTORIAL SYSTEM
    // ----------------------------
    void UpdateTutorial()
    {
        if (!enableTutorial || tutorialComplete) return;

        tutorialAnimTime += Time.deltaTime;

        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        planesDetected = raycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon);

        if (tutorialState == TutorialState.ScanSurfaces && planesDetected)
        {
            tutorialState = TutorialState.TapToPlace;
            tutorialAnimTime = 0f;
        }

        if (tutorialState == TutorialState.TapToPlace && spawnedObject != null)
        {
            tutorialState = TutorialState.Complete;
            tutorialComplete = true;
        }
    }

    void DrawRotatingArrows(float centerX, float centerY, float radius, float arrowSize)
    {
        for (int i = 0; i < 4; i++)
        {
            float angle = (tutorialAnimTime * 60f + i * 90f) * Mathf.Deg2Rad;
            float x = centerX + Mathf.Cos(angle) * radius;
            float y = centerY + Mathf.Sin(angle) * radius;
            
            GUIStyle arrowStyle = new GUIStyle(GUI.skin.label);
            arrowStyle.fontSize = (int)arrowSize;
            arrowStyle.alignment = TextAnchor.MiddleCenter;
            arrowStyle.normal.textColor = tutorialColor;
            
            GUI.Label(new Rect(x - arrowSize/2, y - arrowSize/2, arrowSize, arrowSize), "→", arrowStyle);
        }
    }

    void DrawPulsingCircle(float centerX, float centerY, float baseSize)
    {
        float pulse = 1f + Mathf.Sin(tutorialAnimTime * 3f) * 0.3f;
        float size = baseSize * pulse;
        
        GUIStyle circleStyle = new GUIStyle(GUI.skin.label);
        circleStyle.fontSize = (int)size;
        circleStyle.alignment = TextAnchor.MiddleCenter;
        circleStyle.normal.textColor = new Color(tutorialColor.r, tutorialColor.g, tutorialColor.b, 
                                                  tutorialColor.a * (0.5f + 0.5f * Mathf.Sin(tutorialAnimTime * 3f)));
        
        GUI.Label(new Rect(centerX - size/2, centerY - size/2, size, size), "○", circleStyle);
    }

    void DrawTapIndicator(float centerX, float centerY)
    {
        float bounce = Mathf.Abs(Mathf.Sin(tutorialAnimTime * 2f)) * 20f;
        
        GUIStyle handStyle = new GUIStyle(GUI.skin.label);
        handStyle.fontSize = 60;
        handStyle.alignment = TextAnchor.MiddleCenter;
        handStyle.normal.textColor = tutorialColor;
        
        GUI.Label(new Rect(centerX - 40, centerY - 40 - bounce, 80, 80), "👆", handStyle);
        
        float rippleSize = (tutorialAnimTime * 100f) % 100f;
        float rippleAlpha = 1f - (rippleSize / 100f);
        
        GUIStyle rippleStyle = new GUIStyle(GUI.skin.label);
        rippleStyle.fontSize = (int)rippleSize;
        rippleStyle.alignment = TextAnchor.MiddleCenter;
        rippleStyle.normal.textColor = new Color(tutorialColor.r, tutorialColor.g, tutorialColor.b, 
                                                  tutorialColor.a * rippleAlpha * 0.5f);
        
        GUI.Label(new Rect(centerX - rippleSize/2, centerY - rippleSize/2, rippleSize, rippleSize), "○", rippleStyle);
    }

    // ----------------------------
    // LOCK/UNLOCK TOGGLE
    // ----------------------------
    void HandleLockToggle()
    {
        if (spawnedObject == null) return;

        if (Input.GetKeyDown(lockKey))
        {
            ToggleLock();
        }

        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                float timeSinceLastTap = Time.time - lastTapTime;
                
                if (timeSinceLastTap < tripleTapTimeWindow)
                {
                    tapCount++;
                    if (tapCount >= 2)
                    {
                        ToggleLock();
                        tapCount = 0;
                    }
                }
                else
                {
                    tapCount = 0;
                }
                
                lastTapTime = Time.time;
            }
        }
    }

    void ToggleLock()
    {
        isLocked = !isLocked;
        Debug.Log(isLocked ? "🔒 Object LOCKED" : "🔓 Object UNLOCKED");
        ShowLockNotification();
    }

    // ----------------------------
    // LOCK NOTIFICATION SYSTEM
    // ----------------------------
    void ShowLockNotification()
    {
        showLockNotification = true;
        notificationTimer = notificationDuration;
        notificationAlpha = 1f;
    }

    void UpdateLockNotification()
    {
        if (showLockNotification)
        {
            notificationTimer -= Time.deltaTime;
            
            if (notificationTimer < 0.5f)
            {
                notificationAlpha = notificationTimer / 0.5f;
            }
            
            if (notificationTimer <= 0f)
            {
                showLockNotification = false;
            }
        }
    }

    void OnGUI()
    {
        // TUTORIAL OVERLAY
        if (enableTutorial && !tutorialComplete)
        {
            float centerX = Screen.width / 2f;
            float centerY = Screen.height / 2f;

            Color overlayBg = new Color(0, 0, 0, 0.5f);
            GUI.color = overlayBg;
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "");
            GUI.color = Color.white;

            if (tutorialState == TutorialState.ScanSurfaces)
            {
                float boxWidth = 500;
                float boxHeight = 200;
                float boxX = centerX - boxWidth / 2;
                float boxY = centerY - boxHeight / 2;

                GUI.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
                GUI.Box(new Rect(boxX, boxY, boxWidth, boxHeight), "");
                GUI.color = Color.white;

                GUIStyle iconStyle = new GUIStyle(GUI.skin.label);
                iconStyle.fontSize = 70;
                iconStyle.alignment = TextAnchor.MiddleCenter;
                iconStyle.normal.textColor = tutorialColor;
                GUI.Label(new Rect(boxX, boxY + 20, boxWidth, 80), "📱", iconStyle);

                GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
                titleStyle.fontSize = 32;
                titleStyle.fontStyle = FontStyle.Bold;
                titleStyle.alignment = TextAnchor.MiddleCenter;
                titleStyle.normal.textColor = tutorialColor;
                GUI.Label(new Rect(boxX, boxY + 100, boxWidth, 40), "Scan For Surfaces", titleStyle);

                GUIStyle subtitleStyle = new GUIStyle(GUI.skin.label);
                subtitleStyle.fontSize = 20;
                subtitleStyle.alignment = TextAnchor.MiddleCenter;
                subtitleStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f, 1f);
                GUI.Label(new Rect(boxX, boxY + 145, boxWidth, 30), "Move your device to detect flat surfaces", subtitleStyle);

                DrawRotatingArrows(centerX, centerY, 280, 50);
            }
            else if (tutorialState == TutorialState.TapToPlace)
            {
                float boxWidth = 450;
                float boxHeight = 180;
                float boxX = centerX - boxWidth / 2;
                float boxY = centerY - boxHeight / 2;

                GUI.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
                GUI.Box(new Rect(boxX, boxY, boxWidth, boxHeight), "");
                GUI.color = Color.white;

                GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
                titleStyle.fontSize = 36;
                titleStyle.fontStyle = FontStyle.Bold;
                titleStyle.alignment = TextAnchor.MiddleCenter;
                titleStyle.normal.textColor = new Color(0.3f, 1f, 0.3f, 1f);
                GUI.Label(new Rect(boxX, boxY + 30, boxWidth, 45), "Surface Found! ✓", titleStyle);

                GUIStyle instructionStyle = new GUIStyle(GUI.skin.label);
                instructionStyle.fontSize = 28;
                instructionStyle.alignment = TextAnchor.MiddleCenter;
                instructionStyle.normal.textColor = tutorialColor;
                instructionStyle.wordWrap = true;
                GUI.Label(new Rect(boxX + 20, boxY + 85, boxWidth - 40, 80), 
                         "Tap on the surface to place your object", instructionStyle);

                DrawTapIndicator(centerX, centerY + 150);
                DrawPulsingCircle(centerX, centerY + 150, 80);
            }

            GUI.color = Color.white;
            return;
        }

        // LOCK NOTIFICATION
        if (spawnedObject != null && showLockNotification)
        {
            float boxWidth = 320;
            float boxHeight = 160;
            float xPos = Screen.width - boxWidth - 20;
            float yPos = Screen.height / 2 - boxHeight / 2;
            
            Color bgColor = new Color(0, 0, 0, 0.75f * notificationAlpha);
            GUI.color = bgColor;
            GUI.Box(new Rect(xPos - 10, yPos - 10, boxWidth + 20, boxHeight + 20), "");
            
            GUIStyle iconStyle = new GUIStyle(GUI.skin.label);
            iconStyle.fontSize = 60;
            iconStyle.alignment = TextAnchor.MiddleCenter;
            iconStyle.normal.textColor = new Color(1, 1, 1, notificationAlpha);
            
            string lockIcon = isLocked ? "🔒" : "🔓";
            GUI.color = Color.white;
            GUI.Label(new Rect(xPos, yPos, boxWidth, 70), lockIcon, iconStyle);
            
            GUIStyle statusStyle = new GUIStyle(GUI.skin.label);
            statusStyle.fontSize = 26;
            statusStyle.fontStyle = FontStyle.Bold;
            statusStyle.alignment = TextAnchor.MiddleCenter;
            statusStyle.normal.textColor = new Color(
                isLocked ? 1f : 0.3f, 
                isLocked ? 0.3f : 1f, 
                0.3f, 
                notificationAlpha
            );
            
            string statusText = isLocked ? "LOCKED" : "UNLOCKED";
            GUI.Label(new Rect(xPos, yPos + 65, boxWidth, 35), statusText, statusStyle);
            
            GUIStyle instructionStyle = new GUIStyle(GUI.skin.label);
            instructionStyle.fontSize = 18;
            instructionStyle.alignment = TextAnchor.MiddleCenter;
            instructionStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f, notificationAlpha);
            instructionStyle.wordWrap = true;
            
            string instructionText = Application.platform == RuntimePlatform.Android || 
                                    Application.platform == RuntimePlatform.IPhonePlayer
                ? "Triple-tap to toggle"
                : $"Press '{lockKey}' to toggle";
            
            GUI.Label(new Rect(xPos, yPos + 105, boxWidth, 40), instructionText, instructionStyle);
            
            GUI.color = Color.white;
        }
    }

    // ----------------------------
    // UI DETECTION HELPER
    // ----------------------------
    bool IsPointerOverUI(Vector2 touchPosition)
    {
        if (EventSystem.current != null)
        {
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = touchPosition;
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            return results.Count > 0;
        }
        return false;
    }

    // ----------------------------
    // SELECT OBJECT
    // ----------------------------
    void HandleSelection()
    {
        if (spawnedObject == null) return;

        if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            Vector2 touchPos = Input.GetTouch(0).position;
            
            if (IsPointerOverUI(touchPos))
                return;
            
            Ray r = cam.ScreenPointToRay(touchPos);
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

        if (Input.GetMouseButtonDown(0))
        {
            if (IsPointerOverUI(Input.mousePosition))
                return;
            
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

    // ----------------------------
    // PLACE OBJECT
    // ----------------------------
    void HandlePlacement()
    {
        if (isSelected) return;

        if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            Vector2 touchPos = Input.GetTouch(0).position;
            
            if (IsPointerOverUI(touchPos))
                return;
            
            TryPlaceObject(touchPos);
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (IsPointerOverUI(Input.mousePosition))
                return;
            
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
                spawnedObject = Instantiate(objectToPlace, hitPose.position, hitPose.rotation);

                if (previewObject != null)
                    previewObject.SetActive(false);
                if (spinningIndicator != null)
                    spinningIndicator.SetActive(false);

                ClampScale();

                AddCollidersToPlanes();
                
                if (disablePlanesAfterPlacement)
                {
                    DisableAllPlanes();
                }
		ShowLockNotification();

            // Connect ProjectileLauncher if present
            ProjectileLauncher launcher = spawnedObject.GetComponentInChildren<ProjectileLauncher>();
            ProjectileUIBinder uiBinder = FindFirstObjectByType<ProjectileUIBinder>();
            if (uiBinder != null && launcher != null)
            {
                uiBinder.BindLauncherAtRuntime(launcher);
                Debug.Log("ProjectileLauncher connected to UI!");
            }

            // Connect CliffController if present
            CliffController cliffCtrl = spawnedObject.GetComponentInChildren<CliffController>();
            if (cliffCtrl != null && uiBinder != null)
            {
                uiBinder.BindCliffController(cliffCtrl);
                Debug.Log("CliffController connected to UI!");
            }

            // Connect Pendulum3D if present
            Pendulum3D pend = spawnedObject.GetComponentInChildren<Pendulum3D>();
            PendulumUI pendUI = FindFirstObjectByType<PendulumUI>();
            if (pendUI != null && pend != null)
            {
                pendUI.SetPendulum(pend);
                Debug.Log("Pendulum connected to UI!");
            }
        }
        else if (!isSelected)
        {
            spawnedObject.transform.SetPositionAndRotation(hitPose.position, hitPose.rotation);
        }
    }
}

// ----------------------------
// MANIPULATION
// ----------------------------
void HandleManipulation()
{
    if (spawnedObject == null || !isSelected) return;

    if (Input.touchCount == 1)
    {
        Touch t = Input.GetTouch(0);
        if (t.phase == TouchPhase.Moved)
            MoveObject(t.deltaPosition * 0.0015f);
    }
    else if (Input.touchCount == 2)
    {
        Touch t0 = Input.GetTouch(0);
        Touch t1 = Input.GetTouch(1);

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
            
            ClampScale();

            float rotationDelta =
                Vector2.SignedAngle(t1.position - lastTouchPos1, t1.position - t0.position);
            spawnedObject.transform.Rotate(Vector3.up, rotationDelta);
        }

        lastTouchPos0 = t0.position;
        lastTouchPos1 = t1.position;
    }

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
        
        ClampScale();
    }
}

void MoveObject(Vector2 delta)
{
    Vector3 move =
        cam.transform.right * delta.x +
        cam.transform.up * delta.y;

    spawnedObject.transform.position += move;
}

void ClampScale()
{
    if (spawnedObject == null) return;

    Vector3 scale = spawnedObject.transform.localScale;
    scale.x = Mathf.Clamp(scale.x, minScale, maxScale);
    scale.y = Mathf.Clamp(scale.y, minScale, maxScale);
    scale.z = Mathf.Clamp(scale.z, minScale, maxScale);
    spawnedObject.transform.localScale = scale;
}

// ----------------------------
// PUBLIC METHODS
// ----------------------------
public void SetLocked(bool locked)
{
    isLocked = locked;
    Debug.Log(isLocked ? "🔒 Object LOCKED" : "🔓 Object UNLOCKED");
}

public bool IsLocked()
{
    return isLocked;
}

public void SetPreviewEnabled(bool enabled)
{
    showPreview = enabled;
    if (previewObject != null && !enabled)
        previewObject.SetActive(false);
}

public void SetSpinningIndicatorEnabled(bool enabled)
{
    showSpinningIndicator = enabled;
    if (spinningIndicator != null && !enabled)
        spinningIndicator.SetActive(false);
}

void OnDestroy()
{
    if (previewObject != null)
        Destroy(previewObject);
    if (spinningIndicator != null)
        Destroy(spinningIndicator);
    
    // Unsubscribe using NEW API
    if (planeManager != null)
    {
        planeManager.trackablesChanged.RemoveListener(OnPlanesChanged);
    }
}
}