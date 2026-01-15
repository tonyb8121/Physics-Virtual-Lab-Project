using UnityEngine;
using Firebase.Auth;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using Firebase.Firestore;

/// <summary>
/// Manages authentication state and user sessions with smart routing.
/// </summary>
public class AuthManager : MonoBehaviour
{
    private static AuthManager _instance;
    public static AuthManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("AuthManager");
                _instance = go.AddComponent<AuthManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    public FirebaseUser CurrentUser => FirebaseManager.Instance.Auth?.CurrentUser;
    public bool IsLoggedIn => CurrentUser != null;
    public string UserRole { get; private set; } = "student";
    
    // ✅ FIX 1: Add this property to store the Student's Class ID
    public string UserClassId { get; private set; } = ""; 
    
    // Track if we've already checked auth state on startup
    private bool hasCheckedInitialAuth = false;

    // ⚠️ NEW: Define constants for scene names for cleaner routing logic
    private const string HOME_SCENE = "Home";
    private const string STUDENT_DASHBOARD_SCENE = "MainScene"; // Assumed student dashboard
    private const string TEACHER_DASHBOARD_SCENE = "TeacherDashboardScene"; // Assumed teacher dashboard

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Listen for auth state changes
        StartCoroutine(CheckAuthStateOnStartup());
    }

    /// <summary>
    /// Check authentication state when app starts (auto-login)
    /// </summary>
    private System.Collections.IEnumerator CheckAuthStateOnStartup()
    {
        // Wait for Firebase to initialize
        yield return new WaitForSeconds(0.5f);
        
        if (!hasCheckedInitialAuth && IsLoggedIn)
        {
            hasCheckedInitialAuth = true;
            Debug.Log("User already logged in, fetching role and class ID...");
            
            // Wait for profile fetch to complete before routing
            Task fetchTask = FetchUserRole();
            yield return new WaitUntil(() => fetchTask.IsCompleted);

            // ⚠️ CRITICAL ADDITION: Route the authenticated user to their correct dashboard
            RouteUserToDashboard();
        }
    }

    public async Task<bool> LoginAsync(string email, string password)
    {
        try
        {
            Logger.Log($"Attempting login for {email}...");
            await FirebaseManager.Instance.Auth.SignInWithEmailAndPasswordAsync(email, password);
            await FetchUserRole(); // ✅ Now fetches Class ID too
            Logger.Log("Login successful!", Color.green);
            
            // ⚠️ CRITICAL ADDITION: Route to dashboard immediately after successful login
            RouteUserToDashboard();

            return true;
        }
        catch (Exception e)
        {
            Logger.LogError($"Login Failed: {e.Message}");
            
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowToast($"Login failed: {e.Message}");
            }
            
            return false;
        }
    }

    public async Task<bool> SignupAsync(string email, string password, string fullName, string role, string schoolName = "")
    {
        try
        {
            Logger.Log($"Creating account for {email}...");
            AuthResult result = await FirebaseManager.Instance.Auth.CreateUserWithEmailAndPasswordAsync(email, password);
            FirebaseUser newUser = result.User;

            UserProfile profile = new UserProfile { DisplayName = fullName };
            await newUser.UpdateUserProfileAsync(profile);

            var userData = new Dictionary<string, object>
            {
                { "uid", newUser.UserId },
                { "email", email },
                { "displayName", fullName },
                { "role", role },
                { "createdAt", Timestamp.GetCurrentTimestamp() },
                { "totalPoints", 0 }
            };

            if (role == "teacher" && !string.IsNullOrEmpty(schoolName))
            {
                userData.Add("schoolName", schoolName);
            }
            // Note: classId is usually added later when a student joins a class, 
            // but we ensure UserRole is updated here.
            
            await FirestoreHelper.SaveUserData(newUser.UserId, userData);
            UserRole = role;

            Logger.Log("Signup successful!", Color.green);
            
            // ⚠️ CRITICAL ADDITION: Route to dashboard immediately after successful signup
            RouteUserToDashboard();

            return true;
        }
        catch (Exception e)
        {
            Logger.LogError($"Signup Failed: {e.Message}");
            
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowToast($"Signup Error: {e.Message}");
            }
            
            return false;
        }
    }

    public async Task<bool> ResetPasswordAsync(string email)
    {
        try
        {
            await FirebaseManager.Instance.Auth.SendPasswordResetEmailAsync(email);
            Logger.Log("Reset email sent.");
            
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowToast($"Password reset email sent to {email}.");
            }
            
            return true;
        }
        catch (Exception e)
        {
            Logger.LogError($"Reset Failed: {e.Message}");
            
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowToast($"Error: {e.Message}");
            }
            
            return false;
        }
    }

    /// <summary>
    /// Logout user and route to Home scene (where they can choose to login or go offline).
    /// </summary>
    public void Logout()
    {
        FirebaseManager.Instance.Auth.SignOut();
        UserRole = "student";
        UserClassId = ""; // Clear class ID on logout
        hasCheckedInitialAuth = false; // Reset flag for next login
        
        Logger.Log("User logged out.");
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowToast("Logged out successfully.");
        }
        
        // Route to Home (landing page)
        LoadScene(HOME_SCENE);
    }

    /// <summary>
    /// Updates user profile (name and/or password).
    /// </summary>
    public async Task<string> UpdateProfileAsync(string newName, string newPassword = "")
    {
        var user = CurrentUser;
        if (user == null) return "No user logged in.";

        try
        {
            // 1. Update Display Name
            if (!string.IsNullOrEmpty(newName) && newName != user.DisplayName)
            {
                UserProfile profile = new UserProfile { DisplayName = newName };
                await user.UpdateUserProfileAsync(profile);
                await FirestoreHelper.UpdateUserName(user.UserId, newName);
                Logger.Log($"Display name updated to: {newName}");
            }

            // 2. Update Password (if provided)
            if (!string.IsNullOrEmpty(newPassword))
            {
                await user.UpdatePasswordAsync(newPassword);
                Logger.Log("Password updated successfully!");
                return "PasswordChanged"; // Special code to trigger logout
            }

            return "Success";
        }
        catch (AggregateException ae)
        {
            if (ae.InnerException != null)
            {
                string errorMsg = ae.InnerException.Message;
                Logger.LogError($"Update Error: {errorMsg}");
                
                // Check for common Firebase error codes
                if (errorMsg.Contains("requires-recent-login") || errorMsg.Contains("CREDENTIAL_TOO_OLD"))
                {
                    return "Password change requires recent login. Please log out and log back in.";
                }
                
                return $"Error: {errorMsg}";
            }
            return $"Error: {ae.Message}";
        }
        catch (Exception ex)
        {
            Logger.LogError($"Update Error: {ex.Message}");
            
            if (ex.Message.Contains("requires-recent-login") || ex.Message.Contains("CREDENTIAL_TOO_OLD"))
            {
                return "Password change requires recent login. Please log out and log back in.";
            }
            
            return $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Fetch user role and classId from Firestore
    /// This is called on login and app startup
    /// </summary>
    public async Task FetchUserRole() // Note: Renaming this to FetchUserProfileData would be clearer
    {
        if (!IsLoggedIn) 
        {
            Debug.Log("Cannot fetch profile - user not logged in");
            return;
        }

        try
        {
            var data = await FirestoreHelper.GetUserData(CurrentUser.UserId);
            
            if (data != null)
            {
                // 1. Get Role
                if (data.ContainsKey("role"))
                {
                    UserRole = data["role"].ToString();
                    Logger.Log($"User Role fetched: {UserRole}");
                }
                else
                {
                    UserRole = "student";
                }

                // ✅ FIX 2: Get Class ID (Crucial for Student Assignments)
                if (data.ContainsKey("classId"))
                {
                    UserClassId = data["classId"].ToString();
                    Logger.Log($"User Class ID fetched: {UserClassId}");
                }
                else
                {
                    UserClassId = ""; // No class joined yet
                    Logger.LogWarning("User profile has no classId field or it is empty.");
                }
            }
        }
        catch (Exception e)
        {
            Logger.LogError($"Failed to fetch user profile: {e.Message}");
            UserRole = "student";
            UserClassId = "";
        }
    }

    // ✅ FIX 3: Helper to update Class ID locally immediately after joining a class
    public void UpdateLocalClassId(string newClassId)
    {
        UserClassId = newClassId;
        Logger.Log($"Local Class ID updated to: {newClassId}");
    }

    // ---------------------------------------------------------------------
    // ⚠️ NEW CENTRALIZED ROUTING LOGIC (No changes needed here)
    // ---------------------------------------------------------------------

    /// <summary>
    /// Determines the correct scene (dashboard) to load based on the authenticated user's role.
    /// This should be called after successful login, signup, and on app startup.
    /// </summary>
    public void RouteUserToDashboard()
    {
        if (!IsLoggedIn)
        {
            Debug.LogWarning("Attempted to route to dashboard, but user is not logged in.");
            LoadScene(HOME_SCENE);
            return;
        }
        
        string sceneToLoad;

        if (UserRole == "student")
        {
            // Student goes to MainScene dashboard, NOT the generic Menu scene
            sceneToLoad = STUDENT_DASHBOARD_SCENE;
        }
        else if (UserRole == "teacher")
        {
            sceneToLoad = TEACHER_DASHBOARD_SCENE;
        }
        else
        {
            // Default authenticated user to student dashboard if role is unknown
            Debug.LogWarning($"Unknown role '{UserRole}', defaulting to Student Dashboard.");
            sceneToLoad = STUDENT_DASHBOARD_SCENE;
        }

        Logger.Log($"Routing logged-in user (Role: {UserRole}) to {sceneToLoad}");
        LoadScene(sceneToLoad);
    }

    /// <summary>
    /// Helper method to centralize scene loading logic.
    /// </summary>
    private void LoadScene(string sceneName)
    {
        if (SceneFlowManager.Instance != null)
        {
            SceneFlowManager.Instance.LoadScene(sceneName);
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }
    }
    
    /// <summary>
    /// Check if authentication is ready (Firebase initialized)
    /// </summary>
    public bool IsAuthReady()
    {
        return FirebaseManager.Instance != null && 
               FirebaseManager.Instance.Auth != null;
    }
    
    /// <summary>
    /// Get user display name safely
    /// </summary>
    public string GetDisplayName()
    {
        if (!IsLoggedIn) return "Guest";
        
        string name = CurrentUser.DisplayName;
        if (string.IsNullOrEmpty(name))
        {
            name = CurrentUser.Email;
        }
        
        return name ?? "User";
    }
}