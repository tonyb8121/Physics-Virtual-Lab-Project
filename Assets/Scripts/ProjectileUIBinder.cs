using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ProjectileUIBinder : MonoBehaviour
{
    [Header("Launcher UI Controls")]
    public Slider angleSlider;
    public Slider velocitySlider;
    public TMP_Dropdown gravityDropdown;
    public Button launchButton;
    public Button closeButton;
    
    [Header("Cliff UI Controls")]
    public Slider heightSlider;
    public TextMeshProUGUI heightLabel;
    
    [Header("Display Labels")]
    public TextMeshProUGUI angleLabel;
    public TextMeshProUGUI velocityLabel;
    public TextMeshProUGUI gravityLabel;

    private ProjectileLauncher boundLauncher;
    private CliffController boundCliff;

    void Start()
    {
        Debug.Log("[ProjectileUIBinder] Start - Checking UI references:");
        Debug.Log($"  Height Slider: {(heightSlider != null ? heightSlider.name : "NULL")}");
        Debug.Log($"  Height Label: {(heightLabel != null ? heightLabel.name : "NULL")}");
        
        if (heightSlider != null)
        {
            Debug.Log($"  Slider Min: {heightSlider.minValue}, Max: {heightSlider.maxValue}, Value: {heightSlider.value}");
        }
    }

    public void BindLauncherAtRuntime(ProjectileLauncher launcher)
    {
        Debug.Log("[ProjectileUIBinder] BindLauncherAtRuntime called");
        
        boundLauncher = launcher;
        if (boundLauncher == null)
        {
            Debug.LogError("ProjectileLauncher is null!");
            return;
        }

        boundLauncher.angleSlider = angleSlider;
        boundLauncher.velocitySlider = velocitySlider;
        boundLauncher.gravityDropdown = gravityDropdown;
        boundLauncher.launchButton = launchButton;
        boundLauncher.angleLabel = angleLabel;
        boundLauncher.velocityLabel = velocityLabel;
        boundLauncher.gravityLabel = gravityLabel;

        if (angleSlider != null)
            boundLauncher.OnAngleChanged(angleSlider.value);
        if (velocitySlider != null)
            boundLauncher.OnVelocityChanged(velocitySlider.value);
        if (gravityDropdown != null)
            boundLauncher.OnGravityChanged(gravityDropdown.value);

        if (angleSlider != null)
            angleSlider.onValueChanged.AddListener(boundLauncher.OnAngleChanged);
        if (velocitySlider != null)
            velocitySlider.onValueChanged.AddListener(boundLauncher.OnVelocityChanged);
        if (gravityDropdown != null)
            gravityDropdown.onValueChanged.AddListener(boundLauncher.OnGravityChanged);
        if (launchButton != null)
            launchButton.onClick.AddListener(boundLauncher.OnLaunchClicked);

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(OnCloseButtonClicked);
        }

        Debug.Log("✓ Launcher UI bound successfully!");
    }

    void OnCloseButtonClicked()
    {
        gameObject.SetActive(false);
    }

    public void BindCliffController(CliffController cliff)
    {
        Debug.Log("[ProjectileUIBinder] BindCliffController called");
        
        boundCliff = cliff;
        if (boundCliff == null)
        {
            Debug.LogError("⚠️ CliffController is null!");
            return;
        }

        Debug.Log($"Binding cliff controller on GameObject: {cliff.gameObject.name}");

        if (heightSlider == null)
        {
            Debug.LogError("⚠️ Height slider NOT assigned in ProjectileUIBinder inspector!");
            return;
        }

        Debug.Log($"✓ Height slider found: {heightSlider.name}");

        // Assign references to the cliff controller
        boundCliff.heightSlider = heightSlider;
        boundCliff.heightLabel = heightLabel;

        // Configure slider range
        Debug.Log($"Setting slider range: {boundCliff.minHeight} to {boundCliff.maxHeight}");
        heightSlider.minValue = boundCliff.minHeight;
        heightSlider.maxValue = boundCliff.maxHeight;
        
        // IMPORTANT: Set slider to 0 BEFORE adding listener
        heightSlider.value = 0f;

        Debug.Log("Removing old slider listeners...");
        heightSlider.onValueChanged.RemoveAllListeners();
        
        Debug.Log("Adding new slider listener...");
        heightSlider.onValueChanged.AddListener(boundCliff.OnHeightChanged);

        // Force initial update to ensure cliff starts at height 0
        // This is called AFTER the listener is added so it triggers properly
        Debug.Log("Setting initial cliff height to 0...");
        boundCliff.OnHeightChanged(0f);

        Debug.Log("✓ Cliff UI bound successfully!");
        Debug.Log("Slider is now ready - try moving it!");
    }

    public float GetCliffHeight()
    {
        if (boundCliff != null)
            return boundCliff.currentHeight;
        return 0f;
    }

    public void SetCliffHeight(float height)
    {
        if (boundCliff != null)
        {
            boundCliff.SetHeight(height);
        }
    }
}