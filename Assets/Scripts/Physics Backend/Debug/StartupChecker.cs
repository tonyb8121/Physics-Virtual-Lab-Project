using UnityEngine;
using System.Collections;

public class StartupChecker : MonoBehaviour
{
    private void Awake()
    {
        OnScreenLogger.Log("StartupChecker: Awake called");
        StartCoroutine(CheckSystems());
    }

    private IEnumerator CheckSystems()
    {
        yield return new WaitForSeconds(0.5f);

        OnScreenLogger.Log("Checking Firebase...");

        CheckFirebase();

        // Wait for Firebase check
        yield return StartCoroutine(WaitForFirebaseInit());

        // Check other managers
        CheckManagers();

        OnScreenLogger.Log("=== SYSTEM CHECK COMPLETE ===");
    }

    private void CheckFirebase()
    {
        try
        {
            if (FirebaseManager.Instance != null)
            {
                OnScreenLogger.Log("[OK] FirebaseManager found");
            }
            else
            {
                OnScreenLogger.Log("[FAIL] FirebaseManager is NULL!");
            }
        }
        catch (System.Exception e)
        {
            OnScreenLogger.Log($"[FAIL] Firebase Check Error: {e.Message}");
        }
    }

    private IEnumerator WaitForFirebaseInit()
    {
        OnScreenLogger.Log("Initializing Firebase...");

        if (FirebaseManager.Instance == null)
        {
            OnScreenLogger.Log("[FAIL] Cannot initialize - FirebaseManager is null");
            yield break;
        }

        var initTask = FirebaseManager.Instance.InitializeFirebase();

        // Wait outside try-catch
        yield return new WaitUntil(() => initTask.IsCompleted);

        // Check result inside try-catch (no yield here)
        try
        {
            if (initTask.Result)
            {
                OnScreenLogger.Log("[OK] Firebase initialized successfully!");
            }
            else
            {
                OnScreenLogger.Log("[FAIL] Firebase initialization FAILED!");
                OnScreenLogger.Log("[WARN] Check google-services.json!");
            }
        }
        catch (System.Exception e)
        {
            OnScreenLogger.Log($"[FAIL] Firebase Init Error: {e.Message}");
        }
    }

    private void CheckManagers()
    {
        try
        {
            if (AuthManager.Instance != null)
            {
                OnScreenLogger.Log("[OK] AuthManager found");
            }
            else
            {
                OnScreenLogger.Log("[FAIL] AuthManager is NULL!");
            }
        }
        catch (System.Exception e)
        {
            OnScreenLogger.Log($"[FAIL] AuthManager Error: {e.Message}");
        }

        try
        {
            if (UIManager.Instance != null)
            {
                OnScreenLogger.Log("[OK] UIManager found");
            }
            else
            {
                OnScreenLogger.Log("[WARN] UIManager not found");
            }
        }
        catch (System.Exception e)
        {
            OnScreenLogger.Log($"[FAIL] UIManager Error: {e.Message}");
        }
    }
}