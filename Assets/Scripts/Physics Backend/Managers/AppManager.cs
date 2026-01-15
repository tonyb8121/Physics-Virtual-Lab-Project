using UnityEngine;
using System.Collections;
using System.Threading.Tasks;
using Firebase;

public class AppManager : MonoBehaviour
{
    public static AppManager Instance { get; private set; }
    public bool IsInitialized { get; private set; } = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator Start()
    {
        Logger.Log("AppManager: Starting Initialization sequence...");

        // 1. Initialize Firebase (Correct Pattern)
        if (FirebaseManager.Instance != null)
        {
            // We call the method we wrote in FirebaseManager which handles the Task internally
            // Note: Ensure your FirebaseManager.InitializeFirebase() returns Task
            // Or simpler: Wait for FirebaseManager to set its own "IsInitialized" flag
            
            Task initTask = FirebaseManager.Instance.InitializeFirebase();
            yield return new WaitUntil(() => initTask.IsCompleted);
            
            if (initTask.IsFaulted)
            {
                Logger.LogError($"Firebase Init Failed: {initTask.Exception}");
                // Handle fatal error UI here
                yield break;
            }
        }

        // 2. Initialize Local Database
        if (DataPersistenceManager.Instance != null)
        {
            DataPersistenceManager.Instance.Initialize();
        }

        // 3. Mark as complete
        IsInitialized = true;
        Logger.Log("AppManager: System Initialized. Ready for Boot.");
    }
}