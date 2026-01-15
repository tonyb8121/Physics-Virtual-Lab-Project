using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Analytics;
using System.Threading.Tasks;
using System;

/// <summary>
/// Wrapper for Firebase initialization.
/// Handles dependency checks on Android.
/// </summary>
public class FirebaseManager : MonoBehaviour
{
    private static FirebaseManager _instance;
    public static FirebaseManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("FirebaseManager");
                _instance = go.AddComponent<FirebaseManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    public FirebaseAuth Auth { get; private set; }
    public FirebaseFirestore Firestore { get; private set; }
  
    public bool IsInitialized { get; private set; } = false;
private async void Start()
{
    bool success = await FirebaseManager.Instance.InitializeFirebase();
    
    if (!success)
    {
        // Show retry UI
        UIManager.Instance.ShowToast("⚠️ Firebase connection failed. Retrying...");
        await Task.Delay(3000);
        await FirebaseManager.Instance.InitializeFirebase();
    }
}
    /// <summary>
    /// Initialize Firebase with dependency checking.
    /// Call this method from AppManager or on app start.
    /// </summary>
    public async Task<bool> InitializeFirebase()
    {
        return await CheckAndFixDependenciesAsync();
    }

    private async Task<bool> CheckAndFixDependenciesAsync()
    {
        Logger.Log("Checking Firebase dependencies...");
        var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
        
        if (dependencyStatus == DependencyStatus.Available)
        {
            try
            {
                // Initialize modules
                Auth = FirebaseAuth.DefaultInstance;
                Firestore = FirebaseFirestore.DefaultInstance;
                FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
                
                // Enable Firestore offline persistence
                Firestore.Settings.PersistenceEnabled = true;
                
                IsInitialized = true;
                Logger.Log("Firebase Initialized Successfully", Color.green);
                return true; // Return true on success
            }
            catch (Exception e)
            {
                Logger.LogError($"Firebase Init Exception: {e.Message}");
                return false; // Return false on initialization failure
            }
        }
        else
        {
            Logger.LogError($"Could not resolve all Firebase dependencies: {dependencyStatus}");
            return false; // Return false on dependency failure
        }
    }
}