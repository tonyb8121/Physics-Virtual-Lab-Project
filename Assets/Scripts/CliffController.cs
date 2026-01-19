using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CliffController : MonoBehaviour
{
    [Header("Cliff References")]
    public Transform cliffCube;
    public Transform greenTop;  // NEW: The green plane on top
    
    [Header("Height Settings")]
    public float minHeight = 0f;
    public float maxHeight = 50f;
    
    [HideInInspector] public Slider heightSlider;
    [HideInInspector] public TextMeshProUGUI heightLabel;
    
    public float currentHeight { get; private set; } = 0f;
    
    private Vector3 baseLocalPosition;
    private Vector3 baseScale;
    private Vector3 greenTopBaseLocalPosition;  // NEW: Store green top base position
    private bool isInitialized = false;

    void Awake()
    {
        Debug.Log($"[CliffController] Awake called on {gameObject.name}");
        
        // Find the cliff cube
        if (cliffCube == null)
        {
            Transform found = transform.Find("CliffCube");
            if (found != null)
            {
                cliffCube = found;
                Debug.Log($"✓ Auto-found CliffCube: {cliffCube.name}");
            }
            else
            {
                MeshFilter[] meshes = GetComponentsInChildren<MeshFilter>();
                foreach (MeshFilter mf in meshes)
                {
                    if (mf.sharedMesh != null && mf.sharedMesh.name.Contains("Cube"))
                    {
                        cliffCube = mf.transform;
                        Debug.Log($"✓ Auto-found cube: {cliffCube.name}");
                        break;
                    }
                }
            }
        }
        else
        {
            Debug.Log($"✓ CliffCube already assigned: {cliffCube.name}");
        }

        // Find the green top plane
        if (greenTop == null)
        {
            Transform found = transform.Find("GreenTop");
            if (found != null)
            {
                greenTop = found;
                Debug.Log($"✓ Auto-found GreenTop: {greenTop.name}");
            }
        }

        // Initialize immediately in Awake
        if (cliffCube != null)
        {
            // Store the ORIGINAL LOCAL position and scale
            baseLocalPosition = cliffCube.localPosition;
            baseScale = cliffCube.localScale;
            
            // Store green top base position if it exists
            if (greenTop != null)
            {
                greenTopBaseLocalPosition = greenTop.localPosition;
                Debug.Log($"✓ GreenTop base position stored: {greenTopBaseLocalPosition}");
            }
            
            Debug.Log($"Base LOCAL Position: {baseLocalPosition}");
            Debug.Log($"Base Scale: {baseScale}");
            
            // Mark as initialized
            isInitialized = true;
            
            // Start with cliff hidden (height = 0)
            SetHeightDirectly(0f);
            
            Debug.Log($"✓ CliffController initialized in Awake (Cube: {cliffCube.name})");
        }
        else
        {
            Debug.LogError($"⚠️ CliffCube is NULL in Awake on {gameObject.name}!");
        }
    }

    void Start()
    {
        // Additional check in Start
        if (!isInitialized)
        {
            Debug.LogError($"⚠️ Cliff was not initialized in Awake!");
            Debug.LogError($"Children count: {transform.childCount}");
            for (int i = 0; i < transform.childCount; i++)
            {
                Debug.LogError($"  Child {i}: {transform.GetChild(i).name}");
            }
        }
    }

    public void OnHeightChanged(float newHeight)
    {
        Debug.Log($"[CliffController] OnHeightChanged called with value: {newHeight}");
        
        if (!isInitialized)
        {
            Debug.LogWarning("Cliff not initialized yet, initializing now...");
            
            // Emergency initialization if something went wrong
            if (cliffCube != null)
            {
                baseLocalPosition = cliffCube.localPosition;
                baseScale = cliffCube.localScale;
                
                if (greenTop != null)
                {
                    greenTopBaseLocalPosition = greenTop.localPosition;
                }
                
                isInitialized = true;
                Debug.Log("✓ Emergency initialization complete");
            }
            else
            {
                Debug.LogError("⚠️ Cannot initialize - CliffCube is null!");
                return;
            }
        }
        
        SetHeightDirectly(newHeight);
    }

    void SetHeightDirectly(float height)
    {
        if (cliffCube == null)
        {
            Debug.LogError("⚠️ SetHeightDirectly: CliffCube is NULL!");
            return;
        }

        // Clamp height to valid range
        height = Mathf.Clamp(height, minHeight, maxHeight);
        currentHeight = height;
        
        // Update label
        if (heightLabel != null)
        {
            heightLabel.text = $"{height:F1} m";
        }

        if (height <= 0.01f)
        {
            // HIDDEN STATE - cliff at ground level with minimal scale
            Vector3 hiddenScale = baseScale;
            hiddenScale.y = 0.01f; // Nearly invisible
            cliffCube.localScale = hiddenScale;
            
            // Reset to base LOCAL position
            cliffCube.localPosition = baseLocalPosition;
            
            // Hide green top
            if (greenTop != null)
            {
                greenTop.gameObject.SetActive(false);
            }
            
            Debug.Log($"Cliff HIDDEN - Scale Y: {hiddenScale.y:F3}, Local Pos: {baseLocalPosition}");
        }
        else
        {
            // VISIBLE STATE - scale up and raise position
            Vector3 newScale = baseScale;
            newScale.y = height; // Scale to desired height
            cliffCube.localScale = newScale;
            
            // Raise the cube LOCALLY so bottom stays at base position
            Vector3 newLocalPos = baseLocalPosition;
            newLocalPos.y = baseLocalPosition.y + (height / 2f);
            cliffCube.localPosition = newLocalPos;
            
            // Position and show green top
            if (greenTop != null)
            {
                greenTop.gameObject.SetActive(true);
                
                // Position green top at the top of the cliff
                Vector3 greenTopPos = greenTopBaseLocalPosition;
                greenTopPos.y = baseLocalPosition.y + height;  // Top of the cliff
                greenTop.localPosition = greenTopPos;
                
                Debug.Log($"GreenTop positioned at local Y: {greenTopPos.y:F2}");
            }
            
            Debug.Log($"Cliff VISIBLE - Height: {height:F2}m, Scale Y: {newScale.y:F2}, Local Pos Y: {newLocalPos.y:F2}");
        }
    }

    public void SetHeight(float height)
    {
        Debug.Log($"[CliffController] SetHeight called with: {height}");
        
        if (heightSlider != null)
        {
            Debug.Log($"Setting slider value to: {height}");
            heightSlider.value = height;
        }
        else
        {
            Debug.Log("No slider assigned, setting directly");
            SetHeightDirectly(height);
        }
    }

    public float GetCurrentHeight()
    {
        return currentHeight;
    }
}