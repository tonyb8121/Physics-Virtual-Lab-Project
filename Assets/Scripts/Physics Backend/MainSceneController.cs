using UnityEngine;

/// <summary>
/// Main scene controller - handles scene initialization and back button
/// </summary>
public class MainSceneController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject dashboardPanel;
    
    private void Start()
    {
        InitializeScene();
    }
    
    private void InitializeScene()
    {
        // Show Welcome Toast if user is logged in
        if (AuthManager.Instance != null && AuthManager.Instance.IsLoggedIn)
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowToast("Welcome to AR Physics Lab!");
            }
        }
        
        // Show dashboard by default
        if (dashboardPanel != null)
        {
            dashboardPanel.SetActive(true);
        }
        
        Debug.Log("Main Scene Initialized");
    }
    
    /// <summary>
    /// Called when back button is pressed
    /// </summary>
    public void OnBackButtonClicked()
    {
        // For now, just show a message
        // Later this will handle module unloading
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowToast("Press Logout to exit.");
        }
        else
        {
            Debug.Log("Back button pressed - Logout to exit");
        }
    }
    
    /// <summary>
    /// Called when logout button is pressed
    /// </summary>
    public void OnLogoutClicked()
    {
        if (AuthManager.Instance != null)
        {
            AuthManager.Instance.Logout();
        }
        else
        {
            Debug.LogWarning("AuthManager not found");
        }
    }
}