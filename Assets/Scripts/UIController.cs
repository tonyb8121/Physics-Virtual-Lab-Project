using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// UI Controller for Home scene (landing page)
/// Handles navigation to AuthScene and Menu (offline mode)
/// </summary>
public class UIController : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button startNowButton;
    [SerializeField] private Button registerButton;
    
    [Header("Scene Names")]
    [SerializeField] private string authSceneName = "AuthScene";
    [SerializeField] private string menuSceneName = "Menu";
    
    private void Start()
    {
        // Setup button listeners
        if (startNowButton != null)
        {
            startNowButton.onClick.AddListener(OnStartNowClicked);
        }
        
        if (registerButton != null)
        {
            registerButton.onClick.AddListener(OnRegisterClicked);
        }
    }
    
    /// <summary>
    /// Start Now button - Goes to Menu (offline mode)
    /// This allows users to use the app without authentication
    /// </summary>
    private void OnStartNowClicked()
    {
        Debug.Log("Start Now clicked - Loading Menu (offline mode)");
        
        if (SceneFlowManager.Instance != null)
        {
            SceneFlowManager.Instance.LoadScene(menuSceneName);
        }
        else
        {
            SceneManager.LoadScene(menuSceneName);
        }
    }
    
    /// <summary>
    /// Register button - Goes to AuthScene (login/signup)
    /// This takes users to the authentication flow
    /// </summary>
    private void OnRegisterClicked()
    {
        Debug.Log("Register clicked - Loading AuthScene");
        
        if (SceneFlowManager.Instance != null)
        {
            SceneFlowManager.Instance.LoadScene(authSceneName);
        }
        else
        {
            SceneManager.LoadScene(authSceneName);
        }
    }
    
    /// <summary>
    /// Optional: Quit application
    /// </summary>
    public void QuitApplication()
    {
        Debug.Log("Quitting application...");
        
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}