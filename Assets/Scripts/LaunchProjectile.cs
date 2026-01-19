using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ProjectileLauncher : MonoBehaviour
{
    [Header("References")]
    public Transform pivot;
    public Transform barrel;
    public Transform barrelTip;
    public GameObject projectilePrefab;
    public ParticleSystem muzzleFlash;
    public AudioClip fireSound;

    [Header("UI Controls")]
    public Slider angleSlider;
    public Slider velocitySlider;
    public TMP_Dropdown gravityDropdown;
    public Button launchButton;

    [Header("UI Display References")]
    public TextMeshProUGUI angleLabel;         
    public TextMeshProUGUI velocityLabel;      
    public TextMeshProUGUI gravityLabel;
    
    [Header("Results Display")]
    public GameObject resultsPanel;
    public TextMeshProUGUI timeOfFlightText;
    public TextMeshProUGUI maxHeightText;
    public TextMeshProUGUI rangeText;
    public TextMeshProUGUI launchHeightText;

    [Header("Launch Settings")]
    public float projectileAutoDestroy = 15f;
    public float slowMotionFactor = 1f;  // 1 = normal speed, 0.5 = half speed
    
    private AudioSource audioSource;
    private CliffController cliffController;

    private readonly float[] gravityValues = { 9.81f, 1.62f, 3.71f, 24.79f, 0f };
    private readonly string[] gravityNames = 
        { "Earth (9.81)", "Moon (1.62)", "Mars (3.71)", "Jupiter (24.79)", "Zero (0.00)" };

    void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
    }

    void Start()
    {
        // Find cliff controller in scene (optional - for getting launch height)
        cliffController = FindFirstObjectByType<CliffController>();
        
        if (cliffController != null)
        {
            Debug.Log("Cliff controller found - will use cliff height for launch calculations");
        }
        else
        {
            Debug.Log("No cliff controller found - using barrel tip height");
        }

        // Safety checks
        if (pivot == null || barrel == null || barrelTip == null)
            Debug.LogWarning("Basic references missing!");

        if (angleSlider == null || velocitySlider == null || gravityDropdown == null || launchButton == null)
            Debug.LogWarning("UI references missing!");

        // Hook UI
        if (angleSlider != null)
            angleSlider.onValueChanged.AddListener(OnAngleChanged);
        if (velocitySlider != null)
            velocitySlider.onValueChanged.AddListener(OnVelocityChanged);
        if (gravityDropdown != null)
            gravityDropdown.onValueChanged.AddListener(OnGravityChanged);
        if (launchButton != null)
            launchButton.onClick.AddListener(OnLaunchClicked);

        // Initialize UI
        if (angleSlider != null)
            OnAngleChanged(angleSlider.value);
        if (velocitySlider != null)
            OnVelocityChanged(velocitySlider.value);
        if (gravityDropdown != null)
            OnGravityChanged(gravityDropdown.value);
        
        // Hide results panel initially
        if (resultsPanel != null)
            resultsPanel.SetActive(false);
    }

    void OnDestroy()
    {
        if (angleSlider != null)
            angleSlider.onValueChanged.RemoveAllListeners();
        if (velocitySlider != null)
            velocitySlider.onValueChanged.RemoveAllListeners();
        if (gravityDropdown != null)
            gravityDropdown.onValueChanged.RemoveAllListeners();
        if (launchButton != null)
            launchButton.onClick.RemoveAllListeners();
    }

    public void OnAngleChanged(float val)
    {
        if (pivot != null)
            pivot.localEulerAngles = new Vector3(0f, 0f, -val);

        if (angleLabel != null)
            angleLabel.text = $"{val:F1}°";
    }

    public void OnVelocityChanged(float val)
    {
        if (velocityLabel != null)
            velocityLabel.text = $"{val:F1} m/s";
    }

    public void OnGravityChanged(int index)
    {
        if (index < 0 || index >= gravityValues.Length) return;

        float g = gravityValues[index];
        Physics.gravity = new Vector3(0f, -g, 0f);

        if (gravityLabel != null)
            gravityLabel.text = gravityNames[index];
    }

    public void OnLaunchClicked()
    {
        if (launchButton != null)
            launchButton.interactable = false;
        FireProjectile();
    }

    void FireProjectile()
    {
        if (barrel == null || barrelTip == null || projectilePrefab == null)
        {
            Debug.LogError("Cannot fire projectile - missing references!");
            if (launchButton != null)
                launchButton.interactable = true;
            return;
        }

        Vector3 dir = barrel.transform.up.normalized;
        float speed = velocitySlider != null ? velocitySlider.value : 10f;

        // Play muzzle flash
        if (muzzleFlash != null)
            muzzleFlash.Play();
        
        // Play fire sound
        if (fireSound != null && audioSource != null)
            audioSource.PlayOneShot(fireSound);

        // Instantiate projectile
        GameObject projGO = Instantiate(projectilePrefab, barrelTip.position, Quaternion.identity);

        // Get ProjectileBehavior
        ProjectileBehavior pb = projGO.GetComponent<ProjectileBehavior>();
        if (pb != null)
        {
            // Calculate launch height
            // If cliff exists, use cliff height + barrel tip height
            // Otherwise just use barrel tip height
            if (cliffController != null)
            {
                float cliffHeight = cliffController.GetCurrentHeight();
                pb.launchHeight = cliffHeight + barrelTip.position.y;
                Debug.Log($"Launching from cliff height: {cliffHeight:F2}m + barrel: {barrelTip.position.y:F2}m = {pb.launchHeight:F2}m");
            }
            else
            {
                pb.launchHeight = barrelTip.position.y;
                Debug.Log($"Launching from barrel height: {pb.launchHeight:F2}m");
            }

            // Apply slow motion for better viewing (optional)
            if (slowMotionFactor < 1f)
            {
                Time.timeScale = slowMotionFactor;
            }

            // Launch the projectile
            Vector3 launchVelocity = dir * speed;
            pb.Launch(launchVelocity);

            // Listen for landing event
            pb.onLanded.RemoveAllListeners();
            pb.onLanded.AddListener(OnProjectileLanded);
            
            // Store reference to projectile behavior for the callback
            currentProjectile = pb;
        }
        else
        {
            Debug.LogError("Projectile prefab missing ProjectileBehavior component!");
            Time.timeScale = 1f;
            if (launchButton != null)
                launchButton.interactable = true;
        }

        // Destroy after set duration
        Destroy(projGO, projectileAutoDestroy);
    }

    private ProjectileBehavior currentProjectile;

    void OnProjectileLanded()
    {
        // Reset time scale
        Time.timeScale = 1f;
        
        if (launchButton != null)
            launchButton.interactable = true;

        // Display results
        if (currentProjectile != null)
        {
            DisplayResults(currentProjectile);
        }
    }

    void DisplayResults(ProjectileBehavior pb)
    {
        if (resultsPanel != null)
            resultsPanel.SetActive(true);

        if (timeOfFlightText != null)
            timeOfFlightText.text = $"Time of Flight: {pb.timeOfFlight:F2} s";

        if (maxHeightText != null)
            maxHeightText.text = $"Max Height: {pb.maxHeight:F2} m";

        if (rangeText != null)
            rangeText.text = $"Horizontal Range: {pb.range:F2} m";

        if (launchHeightText != null)
            launchHeightText.text = $"Launch Height: {pb.launchHeight:F2} m";

        Debug.Log($"===== RESULTS =====");
        Debug.Log($"Launch Height: {pb.launchHeight:F2} m");
        Debug.Log($"Time of Flight: {pb.timeOfFlight:F2} s");
        Debug.Log($"Max Height Above Launch: {pb.maxHeight:F2} m");
        Debug.Log($"Horizontal Range: {pb.range:F2} m");
    }
}