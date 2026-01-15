using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Smart app initializer - Handles startup routing based on user authentication status
/// Attach this to a GameObject in a "Bootstrap" or "Splash" scene (first scene that loads)
/// Or attach to Home scene and it will auto-route on first Start()
/// </summary>
public class AppInitializer : MonoBehaviour
{
    [Header("Routing Configuration")]
    [SerializeField] private bool checkAuthOnStart = true;
    [SerializeField] private float splashDelay = 1f; // Optional delay for splash/logo
    
    [Header("Scene Names")]
    [SerializeField] private string homeSceneName = "Home";
    [SerializeField] private string mainSceneName = "MainScene"; // Student dashboard
    [SerializeField] private string teacherDashboardSceneName = "TeacherDashboardScene";
    [SerializeField] private string menuSceneName = "Menu"; // Offline AR menu
    
    private static bool hasInitialized = false;
    
    private void Start()
    {
        // Only run initialization once per app session
        if (!hasInitialized && checkAuthOnStart)
        {
            hasInitialized = true;
            StartCoroutine(InitializeApp());
        }
    }
    
    private IEnumerator InitializeApp()
    {
        // Optional: Show splash screen or loading
        Debug.Log("Initializing app...");
        
        // Wait for splash delay (can show logo here)
        yield return new WaitForSeconds(splashDelay);
        
        // Wait for AuthManager to initialize (if using Firebase)
        if (AuthManager.Instance != null)
        {
            // Wait a bit for Firebase to check saved auth state
            yield return new WaitForSeconds(0.5f);
            
            // Check if user is already logged in
            if (AuthManager.Instance.IsLoggedIn)
            {
                Debug.Log("User already logged in. Routing to appropriate scene...");
                RouteLoggedInUser();
            }
            else
            {
                Debug.Log("No user logged in. Showing Home screen...");
                LoadHome();
            }
        }
        else
        {
            // No AuthManager - go to Home
            Debug.Log("No AuthManager found. Loading Home...");
            LoadHome();
        }
    }
    
    /// <summary>
    /// Route already logged-in user to their appropriate scene
    /// </summary>
    private void RouteLoggedInUser()
    {
        string role = AuthManager.Instance.UserRole;
        
        if (role == "teacher")
        {
            Debug.Log($"Welcome back, Teacher {AuthManager.Instance.CurrentUser.DisplayName}!");
            LoadScene(teacherDashboardSceneName);
        }
        else if (role == "student")
        {
            Debug.Log($"Welcome back, Student {AuthManager.Instance.CurrentUser.DisplayName}!");
            LoadScene(mainSceneName);
        }
        else
        {
            // Unknown role - go to home
            Debug.LogWarning($"Unknown user role: {role}. Redirecting to Home.");
            LoadHome();
        }
    }
    
    /// <summary>
    /// Load Home scene (for first-time users or logged-out users)
    /// </summary>
    private void LoadHome()
    {
        LoadScene(homeSceneName);
    }
    
    /// <summary>
    /// Load a scene using SceneFlowManager if available
    /// </summary>
    private void LoadScene(string sceneName)
    {
        if (SceneFlowManager.Instance != null)
        {
            SceneFlowManager.Instance.LoadScene(sceneName);
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }
    
    /// <summary>
    /// Call this method to reset initialization flag (useful for testing)
    /// </summary>
    public static void ResetInitialization()
    {
        hasInitialized = false;
    }
}