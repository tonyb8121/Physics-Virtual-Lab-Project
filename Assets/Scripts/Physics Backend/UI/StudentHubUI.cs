using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Controls the Student Dashboard (MainScene) with tabs and experiment cards.
/// Supports both online and offline modes with auto-navigation.
/// Includes integration for a global Settings UI.
/// </summary>
public class StudentHubUI : MonoBehaviour
{
    [Header("Bottom Tabs")]
    [SerializeField] private Button btnExperiments;
    [SerializeField] private Button btnQuiz;
    [SerializeField] private Button btnProfile;

    [Header("Main Panels")]
    [SerializeField] private GameObject panelExperiments;
    [SerializeField] private GameObject panelQuiz;
    [SerializeField] private GameObject panelProfile;

    [Header("Experiment Cards")]
    [SerializeField] private Button btnProjectile;  // Yellow card
    [SerializeField] private Button btnPendulum;    // Pink card

    [Header("Tab Visuals")]
    [SerializeField] private Image iconExperiments;
    [SerializeField] private Image iconQuiz;
    [SerializeField] private Image iconProfile;
    [SerializeField] private Color activeColor = Color.cyan;
    [SerializeField] private Color inactiveColor = Color.gray;

    // --- ✅ NEW: Global Features Header ---
    [Header("Global Features")]
    [SerializeField] private Button btnSettings;       // ✅ NEW: Drag your new Settings Button here
    [SerializeField] private SettingsUI settingsScript; // ✅ NEW: Drag the SettingsPanel (with script) here
    // ------------------------------------

    [Header("Offline Features")]
    [SerializeField] private Button btnBackToMenu;         // Back to offline menu button
    [SerializeField] private GameObject containerAssignments; // Panel_Assignments in Quiz tab
    [SerializeField] private GameObject containerPractice;    // Panel_Practice in Quiz tab
    [SerializeField] private GameObject containerTiers;       // Panel_Tiers in Quiz tab

    private bool isOfflineMode = false;

    /// <summary>
    /// Initialize the UI with proper timing to handle auto-navigation
    /// Uses coroutine to wait one frame before processing navigation
    /// </summary>
    private IEnumerator Start()
    {
        // Check if user is in offline mode
        CheckOfflineMode();

        // Setup all UI listeners
        SetupTabListeners();
        SetupExperimentListeners();
        SetupOfflineFeatures();
        SetupSettings(); // ✅ Initialize Settings

        // ✅ FIX: Wait 1 frame to let other UI scripts finish initializing
        yield return null;

        // Handle auto-navigation from previous scenes
        HandleAutoNavigation();
    }

    /// <summary>
    /// Check if the user is in offline mode
    /// </summary>
    private void CheckOfflineMode()
    {
        // Assuming AuthManager.Instance is accessible and has IsLoggedIn
        isOfflineMode = (AuthManager.Instance == null || !AuthManager.Instance.IsLoggedIn);

        if (isOfflineMode)
        {
            Debug.Log("[StudentHubUI] Running in OFFLINE mode");
        }
        else
        {
            Debug.Log("[StudentHubUI] Running in ONLINE mode");
        }
    }

    // ✅ NEW: Setup Settings Button
    private void SetupSettings()
    {
        if (btnSettings != null && settingsScript != null)
        {
            btnSettings.onClick.RemoveAllListeners(); // Clean slate
            btnSettings.onClick.AddListener(() => {
                // Ensure the SettingsUI script is assigned and has the OpenSettings method
                settingsScript.OpenSettings(); // Open the panel
            });
            Debug.Log("✅ Settings button connected in MainScene");
        }
        else
        {
            Debug.LogWarning("⚠️ Settings Button or Script not assigned in StudentHubUI inspector! Cannot use Settings feature.");
        }
    }

    /// <summary>
    /// Setup listeners for bottom tab navigation
    /// </summary>
    private void SetupTabListeners()
    {
        if (btnExperiments != null)
        {
            btnExperiments.onClick.RemoveAllListeners();
            btnExperiments.onClick.AddListener(() => SwitchTab(0));
        }

        if (btnQuiz != null)
        {
            btnQuiz.onClick.RemoveAllListeners();
            btnQuiz.onClick.AddListener(() => SwitchTab(1));
        }

        if (btnProfile != null)
        {
            btnProfile.onClick.RemoveAllListeners();
            btnProfile.onClick.AddListener(() => 
            {
                // REDIRECT LOGIC: If offline, clicking Profile goes to Login/Auth Scene
                if (isOfflineMode)
                {
                    Debug.Log("User is offline -> Redirecting to AuthScene");
                    ShowToast("Please log in to access your profile.");
                    SceneManager.LoadScene("AuthScene"); // Go to Login/Signup Scene
                }
                else
                {
                    SwitchTab(2); // Normal behavior: switch to Profile tab
                }
            });
            // The button must always be interactive to allow the user to click and be redirected
            btnProfile.interactable = true; 
            
            // Visual check: Reset color to look inactive if offline
            if (iconProfile) iconProfile.color = isOfflineMode ? (Color.gray * 0.7f) : inactiveColor;
        }
    }

    /// <summary>
    /// Setup listeners for experiment card buttons
    /// </summary>
    private void SetupExperimentListeners()
    {
        if (btnProjectile != null)
        {
            btnProjectile.onClick.RemoveAllListeners();
            btnProjectile.onClick.AddListener(() => LoadExperimentScene("Projectile"));
            Debug.Log("✅ Projectile button listener added");
        }
        else
        {
            Debug.LogError("❌ btnProjectile is NULL! Assign it in Inspector!");
        }

        if (btnPendulum != null)
        {
            btnPendulum.onClick.RemoveAllListeners();
            btnPendulum.onClick.AddListener(() => LoadExperimentScene("Pendulum"));
            Debug.Log("✅ Pendulum button listener added");
        }
        else
        {
            Debug.LogError("❌ btnPendulum is NULL! Assign it in Inspector!");
        }
    }

    /// <summary>
    /// Setup offline-specific features like the back button
    /// </summary>
    private void SetupOfflineFeatures()
    {
        if (btnBackToMenu != null)
        {
            // Only show back button in offline mode
            btnBackToMenu.gameObject.SetActive(isOfflineMode);

            if (isOfflineMode)
            {
                btnBackToMenu.onClick.RemoveAllListeners();
                btnBackToMenu.onClick.AddListener(ReturnToOfflineMenu);
                Debug.Log("✅ Back to Menu button enabled for offline mode");
            }
        }
        else if (isOfflineMode)
        {
            Debug.LogWarning("[StudentHubUI] btnBackToMenu not assigned but running in offline mode!");
        }
    }

    /// <summary>
    /// Handle auto-navigation from PlayerPrefs (e.g., from Offline Menu)
    /// </summary>
    private void HandleAutoNavigation()
    {
        // Check if we need to open a specific tab
        if (PlayerPrefs.HasKey("OpenTargetTab"))
        {
            int targetTab = PlayerPrefs.GetInt("OpenTargetTab");
            PlayerPrefs.DeleteKey("OpenTargetTab");

            Debug.Log($"[StudentHubUI] Auto-navigating to tab {targetTab}");
            
            // Check if attempting to auto-navigate to Profile in offline mode
            if (targetTab == 2 && isOfflineMode)
            {
                Debug.LogWarning("[StudentHubUI] Attempted auto-navigation to Profile in offline mode. Defaulting to Experiments (0).");
                SwitchTab(0);
            }
            else
            {
                SwitchTab(targetTab);
            }

            // Check for sub-tab navigation (e.g., Tiers within Quiz)
            if (PlayerPrefs.HasKey("OpenTargetSubTab"))
            {
                string subTab = PlayerPrefs.GetString("OpenTargetSubTab");
                PlayerPrefs.DeleteKey("OpenTargetSubTab");

                Debug.Log($"[StudentHubUI] Auto-navigating to sub-tab: {subTab}");

                // Handle sub-tab navigation in Quiz tab (index 1)
                if (targetTab == 1)
                {
                    if (subTab == "Tiers")
                    {
                        OpenTiersContainer();
                    }
                    else if (subTab == "Practice")
                    {
                        OpenPracticeContainer();
                    }
                    else if (subTab == "Assignments")
                    {
                        OpenAssignmentsContainer();
                    }
                }
            }
        }
        else
        {
            // Default to Experiments tab
            SwitchTab(0);
        }
    }

    /// <summary>
    /// Switch between main tabs (Experiments, Quiz, Profile)
    /// </summary>
    private void SwitchTab(int index)
    {
        // Validate index
        if (index < 0 || index > 2)
        {
            Debug.LogError($"[StudentHubUI] Invalid tab index: {index}");
            return;
        }

        // IMPORTANT FIX: If offline, we prevent opening panelProfile here,
        // relying on the listener in SetupTabListeners to handle the redirection.
        if (index == 2 && isOfflineMode)
        {
            Debug.LogWarning("[StudentHubUI] Cannot switch to Profile tab in offline mode (redirect expected)");
            return; 
        }

        // 1. Activate/Deactivate Panels
        if (panelExperiments != null) panelExperiments.SetActive(index == 0);
        if (panelQuiz != null) panelQuiz.SetActive(index == 1);
        // Only set panelProfile active if index == 2 AND not offline
        if (panelProfile != null) panelProfile.SetActive(index == 2 && !isOfflineMode); 

        // 2. Update Tab Icon Colors
        UpdateTabVisuals(index);
        
        // ✅ OPTIONAL: If you only want the Settings button visible in Quiz Hub
        // if (btnSettings) btnSettings.gameObject.SetActive(index == 1); 

        Debug.Log($"[StudentHubUI] Switched to tab {index}");
    }

    /// <summary>
    /// Update visual indicators for active/inactive tabs
    /// </summary>
    private void UpdateTabVisuals(int activeIndex)
    {
        if (iconExperiments != null)
        {
            iconExperiments.color = (activeIndex == 0) ? activeColor : inactiveColor;
        }

        if (iconQuiz != null)
        {
            iconQuiz.color = (activeIndex == 1) ? activeColor : inactiveColor;
        }

        if (iconProfile != null)
        {
            // Show active color only if the tab is active AND the app is online
            if (activeIndex == 2 && !isOfflineMode)
            {
                iconProfile.color = activeColor;
            }
            else
            {
                iconProfile.color = inactiveColor;
            }
        }
    }

    /// <summary>
    /// Open the Tiers container within the Quiz tab
    /// Forces all other containers OFF to prevent conflicts
    /// </summary>
    private void OpenTiersContainer()
    {
        if (containerTiers == null)
        {
            Debug.LogError("[StudentHubUI] containerTiers is not assigned! Check Inspector.");
            return;
        }

        // Force all other containers OFF first
        if (containerAssignments != null) containerAssignments.SetActive(false);
        if (containerPractice != null) containerPractice.SetActive(false);

        // Force Tiers ON
        containerTiers.SetActive(true);

        Debug.Log("🎯 [StudentHubUI] Auto-opened Tiers Container");
    }

    /// <summary>
    /// Open the Practice container within the Quiz tab
    /// Forces all other containers OFF to prevent conflicts
    /// </summary>
    private void OpenPracticeContainer()
    {
        if (containerPractice == null)
        {
            Debug.LogError("[StudentHubUI] containerPractice is not assigned! Check Inspector.");
            return;
        }

        // Force all other containers OFF
        if (containerAssignments != null) containerAssignments.SetActive(false);
        if (containerTiers != null) containerTiers.SetActive(false);

        // Force Practice ON
        containerPractice.SetActive(true);

        Debug.Log("🎯 [StudentHubUI] Auto-opened Practice Container");
    }

    /// <summary>
    /// Open the Assignments container within the Quiz tab
    /// Forces all other containers OFF to prevent conflicts
    /// </summary>
    private void OpenAssignmentsContainer()
    {
        if (containerAssignments == null)
        {
            Debug.LogError("[StudentHubUI] containerAssignments is not assigned! Check Inspector.");
            return;
        }

        // Force Assignments ON
        containerAssignments.SetActive(true);

        // Force all other containers OFF
        if (containerPractice != null) containerPractice.SetActive(false);
        if (containerTiers != null) containerTiers.SetActive(false);

        Debug.Log("🎯 [StudentHubUI] Auto-opened Assignments Container");
    }

    /// <summary>
    /// Return to the offline menu scene
    /// </summary>
    private void ReturnToOfflineMenu()
    {
        Debug.Log("[StudentHubUI] Returning to Offline Menu...");
        ShowToast("Returning to menu...");
        SceneManager.LoadScene("Menu");
    }

    /// <summary>
    /// Load an experiment scene (Pendulum or Projectile)
    /// </summary>
    private void LoadExperimentScene(string sceneName)
    {
        Debug.Log($"🚀 [StudentHubUI] Loading scene: {sceneName}");
        ShowToast($"Loading {sceneName}...");
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Show a toast message to the user
    /// </summary>
    private void ShowToast(string message)
    {
        // Assuming UIManager.Instance is available for toast messages
        // if (UIManager.Instance != null)
        // {
        //     UIManager.Instance.ShowToast(message);
        // }
        // else
        // {
            Debug.Log($"[Toast] {message}");
        // }
    }

    /// <summary>
    /// Clean up PlayerPrefs on destroy (optional safety measure)
    /// </summary>
    private void OnDestroy()
    {
        // Clean up any remaining navigation keys
        if (PlayerPrefs.HasKey("OpenTargetTab"))
        {
            PlayerPrefs.DeleteKey("OpenTargetTab");
        }

        if (PlayerPrefs.HasKey("OpenTargetSubTab"))
        {
            PlayerPrefs.DeleteKey("OpenTargetSubTab");
        }
    }
}